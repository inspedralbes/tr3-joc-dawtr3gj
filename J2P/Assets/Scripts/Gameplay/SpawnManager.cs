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
        [SerializeField, Min(0)] private int initialEnemyCount = 6;
        [SerializeField, Min(0)] private int desiredAliveEnemies = 6;
        [SerializeField, Min(0.1f)] private float respawnDelay = 3f;
        [SerializeField] private bool autoRespawn = true;

        private readonly HashSet<GameObject> activeEnemies = new HashSet<GameObject>();
        private Coroutine populationRoutine;

        public event Action<GameObject> EnemySpawned;
        public event Action<GameObject, DamageInfo> EnemyKilled;

        public int ActiveEnemyCount => activeEnemies.Count;
        public IReadOnlyCollection<GameObject> ActiveEnemies => activeEnemies;
        public int DesiredAliveEnemies => desiredAliveEnemies;
        public float RemainingRespawnTime { get; private set; }

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

        public void ConfigureRespawn(int initialCount, int desiredCount, float delay, bool enableAutoRespawn)
        {
            initialEnemyCount = Mathf.Max(0, initialCount);
            desiredAliveEnemies = Mathf.Max(initialEnemyCount, desiredCount);
            respawnDelay = Mathf.Max(0.1f, delay);
            autoRespawn = enableAutoRespawn;
        }

        public void SetPlayerTarget(Transform target)
        {
            playerTarget = target;
        }

        public void StartAutoRespawn()
        {
            StopAutoRespawn();

            if (!autoRespawn)
            {
                return;
            }

            populationRoutine = StartCoroutine(MaintainPopulationRoutine());
        }

        public void StopAutoRespawn()
        {
            if (populationRoutine != null)
            {
                StopCoroutine(populationRoutine);
                populationRoutine = null;
            }

            RemainingRespawnTime = 0f;
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

                for (int index = 0; index < overlaps.Length; index++)
                {
                    Collider2D overlap = overlaps[index];

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

        private IEnumerator MaintainPopulationRoutine()
        {
            while (activeEnemies.Count < initialEnemyCount)
            {
                SpawnEnemy();

                if (spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(spawnInterval);
                }
                else
                {
                    yield return null;
                }
            }

            while (autoRespawn)
            {
                if (activeEnemies.Count >= desiredAliveEnemies)
                {
                    RemainingRespawnTime = 0f;
                    yield return null;
                    continue;
                }

                RemainingRespawnTime = respawnDelay;

                while (RemainingRespawnTime > 0f && activeEnemies.Count < desiredAliveEnemies)
                {
                    RemainingRespawnTime -= Time.deltaTime;
                    yield return null;
                }

                RemainingRespawnTime = 0f;

                if (activeEnemies.Count < desiredAliveEnemies)
                {
                    SpawnEnemy();

                    if (spawnInterval > 0f)
                    {
                        yield return new WaitForSeconds(spawnInterval);
                    }
                }
            }
        }

        private void HandleEnemyDeath(Health health, DamageInfo damage)
        {
            health.Died -= HandleEnemyDeath;
            GameObject enemyObject = health.gameObject;
            activeEnemies.Remove(enemyObject);
            EnemyKilled?.Invoke(enemyObject, damage);
            Destroy(enemyObject);
        }

        private static IEnemyAgent ResolveEnemyAgent(GameObject enemyObject)
        {
            MonoBehaviour[] components = enemyObject.GetComponents<MonoBehaviour>();

            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] is IEnemyAgent enemyAgent)
                {
                    return enemyAgent;
                }
            }

            return null;
        }
    }
}
