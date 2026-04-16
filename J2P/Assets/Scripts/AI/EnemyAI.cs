using UnityEngine;

namespace TankArena2D
{
    [RequireComponent(typeof(TankMovement2D), typeof(TurretAim), typeof(Weapon))]
    [RequireComponent(typeof(Health), typeof(TankPerception2D))]
    public sealed class EnemyAI : MonoBehaviour, IEnemyAgent
    {
        [SerializeField] private Transform target;
        [SerializeField] private Health targetHealth;
        [SerializeField, Min(0.5f)] private float detectionRange = 28f;
        [SerializeField, Min(0.5f)] private float attackRange = 14f;
        [SerializeField, Min(0f)] private float preferredDistance = 8.5f;
        [SerializeField, Min(0f)] private float retreatDistance = 4.5f;
        [SerializeField, Min(0f)] private float strafeStrength = 0.6f;
        [SerializeField, Min(0.1f)] private float searchDuration = 4.5f;
        [SerializeField, Min(0.1f)] private float patrolChangeInterval = 2.5f;
        [SerializeField, Min(0f)] private float patrolMoveStrength = 0.55f;
        [SerializeField, Min(0.1f)] private float stuckCheckInterval = 0.5f;
        [SerializeField, Min(0.01f)] private float stuckDistanceThreshold = 0.12f;
        [SerializeField, Min(0.1f)] private float unstuckDuration = 0.85f;

        private TankMovement2D movement;
        private TurretAim turretAim;
        private Weapon weapon;
        private Health health;
        private TankPerception2D perception;
        private Vector2 patrolDirection;
        private Vector2 forcedMoveDirection;
        private Vector2 lastStuckCheckPosition;
        private float lastStuckCheckTime;
        private float forcedMoveUntil;
        private float nextPatrolChangeTime;
        private float strafeSign;

        private void Awake()
        {
            movement = GetComponent<TankMovement2D>();
            turretAim = GetComponent<TurretAim>();
            weapon = GetComponent<Weapon>();
            health = GetComponent<Health>();
            perception = GetComponent<TankPerception2D>();
            strafeSign = Random.value >= 0.5f ? 1f : -1f;
            ChoosePatrolDirection(true);
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDeath;
            }

            movement.StopImmediate();
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
            float _,
            float __,
            float newStrafeStrength)
        {
            perception ??= GetComponent<TankPerception2D>();
            SetTarget(newTarget);
            detectionRange = Mathf.Max(0.5f, newDetectionRange);
            attackRange = Mathf.Max(0.5f, newAttackRange);
            preferredDistance = Mathf.Max(0f, newPreferredDistance);
            retreatDistance = Mathf.Max(0f, newRetreatDistance);
            strafeStrength = Mathf.Max(0f, newStrafeStrength);

            if (perception != null)
            {
                perception.Configure(null, 16, Mathf.Max(attackRange, 12f), detectionRange, true);
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            targetHealth = target != null ? target.GetComponent<Health>() : null;
        }

        public void ResetAgent(Vector2 position)
        {
            transform.position = position;
            movement.StopImmediate();
            forcedMoveDirection = Vector2.zero;
            forcedMoveUntil = 0f;
            strafeSign = Random.value >= 0.5f ? 1f : -1f;
            ChoosePatrolDirection(true);
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

            perception.Scan(target);

            Vector2 moveInput = Vector2.zero;
            Vector2 aimDirection = turretAim.Forward;
            bool shouldFire = false;

            if (target != null && targetHealth != null && !targetHealth.IsDead)
            {
                Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;

                if (perception.TargetDetected && perception.TargetDistance <= detectionRange)
                {
                    moveInput = EnemySteeringUtility.BuildCombatMove(
                        perception,
                        toTarget,
                        attackRange,
                        preferredDistance,
                        retreatDistance,
                        strafeSign,
                        strafeStrength);

                    aimDirection = perception.TargetDirection;
                    shouldFire = perception.HasLineOfSight &&
                                 perception.TargetDistance <= attackRange &&
                                 Vector2.Dot(turretAim.Forward, perception.TargetDirection) > 0.92f;
                }
                else if (perception.HasLastKnownTarget && perception.TimeSinceLastSeen <= searchDuration)
                {
                    Vector2 toLastKnown = perception.LastKnownTargetPosition - (Vector2)transform.position;
                    moveInput = EnemySteeringUtility.BuildSearchMove(perception, toLastKnown);
                    aimDirection = toLastKnown.sqrMagnitude > 0.01f ? toLastKnown.normalized : turretAim.Forward;
                }
                else
                {
                    moveInput = BuildPatrolMove();
                    aimDirection = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : patrolDirection;
                }
            }
            else
            {
                moveInput = BuildPatrolMove();
                aimDirection = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : patrolDirection;
            }

            moveInput = ApplyStuckRecovery(moveInput);
            movement.SetMoveInput(moveInput);

            if (aimDirection.sqrMagnitude > 0.001f)
            {
                turretAim.AimInDirection(aimDirection);
            }

            if (shouldFire)
            {
                weapon.TryFire(turretAim.Forward);
            }

            UpdateStuckRecovery(moveInput);
        }

        private Vector2 BuildPatrolMove()
        {
            if (Time.time >= nextPatrolChangeTime || perception.GetNormalizedClearance(patrolDirection) < 0.28f)
            {
                ChoosePatrolDirection(false);
            }

            return EnemySteeringUtility.BuildPatrolMove(perception, patrolDirection, patrolMoveStrength);
        }

        private Vector2 ApplyStuckRecovery(Vector2 moveInput)
        {
            if (Time.time < forcedMoveUntil)
            {
                moveInput += forcedMoveDirection;
            }

            return Vector2.ClampMagnitude(moveInput, 1f);
        }

        private void UpdateStuckRecovery(Vector2 desiredMove)
        {
            if (Time.time - lastStuckCheckTime < stuckCheckInterval)
            {
                return;
            }

            Vector2 currentPosition = transform.position;
            float movedDistance = Vector2.Distance(currentPosition, lastStuckCheckPosition);

            if (desiredMove.sqrMagnitude > 0.18f && movedDistance < stuckDistanceThreshold)
            {
                Vector2 lateral = desiredMove.sqrMagnitude > 0.01f
                    ? Vector2.Perpendicular(desiredMove.normalized) * (Random.value > 0.5f ? 1f : -1f)
                    : Random.insideUnitCircle.normalized;

                forcedMoveDirection = perception.GetBestDirection(lateral);
                forcedMoveUntil = Time.time + unstuckDuration;
                strafeSign *= -1f;
                ChoosePatrolDirection(false);
            }

            lastStuckCheckPosition = currentPosition;
            lastStuckCheckTime = Time.time;
        }

        private void ChoosePatrolDirection(bool immediate)
        {
            patrolDirection = Random.insideUnitCircle.normalized;

            if (patrolDirection.sqrMagnitude < 0.0001f)
            {
                patrolDirection = Vector2.right;
            }

            nextPatrolChangeTime = Time.time + (immediate ? 0.5f : patrolChangeInterval);
        }

        private void HandleDeath(Health _, DamageInfo __)
        {
            movement.StopImmediate();
            enabled = false;
        }
    }
}
