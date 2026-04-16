using UnityEngine;

namespace TankArena2D
{
    [RequireComponent(typeof(TankMovement2D), typeof(Health), typeof(TankPerception2D))]
    [RequireComponent(typeof(FactionMember))]
    public sealed class TrainingTargetController : MonoBehaviour
    {
        [SerializeField] private ArenaBounds arenaBounds;
        [SerializeField] private Transform threatTarget;
        [SerializeField, Min(1f)] private float evadeDistance = 10f;
        [SerializeField, Min(1f)] private float safeDistance = 16f;
        [SerializeField, Min(0.1f)] private float patrolChangeInterval = 2.4f;
        [SerializeField, Min(0f)] private float patrolMoveStrength = 0.7f;

        private TankMovement2D movement;
        private Health health;
        private TankPerception2D perception;
        private Vector2 patrolDirection;
        private float nextPatrolChangeTime;
        private float strafeSign;

        public Health Health => health;

        private void Awake()
        {
            CacheComponents();
            strafeSign = Random.value >= 0.5f ? 1f : -1f;
            ChoosePatrolDirection(true);
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        public void Configure(ArenaBounds bounds, Transform threat)
        {
            CacheComponents();
            arenaBounds = bounds;
            threatTarget = threat;
        }

        public void SetThreat(Transform newThreat)
        {
            threatTarget = newThreat;
        }

        public void RespawnAt(Vector2 position)
        {
            CacheComponents();
            transform.position = position;
            movement.StopImmediate();
            health.Revive();
            enabled = true;
            ChoosePatrolDirection(true);
        }

        private void Update()
        {
            CacheComponents();

            if (health == null || health.IsDead)
            {
                return;
            }

            perception.Scan(threatTarget);

            Vector2 move = Vector2.zero;

            if (threatTarget != null)
            {
                Vector2 fromThreat = (Vector2)transform.position - (Vector2)threatTarget.position;
                float distance = fromThreat.magnitude;

                if (distance < evadeDistance)
                {
                    Vector2 desired = distance > 0.001f ? fromThreat / distance : Random.insideUnitCircle.normalized;
                    desired += Vector2.Perpendicular(desired) * strafeSign * 0.4f;
                    move = perception.GetBestDirection(desired);
                }
                else if (distance < safeDistance && perception.HasDirectLineOfSight(threatTarget))
                {
                    Vector2 desired = fromThreat.sqrMagnitude > 0.001f ? fromThreat.normalized : patrolDirection;
                    move = perception.GetBestDirection(desired);
                }
            }

            if (move.sqrMagnitude < 0.01f)
            {
                if (Time.time >= nextPatrolChangeTime || perception.GetNormalizedClearance(patrolDirection) < 0.25f)
                {
                    ChoosePatrolDirection(false);
                }

                move = EnemySteeringUtility.BuildPatrolMove(perception, patrolDirection, patrolMoveStrength);
            }

            movement.SetMoveInput(move);
        }

        private void ChoosePatrolDirection(bool forceImmediate)
        {
            patrolDirection = Random.insideUnitCircle.normalized;

            if (patrolDirection.sqrMagnitude < 0.0001f)
            {
                patrolDirection = Vector2.right;
            }

            nextPatrolChangeTime = Time.time + (forceImmediate ? 0.5f : patrolChangeInterval);
        }

        private void HandleDeath(Health _, DamageInfo __)
        {
            movement.StopImmediate();
            enabled = false;
        }

        private void CacheComponents()
        {
            movement ??= GetComponent<TankMovement2D>();
            health ??= GetComponent<Health>();
            perception ??= GetComponent<TankPerception2D>();
        }
    }
}
