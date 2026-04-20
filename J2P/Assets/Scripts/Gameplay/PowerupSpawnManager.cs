using System.Collections.Generic;
using UnityEngine;

namespace TankArena2D
{
    public sealed class PowerupSpawnManager : MonoBehaviour
    {
        [SerializeField] private ArenaBounds arenaBounds;
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform powerupContainer;
        [SerializeField, Min(1)] private int spawnAttempts = 40;
        [SerializeField, Min(0.1f)] private float spawnCheckRadius = 0.9f;
        [SerializeField, Min(0f)] private float spawnPadding = 2.5f;
        [SerializeField, Min(0.01f)] private float spawnInterval = 12f;
        [SerializeField, Min(1)] private int maxActivePowerups = 3;

        private readonly Dictionary<string, PowerupPickup> activePowerups = new Dictionary<string, PowerupPickup>();
        private bool networkedMode;
        private bool isSpawnAuthority;
        private float nextSpawnTime;

        public int ActivePowerupCount => activePowerups.Count;

        public void Configure(ArenaBounds bounds, PlayerController playerController, bool useNetworkedMode)
        {
            arenaBounds = bounds;
            player = playerController;
            networkedMode = useNetworkedMode;
        }

        public void SetPlayer(PlayerController playerController)
        {
            player = playerController;
        }

        public void SetSpawnAuthority(bool canSpawn)
        {
            isSpawnAuthority = canSpawn;

            if (canSpawn)
            {
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        public void StartSpawning()
        {
            CleanupMissingReferences();
            nextSpawnTime = Time.time + spawnInterval;
        }

        public void StopSpawning()
        {
            CleanupMissingReferences();
        }

        private void Awake()
        {
            if (arenaBounds == null)
            {
                arenaBounds = FindAnyObjectByType<ArenaBounds>();
            }

            if (player == null)
            {
                player = FindAnyObjectByType<PlayerController>();
            }

            if (powerupContainer == null)
            {
                GameObject container = new GameObject("Powerups");
                container.transform.SetParent(transform, false);
                powerupContainer = container.transform;
            }
        }

        private void OnEnable()
        {
            StartSpawning();
        }

        private void Update()
        {
            CleanupMissingReferences();

            if (arenaBounds == null || player == null || !player.IsAlive)
            {
                return;
            }

            if (networkedMode)
            {
                if (!isSpawnAuthority || ActivePowerupCount >= maxActivePowerups || Time.time < nextSpawnTime)
                {
                    return;
                }

                if (!TryFindSpawnPosition(out Vector2 networkPosition))
                {
                    nextSpawnTime = Time.time + spawnInterval;
                    return;
                }

                MultiplayerClient.Active?.ReportPowerupSpawn(GetRandomPowerupType(), networkPosition);
                nextSpawnTime = Time.time + spawnInterval;
                return;
            }

            if (ActivePowerupCount >= maxActivePowerups || Time.time < nextSpawnTime)
            {
                return;
            }

            SpawnPowerup(GetRandomPowerupType(), null, null);
            nextSpawnTime = Time.time + spawnInterval;
        }

        public void SyncPowerups(IEnumerable<MultiplayerClient.PowerupSnapshot> powerups)
        {
            HashSet<string> seenIds = new HashSet<string>();

            if (powerups != null)
            {
                foreach (MultiplayerClient.PowerupSnapshot snapshot in powerups)
                {
                    if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.id))
                    {
                        continue;
                    }

                    seenIds.Add(snapshot.id);

                    if (!TryParseType(snapshot.powerupType, out PowerupType type))
                    {
                        continue;
                    }

                    if (!activePowerups.ContainsKey(snapshot.id))
                    {
                        SpawnPowerup(type, snapshot.id, new Vector2(snapshot.x, snapshot.y));
                    }
                }
            }

            List<string> staleIds = null;

            foreach (KeyValuePair<string, PowerupPickup> entry in activePowerups)
            {
                if (!seenIds.Contains(entry.Key))
                {
                    staleIds ??= new List<string>();
                    staleIds.Add(entry.Key);
                }
            }

            if (staleIds == null)
            {
                return;
            }

            for (int i = 0; i < staleIds.Count; i++)
            {
                RemovePowerup(staleIds[i]);
            }
        }

        public void SyncSpawnPowerup(string id, PowerupType type, Vector2 position)
        {
            if (string.IsNullOrWhiteSpace(id) || activePowerups.ContainsKey(id))
            {
                return;
            }

            SpawnPowerup(type, id, position);
        }

        public void SyncCollectedPowerup(string id, bool collectedByLocalPlayer, PowerupType type)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return;
            }

            if (collectedByLocalPlayer && player != null)
            {
                PlayerPowerupController powerupController = player.GetComponent<PlayerPowerupController>();

                if (powerupController == null)
                {
                    powerupController = player.gameObject.AddComponent<PlayerPowerupController>();
                }

                powerupController.Apply(type);
            }

            RemovePowerup(id);
        }

        private void SpawnPowerup(PowerupType type, string id, Vector2? forcedPosition)
        {
            Vector2 position;

            if (forcedPosition.HasValue)
            {
                position = forcedPosition.Value;
            }
            else if (!TryFindSpawnPosition(out position))
            {
                return;
            }

            GameObject powerupObject = new GameObject("Powerup");
            powerupObject.transform.SetParent(powerupContainer, false);
            powerupObject.transform.position = position;

            PowerupPickup pickup = powerupObject.AddComponent<PowerupPickup>();
            pickup.Initialize(id, type, networkedMode ? HandleOnlineCollected : HandleOfflineCollected);
            activePowerups[pickup.PowerupId] = pickup;
        }

        private bool TryFindSpawnPosition(out Vector2 position)
        {
            position = Vector2.zero;

            if (arenaBounds == null)
            {
                return false;
            }

            for (int attempt = 0; attempt < spawnAttempts; attempt++)
            {
                Vector2 candidate = arenaBounds.GetRandomPoint(spawnPadding);

                if (!arenaBounds.Contains(candidate, spawnCheckRadius))
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

        private void HandleOfflineCollected(PowerupPickup pickup, PlayerController collector)
        {
            if (pickup == null || collector == null)
            {
                return;
            }

            PlayerPowerupController powerupController = collector.GetComponent<PlayerPowerupController>();

            if (powerupController == null)
            {
                powerupController = collector.gameObject.AddComponent<PlayerPowerupController>();
            }

            powerupController.Apply(pickup.PowerupType);
            RemovePowerup(pickup.PowerupId);
        }

        private void HandleOnlineCollected(PowerupPickup pickup, PlayerController collector)
        {
            if (pickup == null || collector == null || MultiplayerClient.Active == null)
            {
                return;
            }

            pickup.SetCollectable(false);
            MultiplayerClient.Active.ReportPowerupCollected(pickup.PowerupId);
        }

        private void CleanupMissingReferences()
        {
            List<string> destroyedIds = null;

            foreach (KeyValuePair<string, PowerupPickup> entry in activePowerups)
            {
                if (entry.Value == null)
                {
                    destroyedIds ??= new List<string>();
                    destroyedIds.Add(entry.Key);
                }
            }

            if (destroyedIds == null)
            {
                return;
            }

            for (int i = 0; i < destroyedIds.Count; i++)
            {
                activePowerups.Remove(destroyedIds[i]);
            }
        }

        private static PowerupType GetRandomPowerupType()
        {
            return (PowerupType)Random.Range(0, 3);
        }

        private void RemovePowerup(string id)
        {
            if (!activePowerups.TryGetValue(id, out PowerupPickup pickup))
            {
                return;
            }

            activePowerups.Remove(id);

            if (pickup != null)
            {
                Destroy(pickup.gameObject);
            }
        }

        private static bool TryParseType(string rawType, out PowerupType type)
        {
            if (!string.IsNullOrWhiteSpace(rawType) &&
                System.Enum.TryParse(rawType, true, out type))
            {
                return true;
            }

            type = PowerupType.Heal;
            return false;
        }
    }
}
