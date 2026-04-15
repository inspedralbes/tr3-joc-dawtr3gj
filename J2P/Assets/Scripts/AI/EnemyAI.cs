using System;
using UnityEngine;

namespace TankArena2D
{
    [RequireComponent(typeof(TankMovement2D), typeof(TurretAim), typeof(Weapon))]
    [RequireComponent(typeof(Health))]
    public sealed class EnemyAI : MonoBehaviour, IEnemyAgent
    {
        [SerializeField] private Transform target;
        [SerializeField] private Health targetHealth;
        [SerializeField, Min(0.5f)] private float detectionRange = 24f;
        [SerializeField, Min(0.5f)] private float attackRange = 13f;
        [SerializeField, Min(0f)] private float preferredDistance = 8f;
        [SerializeField, Min(0f)] private float retreatDistance = 5f;
        [SerializeField, Min(0f)] private float strafeStrength = 0.55f;
        [SerializeField, Min(0.1f)] private float obstacleCheckDistance = 2.75f;
        [SerializeField, Min(0.05f)] private float obstacleProbeRadius = 0.38f;
        [SerializeField, Min(0.1f)] private float stuckCheckInterval = 0.5f;
        [SerializeField, Min(0.01f)] private float stuckDistanceThreshold = 0.12f;
        [SerializeField, Min(0.1f)] private float unstuckDuration = 0.85f;

        private TankMovement2D movement;
        private TurretAim turretAim;
        private Weapon weapon;
        private Health health;
        private Collider2D[] selfColliders;
        private Vector2 lastStuckCheckPosition;
        private float lastStuckCheckTime;
        private Vector2 forcedMoveDirection;
        private float forcedMoveUntil;
        private float strafeSign;

        private void Awake()
        {
            movement = GetComponent<TankMovement2D>();
            turretAim = GetComponent<TurretAim>();
            weapon = GetComponent<Weapon>();
            health = GetComponent<Health>();
            selfColliders = GetComponentsInChildren<Collider2D>(true);
            strafeSign = UnityEngine.Random.value >= 0.5f ? 1f : -1f;
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDeath;
            }

            lastStuckCheckPosition = transform.position;
            lastStuckCheckTime = Time.time;
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        public void Configure(
            Transform newTarget,
            float newDetectionRange,
            float newAttackRange,
            float newPreferredDistance,
            float newRetreatDistance,
            float newObstacleCheckDistance,
            float newObstacleProbeRadius,
            float newStrafeStrength)
        {
            SetTarget(newTarget);
            detectionRange = Mathf.Max(0.5f, newDetectionRange);
            attackRange = Mathf.Max(0.5f, newAttackRange);
            preferredDistance = Mathf.Max(0f, newPreferredDistance);
            retreatDistance = Mathf.Max(0f, newRetreatDistance);
            obstacleCheckDistance = Mathf.Max(0.1f, newObstacleCheckDistance);
            obstacleProbeRadius = Mathf.Max(0.05f, newObstacleProbeRadius);
            strafeStrength = Mathf.Max(0f, newStrafeStrength);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            targetHealth = target != null ? target.GetComponent<Health>() : null;
        }

        public void ResetAgent(Vector2 position)
        {
            transform.position = position;
            forcedMoveDirection = Vector2.zero;
            forcedMoveUntil = 0f;
            lastStuckCheckPosition = position;
            lastStuckCheckTime = Time.time;

            if (health != null && health.IsDead)
            {
                health.Revive();
            }

            enabled = true;
        }

        private void Update()
        {
            if (health == null || health.IsDead)
            {
                return;
            }

            if (target == null || targetHealth == null || targetHealth.IsDead)
            {
                movement.SetMoveInput(Vector2.zero);
                return;
            }

            Vector2 currentPosition = transform.position;
            Vector2 toTarget = (Vector2)target.position - currentPosition;
            float sqrDistance = toTarget.sqrMagnitude;

            if (sqrDistance > detectionRange * detectionRange)
            {
                movement.SetMoveInput(Vector2.zero);
                turretAim.AimInDirection(toTarget);
                return;
            }

            turretAim.AimAtWorldPoint(target.position);

            Vector2 moveCommand = BuildMovementCommand(toTarget);
            movement.SetMoveInput(moveCommand);

            if (sqrDistance <= attackRange * attackRange && HasLineOfSight())
            {
                weapon.TryFire(turretAim.Forward);
            }

            UpdateStuckRecovery(moveCommand, currentPosition, toTarget);
        }

        private Vector2 BuildMovementCommand(Vector2 toTarget)
        {
            float distance = toTarget.magnitude;
            Vector2 directionToTarget = distance > 0.001f ? toTarget / distance : Vector2.zero;
            Vector2 desired = Vector2.zero;

            if (distance > preferredDistance)
            {
                desired += directionToTarget;
            }
            else if (distance < retreatDistance)
            {
                desired -= directionToTarget * 0.75f;
            }

            if (distance <= attackRange)
            {
                desired += Vector2.Perpendicular(directionToTarget) * strafeSign * strafeStrength;
            }

            Vector2 referenceDirection = desired.sqrMagnitude > 0.0001f ? desired.normalized : directionToTarget;
            desired += GetObstacleAvoidance(referenceDirection);

            if (Time.time < forcedMoveUntil)
            {
                desired += forcedMoveDirection;
            }

            return Vector2.ClampMagnitude(desired, 1f);
        }

        private Vector2 GetObstacleAvoidance(Vector2 referenceDirection)
        {
            if (referenceDirection.sqrMagnitude < 0.0001f)
            {
                return Vector2.zero;
            }

            Vector2 avoidance = Vector2.zero;
            Vector2 forward = referenceDirection.normalized;
            Vector2 left = Rotate(forward, 30f);
            Vector2 right = Rotate(forward, -30f);

            avoidance += GetAvoidanceFromProbe(forward, 1.25f);
            avoidance += GetAvoidanceFromProbe(left, 0.9f);
            avoidance += GetAvoidanceFromProbe(right, 0.9f);

            return avoidance;
        }

        private Vector2 GetAvoidanceFromProbe(Vector2 direction, float weight)
        {
            RaycastHit2D hit = FindBlockingHit(direction, obstacleCheckDistance);

            if (hit.collider == null)
            {
                return Vector2.zero;
            }

            float proximity = 1f - Mathf.Clamp01(hit.distance / obstacleCheckDistance);
            return hit.normal * weight * (proximity + 0.15f);
        }

        private RaycastHit2D FindBlockingHit(Vector2 direction, float distance)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, obstacleProbeRadius, direction, distance);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hitCollider = hits[i].collider;

                if (hitCollider == null || hitCollider.isTrigger)
                {
                    continue;
                }

                if (BelongsToSelf(hitCollider) || BelongsToTarget(hitCollider))
                {
                    continue;
                }

                return hits[i];
            }

            return default;
        }

        private bool HasLineOfSight()
        {
            Vector2 origin = weapon.Muzzle != null ? weapon.Muzzle.position : transform.position;
            Vector2 toTarget = (Vector2)target.position - origin;
            float distance = toTarget.magnitude;

            if (distance <= 0.01f)
            {
                return true;
            }

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, toTarget.normalized, distance);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hitCollider = hits[i].collider;

                if (hitCollider == null || hitCollider.isTrigger || BelongsToSelf(hitCollider))
                {
                    continue;
                }

                return BelongsToTarget(hitCollider);
            }

            return true;
        }

        private void UpdateStuckRecovery(Vector2 desiredMove, Vector2 currentPosition, Vector2 toTarget)
        {
            if (Time.time - lastStuckCheckTime < stuckCheckInterval)
            {
                return;
            }

            float movedDistance = Vector2.Distance(currentPosition, lastStuckCheckPosition);

            if (desiredMove.sqrMagnitude > 0.2f && movedDistance < stuckDistanceThreshold)
            {
                Vector2 escapeDirection = toTarget.sqrMagnitude > 0.01f
                    ? Vector2.Perpendicular(toTarget.normalized) * strafeSign
                    : UnityEngine.Random.insideUnitCircle.normalized;

                if (UnityEngine.Random.value > 0.5f)
                {
                    escapeDirection *= -1f;
                }

                forcedMoveDirection = escapeDirection;
                forcedMoveUntil = Time.time + unstuckDuration;
                strafeSign *= -1f;
            }

            lastStuckCheckPosition = currentPosition;
            lastStuckCheckTime = Time.time;
        }

        private bool BelongsToSelf(Collider2D hitCollider)
        {
            for (int i = 0; i < selfColliders.Length; i++)
            {
                if (selfColliders[i] == hitCollider)
                {
                    return true;
                }
            }

            return hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform);
        }

        private bool BelongsToTarget(Collider2D hitCollider)
        {
            return target != null &&
                   (hitCollider.transform == target || hitCollider.transform.IsChildOf(target));
        }

        private void HandleDeath(Health _, DamageInfo __)
        {
            movement.StopImmediate();
            enabled = false;
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);

            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }
    }
}
