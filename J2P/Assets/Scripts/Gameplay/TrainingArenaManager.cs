using UnityEngine;

namespace TankArena2D
{
    public sealed class TrainingArenaManager : MonoBehaviour
    {
        [SerializeField] private ArenaBounds arenaBounds;
        [SerializeField] private EnemyMlAgent enemyAgent;
        [SerializeField] private TrainingTargetController trainingTarget;
        [SerializeField, Min(0f)] private float spawnPadding = 2f;
        [SerializeField, Min(0f)] private float spawnCheckRadius = 0.8f;
        [SerializeField, Min(1f)] private float minimumSpawnSeparation = 12f;
        [SerializeField, Min(8)] private int spawnAttempts = 64;

        public void Configure(
            ArenaBounds bounds,
            EnemyMlAgent enemy,
            TrainingTargetController target,
            float padding,
            float checkRadius,
            float separation,
            int attempts)
        {
            arenaBounds = bounds;
            enemyAgent = enemy;
            trainingTarget = target;
            spawnPadding = Mathf.Max(0f, padding);
            spawnCheckRadius = Mathf.Max(0.1f, checkRadius);
            minimumSpawnSeparation = Mathf.Max(1f, separation);
            spawnAttempts = Mathf.Max(8, attempts);
            WireActors();
        }

        private void Awake()
        {
            WireActors();
        }

        public void ResetEpisode(EnemyMlAgent requester)
        {
            if (arenaBounds == null || enemyAgent == null || trainingTarget == null)
            {
                return;
            }

            if (!TryFindSpawnPair(out Vector2 enemySpawn, out Vector2 targetSpawn))
            {
                enemySpawn = arenaBounds.ClampInside(new Vector2(-6f, 0f), 2f);
                targetSpawn = arenaBounds.ClampInside(new Vector2(6f, 0f), 2f);
            }

            WireActors();
            trainingTarget.RespawnAt(targetSpawn);
            enemyAgent.ResetAgent(enemySpawn);
        }

        private bool TryFindSpawnPair(out Vector2 enemySpawn, out Vector2 targetSpawn)
        {
            enemySpawn = Vector2.zero;
            targetSpawn = Vector2.zero;

            for (int attempt = 0; attempt < spawnAttempts; attempt++)
            {
                Vector2 candidateEnemy = arenaBounds.GetRandomPoint(spawnPadding);
                Vector2 candidateTarget = arenaBounds.GetRandomPoint(spawnPadding);

                if ((candidateEnemy - candidateTarget).sqrMagnitude < minimumSpawnSeparation * minimumSpawnSeparation)
                {
                    continue;
                }

                if (IsBlocked(candidateEnemy) || IsBlocked(candidateTarget))
                {
                    continue;
                }

                enemySpawn = candidateEnemy;
                targetSpawn = candidateTarget;
                return true;
            }

            return false;
        }

        private bool IsBlocked(Vector2 position)
        {
            Collider2D[] overlaps = Physics2D.OverlapCircleAll(position, spawnCheckRadius);

            for (int index = 0; index < overlaps.Length; index++)
            {
                Collider2D overlap = overlaps[index];

                if (overlap == null || overlap.isTrigger)
                {
                    continue;
                }

                if (BelongsToTrackedActor(overlap))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private bool BelongsToTrackedActor(Collider2D overlap)
        {
            return overlap != null &&
                   ((enemyAgent != null && (overlap.transform == enemyAgent.transform || overlap.transform.IsChildOf(enemyAgent.transform))) ||
                    (trainingTarget != null && (overlap.transform == trainingTarget.transform || overlap.transform.IsChildOf(trainingTarget.transform))));
        }

        private void WireActors()
        {
            if (enemyAgent == null || trainingTarget == null)
            {
                return;
            }

            enemyAgent.SetTarget(trainingTarget.transform);
            trainingTarget.SetThreat(enemyAgent.transform);
        }
    }
}
