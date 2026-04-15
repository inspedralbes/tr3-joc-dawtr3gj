using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TankArena2D
{
    public sealed class SpawnManager : MonoBehaviour
    {
        [SerializeField] private ArenaBounds arenaBounds;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform enemyContainer;
        [SerializeField, Min(1)] private int spawnAttempts = 40;
        [SerializeField, Min(0.1f)] private float spawnCheckRadius = 0.9f;
        [SerializeField, Min(0f)] private float minDistanceFromPlayer = 8f;
        [SerializeField, Min(0f)] private float spawnPadding = 2f;
        [SerializeField, Min(0.01f)] private float spawnInterval = 0.35f;

        private readonly HashSet<GameObject> activeEnemies = new HashSet<GameObject>();
        private Coroutine currentSpawnRoutine;

        public event Action<GameObject> EnemySpawned;
        public event Action<GameObject> EnemyKilled;

        public int ActiveEnemyCount => activeEnemies.Count;
        public bool IsSpawningWave => currentSpawnRoutine != null;
        public IReadOnlyCollection<GameObject> ActiveEnemies => activeEnemies;

        public void Configure(
            ArenaBounds bounds,
            GameObject prefab,
            Transform target,
            float minPlayerDistance,
            float padding,
            float checkRadius,
            float interval,
            Transform container)
        {
            arenaBounds = bounds;
            enemyPrefab = prefab;
            playerTarget = target;
            minDistanceFromPlayer = Mathf.Max(0f, minPlayerDistance);
            spawnPadding = Mathf.Max(0f, padding);
            spawnCheckRadius = Mathf.Max(0.1f, checkRadius);
            spawnInterval = Mathf.Max(0.01f, interval);
            enemyContainer = container;
        }

        public void SetPlayerTarget(Transform target)
        {
            playerTarget = target;
        }

        public void CancelPendingSpawns()
        {
            if (currentSpawnRoutine != null)
            {
                StopCoroutine(currentSpawnRoutine);
                currentSpawnRoutine = null;
            }
        }

        public void SpawnWave(int count)
        {
            CancelPendingSpawns();
            currentSpawnRoutine = StartCoroutine(SpawnWaveRoutine(Mathf.Max(0, count)));
        }

        public GameObject SpawnEnemy()
        {
            if (enemyPrefab == null)
            {
                return null;
            }

            Vector2 spawnPosition = TryFindSpawnPosition(out Vector2 foundPosition)
                ? foundPosition
                : arenaBounds != null
                    ? arenaBounds.GetRandomPoint(2f)
                    : Vector2.zero;

            GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemyContainer);
            enemyObject.SetActive(true);

            IEnemyAgent enemyAgent = ResolveEnemyAgent(enemyObject);

            if (enemyAgent == null)
            {
                Destroy(enemyObject);
                return null;
            }

            enemyAgent.SetTarget(playerTarget);
            enemyAgent.ResetAgent(spawnPosition);

            Health health = enemyObject.GetComponent<Health>();

            if (health != null)
            {
                health.Died += HandleEnemyDeath;
            }

            activeEnemies.Add(enemyObject);
            EnemySpawned?.Invoke(enemyObject);
            return enemyObject;
        }

        public bool TryFindSpawnPosition(out Vector2 position)
        {
            position = Vector2.zero;

            if (arenaBounds == null)
            {
                return false;
            }

            float minDistanceSquared = minDistanceFromPlayer * minDistanceFromPlayer;

            for (int attempt = 0; attempt < spawnAttempts; attempt++)
            {
                Vector2 candidate = arenaBounds.GetRandomPoint(spawnPadding);

                if (playerTarget != null &&
                    ((Vector2)playerTarget.position - candidate).sqrMagnitude < minDistanceSquared)
                {
                    continue;
                }

                Collider2D[] overlaps = Physics2D.OverlapCircleAll(candidate, spawnCheckRadius);
                bool blocked = false;

                for (int i = 0; i < overlaps.Length; i++)
                {
                    Collider2D overlap = overlaps[i];

                    if (overlap == null || overlap.isTrigger)
                    {
                        continue;
                    }

                    blocked = true;
                    break;
                }

                if (!blocked)
                {
                    position = candidate;
                    return true;
                }
            }

            return false;
        }

        private IEnumerator SpawnWaveRoutine(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy();

                if (i < count - 1)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            currentSpawnRoutine = null;
        }

        private void HandleEnemyDeath(Health health, DamageInfo damage)
        {
            health.Died -= HandleEnemyDeath;
            GameObject enemyObject = health.gameObject;

            activeEnemies.Remove(enemyObject);
            EnemyKilled?.Invoke(enemyObject);
            Destroy(enemyObject);
        }

        private static IEnemyAgent ResolveEnemyAgent(GameObject enemyObject)
        {
            MonoBehaviour[] components = enemyObject.GetComponents<MonoBehaviour>();

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IEnemyAgent enemyAgent)
                {
                    return enemyAgent;
                }
            }

            return null;
        }
    }
}
