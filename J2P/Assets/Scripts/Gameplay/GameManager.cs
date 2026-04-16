using System.Collections;
using UnityEngine;

namespace TankArena2D
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private ArenaBounds arenaBounds;
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] private PlayerController player;
        [SerializeField, Min(0)] private int initialEnemyCount = 6;
        [SerializeField, Min(0)] private int maxAliveEnemies = 6;
        [SerializeField, Min(0.1f)] private float enemyRespawnDelay = 3f;
        [SerializeField, Min(0f)] private float playerRespawnDelay = 2.5f;
        [SerializeField, Min(1)] private int maxPlayerLives = 3;
        [SerializeField, Min(1)] private int baseKillScore = 100;
        [SerializeField, Min(0f)] private float extraScorePerSurvivalSecond = 4f;

        private Coroutine playerRespawnRoutine;
        private bool subscriptionsActive;
        private bool sessionSubmitted;

        public int TotalKills { get; private set; }
        public int PlayerKillCount { get; private set; }
        public int Score { get; private set; }
        public int RemainingLives { get; private set; }
        public int MaxPlayerLives => maxPlayerLives;
        public float SurvivalTime { get; private set; }
        public float RespawnCountdown { get; private set; }
        public float EnemyRespawnCountdown => spawnManager != null ? spawnManager.RemainingRespawnTime : 0f;
        public bool IsGameOver { get; private set; }
        public PlayerController Player => player;
        public SpawnManager Spawner => spawnManager;

        private void OnEnable()
        {
            HookSubscriptions();
        }

        private void OnDisable()
        {
            if (spawnManager != null)
            {
                spawnManager.StopAutoRespawn();
            }

            UnhookSubscriptions();
        }

        private void OnDestroy()
        {
            SubmitSessionStats();
        }

        private void Start()
        {
            ProfileService.EnsureInstance().BeginMatch();
            RemainingLives = maxPlayerLives;
            IsGameOver = false;

            if (spawnManager != null)
            {
                spawnManager.ConfigureRespawn(initialEnemyCount, maxAliveEnemies, enemyRespawnDelay, true);
                spawnManager.StartAutoRespawn();
            }
        }

        private void Update()
        {
            if (IsGameOver)
            {
                return;
            }

            SurvivalTime += Time.deltaTime;
        }

        public void Configure(
            ArenaBounds bounds,
            SpawnManager spawner,
            PlayerController playerController,
            int initialEnemies,
            int maxEnemies,
            float enemyDelay,
            float playerDelay,
            int playerLives = 3,
            int killScore = 100,
            float bonusPerSecond = 4f)
        {
            UnhookSubscriptions();

            arenaBounds = bounds;
            spawnManager = spawner;
            player = playerController;
            initialEnemyCount = Mathf.Max(0, initialEnemies);
            maxAliveEnemies = Mathf.Max(initialEnemyCount, maxEnemies);
            enemyRespawnDelay = Mathf.Max(0.1f, enemyDelay);
            playerRespawnDelay = Mathf.Max(0f, playerDelay);
            maxPlayerLives = Mathf.Max(1, playerLives);
            baseKillScore = Mathf.Max(1, killScore);
            extraScorePerSurvivalSecond = Mathf.Max(0f, bonusPerSecond);
            RemainingLives = maxPlayerLives;
            IsGameOver = false;

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

        private void HandleEnemyKilled(GameObject _, DamageInfo damage)
        {
            if (IsGameOver)
            {
                return;
            }

            TotalKills++;

            if (player != null && damage.Source == player.gameObject)
            {
                PlayerKillCount++;
                Score += CalculateKillScore();
            }
        }

        private void HandlePlayerDied(Health _, DamageInfo __)
        {
            if (IsGameOver)
            {
                return;
            }

            RemainingLives = Mathf.Max(0, RemainingLives - 1);

            if (RemainingLives <= 0)
            {
                TriggerGameOver();
                return;
            }

            if (playerRespawnRoutine != null)
            {
                StopCoroutine(playerRespawnRoutine);
            }

            playerRespawnRoutine = StartCoroutine(RespawnPlayerAfterDelay());
        }

        private IEnumerator RespawnPlayerAfterDelay()
        {
            RespawnCountdown = playerRespawnDelay;

            while (RespawnCountdown > 0f && !IsGameOver)
            {
                RespawnCountdown -= Time.deltaTime;
                yield return null;
            }

            RespawnCountdown = 0f;

            if (!IsGameOver)
            {
                RespawnPlayer();
            }

            playerRespawnRoutine = null;
        }

        private int CalculateKillScore()
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseKillScore + SurvivalTime * extraScorePerSurvivalSecond));
        }

        private void RespawnPlayer()
        {
            if (player == null || IsGameOver)
            {
                return;
            }

            Vector2 spawnPosition = arenaBounds != null
                ? arenaBounds.ClampInside(Vector2.zero, 2f)
                : Vector2.zero;

            player.RespawnAt(spawnPosition);
        }

        private void TriggerGameOver()
        {
            if (IsGameOver)
            {
                return;
            }

            IsGameOver = true;
            RespawnCountdown = 0f;

            if (playerRespawnRoutine != null)
            {
                StopCoroutine(playerRespawnRoutine);
                playerRespawnRoutine = null;
            }

            if (spawnManager != null)
            {
                spawnManager.StopAutoRespawn();
            }

            SubmitSessionStats();
            Time.timeScale = 0f;
        }

        private void SubmitSessionStats()
        {
            if (sessionSubmitted)
            {
                return;
            }

            sessionSubmitted = true;
            ProfileService.EnsureInstance().EndMatch(Score, SurvivalTime, PlayerKillCount, true);
        }
    }
}
