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
    [RequireComponent(typeof(FactionMember), typeof(TankPerception2D))]
    public sealed class EnemyMlAgent : Agent, IEnemyAgent
    {
        [SerializeField] private Transform target;
        [SerializeField] private Health targetHealth;
        [SerializeField] private TrainingArenaManager trainingArena;
        [SerializeField] private EnemyAgentExecutionMode executionMode = EnemyAgentExecutionMode.Auto;
        [SerializeField, Min(0.5f)] private float detectionRange = 28f;
        [SerializeField, Min(0.5f)] private float attackRange = 14f;
        [SerializeField, Min(0f)] private float preferredDistance = 8.5f;
        [SerializeField, Min(0f)] private float retreatDistance = 4.5f;
        [SerializeField, Min(0f)] private float strafeStrength = 0.6f;
        [SerializeField, Min(0.1f)] private float searchDuration = 4.5f;
        [SerializeField, Min(0.1f)] private float patrolChangeInterval = 2.4f;
        [SerializeField, Min(1)] private int decisionPeriod = 5;
        [SerializeField] private string behaviorName = "TankArenaEnemy";
        [SerializeField] private ModelAsset trainedModel;
        [SerializeField] private bool deterministicInference = true;
        [SerializeField] private float stepPenalty = -0.0006f;
        [SerializeField] private float approachRewardScale = 0.01f;
        [SerializeField] private float lineOfSightReward = 0.0015f;
        [SerializeField] private float aimAlignmentReward = 0.001f;
        [SerializeField] private float successfulShotReward = 0.015f;
        [SerializeField] private float targetDamageReward = 0.08f;
        [SerializeField] private float targetKillReward = 1.2f;
        [SerializeField] private float selfDamagePenalty = -0.05f;
        [SerializeField] private float deathPenalty = -1f;
        [SerializeField] private float obstacleCollisionPenalty = -0.015f;
        [SerializeField] private float blockedPenalty = -0.0025f;

        private const int BaseObservationCount = 16;

        private TankMovement2D movement;
        private TurretAim turretAim;
        private Weapon weapon;
        private Health health;
        private TankPerception2D perception;
        private BehaviorParameters behaviorParameters;
        private DecisionRequester decisionRequester;
        private Vector2 heuristicPatrolDirection;
        private float heuristicStrafeSign;
        private float nextHeuristicPatrolChangeTime;
        private float previousDistanceToTarget = -1f;

        protected override void Awake()
        {
            base.Awake();
            CacheComponents();
            heuristicStrafeSign = UnityEngine.Random.value >= 0.5f ? 1f : -1f;
            ChooseHeuristicPatrolDirection(true);
            ConfigureBehavior();
        }

        public override void Initialize()
        {
            previousDistanceToTarget = -1f;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            CacheComponents();
            SubscribeToSelf();
            SubscribeToTarget();
        }

        protected override void OnDisable()
        {
            UnsubscribeFromTarget();
            UnsubscribeFromSelf();
            base.OnDisable();
        }

        public void Configure(
            Transform newTarget,
            float newDetectionRange,
            float newAttackRange,
            float newPreferredDistance,
            float newRetreatDistance,
            float _,
            float __,
            float newStrafeStrength,
            ModelAsset model = null,
            string newBehaviorName = "TankArenaEnemy",
            int newDecisionPeriod = 5,
            EnemyAgentExecutionMode mode = EnemyAgentExecutionMode.Auto,
            TrainingArenaManager arenaManager = null)
        {
            CacheComponents();

            target = newTarget;
            targetHealth = target != null ? target.GetComponent<Health>() : null;
            trainingArena = arenaManager;
            executionMode = mode;
            behaviorName = newBehaviorName;
            trainedModel = model;
            decisionPeriod = Mathf.Max(1, newDecisionPeriod);
            detectionRange = Mathf.Max(0.5f, newDetectionRange);
            attackRange = Mathf.Max(0.5f, newAttackRange);
            preferredDistance = Mathf.Max(0f, newPreferredDistance);
            retreatDistance = Mathf.Max(0f, newRetreatDistance);
            strafeStrength = Mathf.Max(0f, newStrafeStrength);

            if (perception != null)
            {
                perception.Configure(null, 16, Mathf.Max(attackRange, 12f), detectionRange, true);
            }

            ConfigureBehavior();
            SubscribeToTarget();
        }

        public void SetTarget(Transform newTarget)
        {
            UnsubscribeFromTarget();
            target = newTarget;
            targetHealth = target != null ? target.GetComponent<Health>() : null;
            SubscribeToTarget();
        }

        public void SetTrainingArena(TrainingArenaManager arena)
        {
            trainingArena = arena;
        }

        public void ResetAgent(Vector2 position)
        {
            CacheComponents();
            transform.position = position;
            previousDistanceToTarget = -1f;
            heuristicStrafeSign = UnityEngine.Random.value >= 0.5f ? 1f : -1f;
            ChooseHeuristicPatrolDirection(true);
            movement.StopImmediate();

            if (health != null)
            {
                health.Revive();
            }

            enabled = true;
            RequestDecision();
        }

        public override void OnEpisodeBegin()
        {
            previousDistanceToTarget = -1f;
            ChooseHeuristicPatrolDirection(true);

            if (trainingArena != null)
            {
                trainingArena.ResetEpisode(this);
                return;
            }

            movement.StopImmediate();

            if (health != null && health.IsDead)
            {
                health.Revive();
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            CacheComponents();
            perception.Scan(target);

            Vector2 currentPosition = transform.position;
            Vector2 velocity = movement != null
                ? movement.Velocity / Mathf.Max(0.1f, movement.MoveSpeed)
                : Vector2.zero;
            Vector2 toTarget = target != null ? (Vector2)target.position - currentPosition : Vector2.zero;
            float distance = toTarget.magnitude;
            Vector2 directionToTarget = distance > 0.001f ? toTarget / distance : Vector2.zero;
            Vector2 turretForward = turretAim != null ? turretAim.Forward : Vector2.right;

            sensor.AddObservation(directionToTarget.x);
            sensor.AddObservation(directionToTarget.y);
            sensor.AddObservation(Mathf.Clamp01(distance / Mathf.Max(1f, detectionRange)));
            sensor.AddObservation(velocity.x);
            sensor.AddObservation(velocity.y);
            sensor.AddObservation(turretForward.x);
            sensor.AddObservation(turretForward.y);
            sensor.AddObservation(health != null ? health.CurrentHealth / health.MaxHealth : 0f);
            sensor.AddObservation(targetHealth != null && !targetHealth.IsDead ? targetHealth.CurrentHealth / targetHealth.MaxHealth : 0f);
            sensor.AddObservation(targetHealth != null && !targetHealth.IsDead ? 1f : 0f);
            sensor.AddObservation(perception.TargetDetected ? 1f : 0f);
            sensor.AddObservation(perception.HasLineOfSight ? 1f : 0f);
            sensor.AddObservation(distance <= attackRange ? 1f : 0f);
            sensor.AddObservation(weapon != null ? weapon.CooldownRemainingNormalized : 0f);
            sensor.AddObservation(weapon != null && weapon.CanFire ? 1f : 0f);
            sensor.AddObservation(Mathf.Clamp01(perception.TimeSinceLastSeen / Mathf.Max(0.1f, searchDuration)));

            for (int index = 0; index < perception.RaySamples.Count; index++)
            {
                TankPerception2D.RaySample sample = perception.RaySamples[index];
                sensor.AddObservation(sample.NormalizedDistance);
                sensor.AddObservation(sample.HitType == PerceptionHitType.Target ? 1f : 0f);
                sensor.AddObservation(sample.HitType == PerceptionHitType.Obstacle ? 1f : 0f);
                sensor.AddObservation(sample.HitType == PerceptionHitType.Boundary ? 1f : 0f);
                sensor.AddObservation(sample.HitType == PerceptionHitType.OtherActor ? 1f : 0f);
            }
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (health == null || health.IsDead)
            {
                movement.SetMoveInput(Vector2.zero);
                return;
            }

            perception.Scan(target);

            Vector2 moveInput = new Vector2(
                DecodeBranch(actions.DiscreteActions[0]),
                DecodeBranch(actions.DiscreteActions[1]));

            Vector2 aimInput = new Vector2(
                DecodeBranch(actions.DiscreteActions[2]),
                DecodeBranch(actions.DiscreteActions[3]));

            int fireAction = actions.DiscreteActions[4];
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            movement.SetMoveInput(moveInput);

            if (aimInput.sqrMagnitude > 0.01f)
            {
                turretAim.AimInDirection(aimInput.normalized);
            }

            Vector2 toTarget = target != null ? (Vector2)target.position - (Vector2)transform.position : Vector2.zero;
            float distanceToTarget = toTarget.magnitude;

            if (fireAction == 1 && weapon != null && weapon.TryFire(turretAim.Forward))
            {
                if (perception.HasLineOfSight &&
                    distanceToTarget <= attackRange &&
                    distanceToTarget > 0.001f &&
                    Vector2.Dot(turretAim.Forward, toTarget / distanceToTarget) > 0.9f)
                {
                    AddReward(successfulShotReward);
                }
                else
                {
                    AddReward(-successfulShotReward * 0.2f);
                }
            }

            ApplyDenseRewards(moveInput, distanceToTarget);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<int> actions = actionsOut.DiscreteActions;
            perception.Scan(target);

            Vector2 move = Vector2.zero;
            Vector2 aim = turretAim != null ? turretAim.Forward : Vector2.right;
            bool shouldFire = false;

            if (target != null && targetHealth != null && !targetHealth.IsDead)
            {
                Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;

                if (perception.TargetDetected)
                {
                    move = EnemySteeringUtility.BuildCombatMove(
                        perception,
                        toTarget,
                        attackRange,
                        preferredDistance,
                        retreatDistance,
                        heuristicStrafeSign,
                        strafeStrength);

                    aim = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : aim;
                    shouldFire = perception.HasLineOfSight && perception.TargetDistance <= attackRange;
                }
                else if (perception.HasLastKnownTarget && perception.TimeSinceLastSeen <= searchDuration)
                {
                    Vector2 toLastKnown = perception.LastKnownTargetPosition - (Vector2)transform.position;
                    move = EnemySteeringUtility.BuildSearchMove(perception, toLastKnown);
                    aim = toLastKnown.sqrMagnitude > 0.01f ? toLastKnown.normalized : aim;
                }
                else
                {
                    if (Time.time >= nextHeuristicPatrolChangeTime || perception.GetNormalizedClearance(heuristicPatrolDirection) < 0.28f)
                    {
                        ChooseHeuristicPatrolDirection(false);
                    }

                    move = EnemySteeringUtility.BuildPatrolMove(perception, heuristicPatrolDirection, 0.55f);
                    aim = move.sqrMagnitude > 0.01f ? move.normalized : heuristicPatrolDirection;
                }
            }

            actions[0] = EncodeBranch(move.x);
            actions[1] = EncodeBranch(move.y);
            actions[2] = EncodeBranch(aim.x);
            actions[3] = EncodeBranch(aim.y);
            actions[4] = shouldFire ? 1 : 0;
        }

        private void ApplyDenseRewards(Vector2 moveInput, float distanceToTarget)
        {
            AddReward(stepPenalty);

            if (target == null || targetHealth == null || targetHealth.IsDead)
            {
                previousDistanceToTarget = -1f;
                return;
            }

            if (previousDistanceToTarget >= 0f && !perception.HasLineOfSight)
            {
                float distanceDelta = previousDistanceToTarget - distanceToTarget;
                AddReward(Mathf.Clamp(distanceDelta * approachRewardScale, -0.01f, 0.01f));
            }

            if (perception.HasLineOfSight)
            {
                float distanceWindow = Mathf.Max(1f, attackRange);
                float rangeQuality = 1f - Mathf.Clamp01(Mathf.Abs(distanceToTarget - preferredDistance) / distanceWindow);
                AddReward(rangeQuality * lineOfSightReward);

                if (distanceToTarget > 0.001f)
                {
                    Vector2 targetDirection = ((Vector2)target.position - (Vector2)transform.position) / distanceToTarget;
                    float aimAlignment = Mathf.Max(0f, Vector2.Dot(turretAim.Forward, targetDirection));
                    AddReward(aimAlignment * aimAlignmentReward);
                }
            }

            if (moveInput.sqrMagnitude > 0.25f && movement.Velocity.sqrMagnitude < 0.025f)
            {
                AddReward(blockedPenalty);
            }

            previousDistanceToTarget = distanceToTarget;
        }

        private void ConfigureBehavior()
        {
            CacheComponents();

            if (behaviorParameters == null || decisionRequester == null || perception == null)
            {
                return;
            }

            var brainParameters = behaviorParameters.BrainParameters;
            brainParameters.VectorObservationSize = BaseObservationCount + perception.RayCount * 5;
            brainParameters.NumStackedVectorObservations = 1;
            brainParameters.ActionSpec = ActionSpec.MakeDiscrete(3, 3, 3, 3, 2);

            behaviorParameters.BehaviorName = behaviorName;
            behaviorParameters.BehaviorType = ResolveBehaviorType();
            behaviorParameters.Model = executionMode == EnemyAgentExecutionMode.Training ? null : trainedModel;
            behaviorParameters.DeterministicInference = deterministicInference;
            behaviorParameters.TeamId = 1;

            decisionRequester.DecisionPeriod = Mathf.Max(1, decisionPeriod);
            decisionRequester.DecisionStep = 0;
            decisionRequester.TakeActionsBetweenDecisions = true;
        }

        private BehaviorType ResolveBehaviorType()
        {
            return executionMode switch
            {
                EnemyAgentExecutionMode.HeuristicOnly => BehaviorType.HeuristicOnly,
                EnemyAgentExecutionMode.InferenceOnly => BehaviorType.InferenceOnly,
                _ => BehaviorType.Default
            };
        }

        private void SubscribeToSelf()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= HandleSelfDamaged;
            health.Died -= HandleSelfDeath;
            health.Damaged += HandleSelfDamaged;
            health.Died += HandleSelfDeath;
        }

        private void UnsubscribeFromSelf()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= HandleSelfDamaged;
            health.Died -= HandleSelfDeath;
        }

        private void SubscribeToTarget()
        {
            if (targetHealth == null)
            {
                return;
            }

            targetHealth.Damaged -= HandleTargetDamaged;
            targetHealth.Died -= HandleTargetDeath;
            targetHealth.Damaged += HandleTargetDamaged;
            targetHealth.Died += HandleTargetDeath;
        }

        private void UnsubscribeFromTarget()
        {
            if (targetHealth == null)
            {
                return;
            }

            targetHealth.Damaged -= HandleTargetDamaged;
            targetHealth.Died -= HandleTargetDeath;
        }

        private void HandleSelfDamaged(Health _, DamageInfo __)
        {
            AddReward(selfDamagePenalty);
        }

        private void HandleSelfDeath(Health _, DamageInfo __)
        {
            movement.StopImmediate();
            AddReward(deathPenalty);
            EndEpisode();
        }

        private void HandleTargetDamaged(Health _, DamageInfo damage)
        {
            if (damage.Source == gameObject)
            {
                AddReward(targetDamageReward);
            }
        }

        private void HandleTargetDeath(Health _, DamageInfo damage)
        {
            if (damage.Source == gameObject)
            {
                AddReward(targetKillReward);
            }

            EndEpisode();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision == null || collision.collider == null)
            {
                return;
            }

            if (collision.collider.GetComponentInParent<ArenaObstacle>() != null)
            {
                AddReward(obstacleCollisionPenalty);
            }
        }

        private void CacheComponents()
        {
            movement ??= GetComponent<TankMovement2D>();
            turretAim ??= GetComponent<TurretAim>();
            weapon ??= GetComponent<Weapon>();
            health ??= GetComponent<Health>();
            perception ??= GetComponent<TankPerception2D>();
            behaviorParameters ??= GetComponent<BehaviorParameters>();
            decisionRequester ??= GetComponent<DecisionRequester>();
        }

        private void ChooseHeuristicPatrolDirection(bool immediate)
        {
            heuristicPatrolDirection = UnityEngine.Random.insideUnitCircle.normalized;

            if (heuristicPatrolDirection.sqrMagnitude < 0.0001f)
            {
                heuristicPatrolDirection = Vector2.right;
            }

            nextHeuristicPatrolChangeTime = Time.time + (immediate ? 0.4f : patrolChangeInterval);
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
    }
}
