using System.Collections;
using UnityEngine;

namespace TankArena2D
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private ArenaBounds arenaBounds;
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] private PlayerController player;
        [SerializeField, Min(1)] private int startingWaveSize = 4;
        [SerializeField, Min(0)] private int waveIncreasePerWave = 2;
        [SerializeField, Min(0f)] private float timeBetweenWaves = 2.5f;
        [SerializeField, Min(0f)] private float playerRespawnDelay = 3f;

        private Coroutine nextWaveRoutine;
        private Coroutine playerRespawnRoutine;
        private bool subscriptionsActive;

        public int CurrentWave { get; private set; }
        public int TotalKills { get; private set; }
        public float RespawnCountdown { get; private set; }
        public float NextWaveCountdown { get; private set; }
        public PlayerController Player => player;
        public SpawnManager Spawner => spawnManager;

        private void OnEnable()
        {
            HookSubscriptions();
        }

        private void OnDisable()
        {
            UnhookSubscriptions();
        }

        private void Start()
        {
            if (CurrentWave == 0)
            {
                BeginNextWave();
            }
        }

        public void Configure(
            ArenaBounds bounds,
            SpawnManager spawner,
            PlayerController playerController,
            int initialWaveSize,
            int waveIncrease,
            float waveDelay,
            float respawnDelay)
        {
            UnhookSubscriptions();

            arenaBounds = bounds;
            spawnManager = spawner;
            player = playerController;
            startingWaveSize = Mathf.Max(1, initialWaveSize);
            waveIncreasePerWave = Mathf.Max(0, waveIncrease);
            timeBetweenWaves = Mathf.Max(0f, waveDelay);
            playerRespawnDelay = Mathf.Max(0f, respawnDelay);

            HookSubscriptions();
        }

        public void RegisterPlayer(PlayerController controller)
        {
            UnhookSubscriptions();
            player = controller;
            HookSubscriptions();

            if (spawnManager != null)
            {
                spawnManager.SetPlayerTarget(player != null ? player.transform : null);
            }
        }

        private void HookSubscriptions()
        {
            if (subscriptionsActive)
            {
                return;
            }

            if (spawnManager != null)
            {
                spawnManager.EnemyKilled += HandleEnemyKilled;
            }

            if (player != null && player.Health != null)
            {
                player.Health.Died += HandlePlayerDied;
            }

            subscriptionsActive = true;
        }

        private void UnhookSubscriptions()
        {
            if (!subscriptionsActive)
            {
                return;
            }

            if (spawnManager != null)
            {
                spawnManager.EnemyKilled -= HandleEnemyKilled;
            }

            if (player != null && player.Health != null)
            {
                player.Health.Died -= HandlePlayerDied;
            }

            subscriptionsActive = false;
        }

        private void HandleEnemyKilled(GameObject enemy)
        {
            TotalKills++;

            if (spawnManager != null &&
                spawnManager.ActiveEnemyCount == 0 &&
                !spawnManager.IsSpawningWave)
            {
                if (nextWaveRoutine != null)
                {
                    StopCoroutine(nextWaveRoutine);
                }

                nextWaveRoutine = StartCoroutine(BeginNextWaveAfterDelay());
            }
        }

        private void HandlePlayerDied(Health _, DamageInfo __)
        {
            if (playerRespawnRoutine != null)
            {
                StopCoroutine(playerRespawnRoutine);
            }

            playerRespawnRoutine = StartCoroutine(RespawnPlayerAfterDelay());
        }

        private IEnumerator BeginNextWaveAfterDelay()
        {
            NextWaveCountdown = timeBetweenWaves;

            while (NextWaveCountdown > 0f)
            {
                NextWaveCountdown -= Time.deltaTime;
                yield return null;
            }

            NextWaveCountdown = 0f;
            BeginNextWave();
            nextWaveRoutine = null;
        }

        private IEnumerator RespawnPlayerAfterDelay()
        {
            RespawnCountdown = playerRespawnDelay;

            while (RespawnCountdown > 0f)
            {
                RespawnCountdown -= Time.deltaTime;
                yield return null;
            }

            RespawnCountdown = 0f;
            RespawnPlayer();
            playerRespawnRoutine = null;
        }

        private void BeginNextWave()
        {
            CurrentWave++;

            int enemyCount = startingWaveSize + (CurrentWave - 1) * waveIncreasePerWave;
            spawnManager?.SpawnWave(enemyCount);
        }

        private void RespawnPlayer()
        {
            if (player == null)
            {
                return;
            }

            Vector2 spawnPosition = arenaBounds != null
                ? arenaBounds.ClampInside(Vector2.zero, 2f)
                : Vector2.zero;

            player.RespawnAt(spawnPosition);
        }
    }
}
