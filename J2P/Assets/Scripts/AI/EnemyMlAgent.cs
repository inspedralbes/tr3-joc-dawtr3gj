using Unity.InferenceEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace TankArena2D
{
    [RequireComponent(typeof(TankMovement2D), typeof(TurretAim), typeof(Weapon))]
    [RequireComponent(typeof(Health), typeof(BehaviorParameters), typeof(DecisionRequester))]
    [RequireComponent(typeof(FactionMember))]
    public sealed class EnemyMlAgent : Agent, IEnemyAgent
    {
        [SerializeField] private Transform target;
        [SerializeField] private Health targetHealth;
        [SerializeField, Min(0.5f)] private float detectionRange = 26f;
        [SerializeField, Min(0.5f)] private float attackRange = 13.5f;
        [SerializeField, Min(0f)] private float preferredDistance = 8f;
        [SerializeField, Min(0f)] private float retreatDistance = 4.8f;
        [SerializeField, Min(0f)] private float strafeStrength = 0.58f;
        [SerializeField, Min(0.1f)] private float obstacleCheckDistance = 2.8f;
        [SerializeField, Min(0.05f)] private float obstacleProbeRadius = 0.38f;
        [SerializeField, Min(1)] private int decisionPeriod = 5;
        [SerializeField] private string behaviorName = "TankArenaEnemy";
        [SerializeField] private ModelAsset trainedModel;
        [SerializeField] private bool deterministicInference = true;

        private TankMovement2D movement;
        private TurretAim turretAim;
        private Weapon weapon;
        private Health health;
        private BehaviorParameters behaviorParameters;
        private DecisionRequester decisionRequester;
        private Collider2D[] selfColliders;
        private float heuristicStrafeSign;
        private float previousDistanceToTarget = -1f;

        protected override void Awake()
        {
            base.Awake();

            movement = GetComponent<TankMovement2D>();
            turretAim = GetComponent<TurretAim>();
            weapon = GetComponent<Weapon>();
            health = GetComponent<Health>();
            behaviorParameters = GetComponent<BehaviorParameters>();
            decisionRequester = GetComponent<DecisionRequester>();
            selfColliders = GetComponentsInChildren<Collider2D>(true);
            heuristicStrafeSign = UnityEngine.Random.value >= 0.5f ? 1f : -1f;

            ConfigureBehavior();
        }

        public override void Initialize()
        {
            previousDistanceToTarget = -1f;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (health != null)
            {
                health.Died += HandleSelfDeath;
            }

            SubscribeToTarget();
        }

        protected override void OnDisable()
        {
            UnsubscribeFromTarget();

            if (health != null)
            {
                health.Died -= HandleSelfDeath;
            }

            base.OnDisable();
        }

        public void Configure(
            Transform newTarget,
            float newDetectionRange,
            float newAttackRange,
            float newPreferredDistance,
            float newRetreatDistance,
            float newObstacleCheckDistance,
            float newObstacleProbeRadius,
            float newStrafeStrength,
            ModelAsset model = null,
            string newBehaviorName = "TankArenaEnemy",
            int newDecisionPeriod = 5)
        {
            trainedModel = model;
            behaviorName = newBehaviorName;
            decisionPeriod = Mathf.Max(1, newDecisionPeriod);
            detectionRange = Mathf.Max(0.5f, newDetectionRange);
            attackRange = Mathf.Max(0.5f, newAttackRange);
            preferredDistance = Mathf.Max(0f, newPreferredDistance);
            retreatDistance = Mathf.Max(0f, newRetreatDistance);
            obstacleCheckDistance = Mathf.Max(0.1f, newObstacleCheckDistance);
            obstacleProbeRadius = Mathf.Max(0.05f, newObstacleProbeRadius);
            strafeStrength = Mathf.Max(0f, newStrafeStrength);

            ConfigureBehavior();
            SetTarget(newTarget);
        }

        public void SetTarget(Transform newTarget)
        {
            UnsubscribeFromTarget();

            target = newTarget;
            targetHealth = target != null ? target.GetComponent<Health>() : null;

            SubscribeToTarget();
        }

        public void ResetAgent(Vector2 position)
        {
            transform.position = position;
            movement.StopImmediate();
            health.Revive();
            previousDistanceToTarget = -1f;
            RequestDecision();
        }

        public override void OnEpisodeBegin()
        {
            movement.StopImmediate();
            previousDistanceToTarget = -1f;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            Vector2 currentPosition = transform.position;
            Vector2 velocity = movement.Velocity / Mathf.Max(0.1f, movement.MoveSpeed);
            Vector2 toTarget = target != null ? (Vector2)target.position - currentPosition : Vector2.zero;
            float distance = toTarget.magnitude;
            Vector2 directionToTarget = distance > 0.001f ? toTarget / distance : Vector2.zero;

            sensor.AddObservation(directionToTarget.x);
            sensor.AddObservation(directionToTarget.y);
            sensor.AddObservation(Mathf.Clamp01(distance / detectionRange));
            sensor.AddObservation(velocity.x);
            sensor.AddObservation(velocity.y);
            sensor.AddObservation(health != null ? health.CurrentHealth / health.MaxHealth : 0f);
            sensor.AddObservation(targetHealth != null ? targetHealth.CurrentHealth / targetHealth.MaxHealth : 0f);
            sensor.AddObservation(targetHealth != null && !targetHealth.IsDead ? 1f : 0f);
            sensor.AddObservation(distance <= attackRange ? 1f : 0f);
            sensor.AddObservation(HasLineOfSight() ? 1f : 0f);
            sensor.AddObservation(weapon.CanFire ? 1f : 0f);
            sensor.AddObservation(ProbeClearance(turretAim.Forward));
            sensor.AddObservation(ProbeClearance(Rotate(turretAim.Forward, 30f)));
            sensor.AddObservation(ProbeClearance(Rotate(turretAim.Forward, -30f)));
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (health == null || health.IsDead)
            {
                movement.SetMoveInput(Vector2.zero);
                return;
            }

            if (target == null || targetHealth == null || targetHealth.IsDead)
            {
                movement.SetMoveInput(Vector2.zero);
                return;
            }

            int xAction = actions.DiscreteActions[0];
            int yAction = actions.DiscreteActions[1];
            int fireAction = actions.DiscreteActions[2];

            Vector2 moveInput = new Vector2(DecodeBranch(xAction), DecodeBranch(yAction));
            movement.SetMoveInput(Vector2.ClampMagnitude(moveInput, 1f));

            turretAim.AimAtWorldPoint(target.position);

            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
            float distanceToTarget = toTarget.magnitude;

            if (fireAction == 1 && distanceToTarget <= attackRange && HasLineOfSight())
            {
                if (weapon.TryFire(turretAim.Forward))
                {
                    AddReward(0.0025f);
                }
            }

            if (previousDistanceToTarget >= 0f)
            {
                float distanceDelta = previousDistanceToTarget - distanceToTarget;
                AddReward(Mathf.Clamp(distanceDelta * 0.01f, -0.01f, 0.01f));
            }

            previousDistanceToTarget = distanceToTarget;
            AddReward(-0.0005f);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<int> actions = actionsOut.DiscreteActions;
            Vector2 move = Vector2.zero;
            bool shouldFire = false;

            if (target != null && targetHealth != null && !targetHealth.IsDead)
            {
                Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
                float sqrDistance = toTarget.sqrMagnitude;

                if (sqrDistance <= detectionRange * detectionRange)
                {
                    move = BuildHeuristicMove(toTarget);
                    shouldFire = sqrDistance <= attackRange * attackRange && HasLineOfSight();
                }
            }

            actions[0] = EncodeBranch(move.x);
            actions[1] = EncodeBranch(move.y);
            actions[2] = shouldFire ? 1 : 0;
        }

        private void Update()
        {
            if (health == null || health.IsDead)
            {
                return;
            }

            if (target != null && targetHealth != null && !targetHealth.IsDead)
            {
                turretAim.AimAtWorldPoint(target.position);
            }
        }

        private void ConfigureBehavior()
        {
            if (behaviorParameters == null || decisionRequester == null)
            {
                return;
            }

            var brainParameters = behaviorParameters.BrainParameters;
            brainParameters.VectorObservationSize = 14;
            brainParameters.NumStackedVectorObservations = 1;
            brainParameters.ActionSpec = ActionSpec.MakeDiscrete(3, 3, 2);
            behaviorParameters.BehaviorName = behaviorName;
            behaviorParameters.BehaviorType = BehaviorType.Default;
            behaviorParameters.Model = trainedModel;
            behaviorParameters.DeterministicInference = deterministicInference;
            behaviorParameters.TeamId = 1;

            decisionRequester.DecisionPeriod = Mathf.Max(1, decisionPeriod);
            decisionRequester.DecisionStep = 0;
            decisionRequester.TakeActionsBetweenDecisions = true;
        }

        private Vector2 BuildHeuristicMove(Vector2 toTarget)
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
                desired -= directionToTarget * 0.8f;
            }

            if (distance <= attackRange)
            {
                desired += Vector2.Perpendicular(directionToTarget) * heuristicStrafeSign * strafeStrength;
            }

            Vector2 referenceDirection = desired.sqrMagnitude > 0.0001f ? desired.normalized : directionToTarget;
            desired += GetObstacleAvoidance(referenceDirection);
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

        private float ProbeClearance(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return 1f;
            }

            RaycastHit2D hit = FindBlockingHit(direction.normalized, obstacleCheckDistance);

            if (hit.collider == null)
            {
                return 1f;
            }

            return Mathf.Clamp01(hit.distance / obstacleCheckDistance);
        }

        private RaycastHit2D FindBlockingHit(Vector2 direction, float distance)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, obstacleProbeRadius, direction, distance);

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

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
            if (target == null)
            {
                return false;
            }

            Vector2 origin = weapon.Muzzle != null ? weapon.Muzzle.position : transform.position;
            Vector2 toTarget = (Vector2)target.position - origin;
            float distance = toTarget.magnitude;

            if (distance <= 0.01f)
            {
                return true;
            }

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, toTarget.normalized, distance);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

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

        private void SubscribeToTarget()
        {
            if (targetHealth != null)
            {
                targetHealth.Died += HandleTargetDeath;
            }
        }

        private void UnsubscribeFromTarget()
        {
            if (targetHealth != null)
            {
                targetHealth.Died -= HandleTargetDeath;
            }
        }

        private void HandleSelfDeath(Health _, DamageInfo __)
        {
            movement.StopImmediate();
            AddReward(-1f);
            EndEpisode();
        }

        private void HandleTargetDeath(Health _, DamageInfo __)
        {
            AddReward(1f);
            EndEpisode();
        }

        private static int EncodeBranch(float value)
        {
            if (value < -0.25f)
            {
                return 0;
            }

            if (value > 0.25f)
            {
                return 2;
            }

            return 1;
        }

        private static float DecodeBranch(int branchValue)
        {
            return branchValue switch
            {
                0 => -1f,
                2 => 1f,
                _ => 0f
            };
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
