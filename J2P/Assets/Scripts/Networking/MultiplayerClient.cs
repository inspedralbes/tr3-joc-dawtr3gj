using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TankArena2D
{
    public sealed class MultiplayerClient : MonoBehaviour
    {
        [Serializable]
        private sealed class MessageBase
        {
            public string type;
        }

        [Serializable]
        private sealed class WelcomeEnvelope
        {
            public string type;
            public WelcomePayload payload;
        }

        [Serializable]
        private sealed class WelcomePayload
        {
            public string selfId;
            public PlayerSnapshot[] players;
            public long serverTime;
        }

        [Serializable]
        private sealed class PlayerEnvelope
        {
            public string type;
            public PlayerPayload payload;
        }

        [Serializable]
        private sealed class PlayerPayload
        {
            public PlayerSnapshot player;
        }

        [Serializable]
        private sealed class PlayerLeftEnvelope
        {
            public string type;
            public PlayerLeftPayload payload;
        }

        [Serializable]
        private sealed class PlayerLeftPayload
        {
            public string playerId;
        }

        [Serializable]
        private sealed class FireEnvelope
        {
            public string type;
            public FirePayload payload;
        }

        [Serializable]
        private sealed class FirePayload
        {
            public string playerId;
            public string projectileId;
            public float x;
            public float y;
            public float dirX;
            public float dirY;
            public float speed;
            public float damage;
            public float ttl;
            public long createdAt;
        }

        [Serializable]
        private sealed class DamageEnvelope
        {
            public string type;
            public DamagePayload payload;
        }

        [Serializable]
        private sealed class DamagePayload
        {
            public string attackerId;
            public string targetId;
            public float amount;
            public string projectileId;
            public float targetHp;
            public bool targetAlive;
            public int attackerKills;
            public int attackerScore;
        }

        [Serializable]
        private sealed class PlayerSnapshot
        {
            public string id;
            public string userId;
            public string username;
            public float x;
            public float y;
            public float bodyAngle;
            public float turretAngle;
            public float hp;
            public float maxHp;
            public bool alive;
            public int kills;
            public int score;
            public long lastUpdateAt;
        }

        public static MultiplayerClient Active { get; private set; }

        [SerializeField] private GameManager gameManager;
        [SerializeField] private ArenaBounds arenaBounds;
        [SerializeField] private PlayerController localPlayer;
        [SerializeField] private Transform remotePlayersRoot;
        [SerializeField] private float stateSendInterval = 0.05f;

        private readonly ConcurrentQueue<string> inboundMessages = new();
        private readonly Dictionary<string, RemotePlayerAvatar> remotePlayers = new();
        private readonly Dictionary<string, NetworkActor> actorIndex = new();

        private ClientWebSocket socket;
        private CancellationTokenSource cancellationSource;
        private Weapon localWeapon;
        private NetworkActor localActor;
        private string selfId;
        private float nextStateSendAt;
        private bool isConnected;
        private bool isConnecting;
        private bool fireSubscribed;
        private string disconnectReason = string.Empty;

        public int RemotePlayerCount => remotePlayers.Count;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            if (Active != null && Active != this)
            {
                Destroy(this);
                return;
            }

            Active = this;
            ResolveDependencies();

            if (localWeapon != null && !fireSubscribed)
            {
                localWeapon.Fired += HandleLocalShot;
                fireSubscribed = true;
            }

            if (ProfileService.Instance.SelectedMatchMode == MatchMode.OnlinePvP &&
                ProfileService.Instance.HasAuthenticatedSession)
            {
                _ = ConnectAsync();
            }
        }

        private void OnDisable()
        {
            if (localWeapon != null && fireSubscribed)
            {
                localWeapon.Fired -= HandleLocalShot;
                fireSubscribed = false;
            }

            _ = DisconnectAsync();

            if (Active == this)
            {
                Active = null;
            }
        }

        private void Update()
        {
            while (inboundMessages.TryDequeue(out string rawMessage))
            {
                HandleServerMessage(rawMessage);
            }

            if (!isConnected || localPlayer == null || localPlayer.Health == null)
            {
                return;
            }

            if (Time.unscaledTime >= nextStateSendAt)
            {
                nextStateSendAt = Time.unscaledTime + stateSendInterval;
                SendPlayerState();
            }
        }

        public void ReportDamage(string targetId, float amount, Vector2 _)
        {
            if (!isConnected || string.IsNullOrWhiteSpace(targetId))
            {
                return;
            }

            SendJson(
                "{\"type\":\"damage\",\"payload\":{" +
                $"\"targetId\":\"{EscapeJson(targetId)}\"," +
                $"\"amount\":{ToInvariant(amount)}" +
                "}}");
        }

        public async Task ConnectAsync()
        {
            if (isConnected || isConnecting)
            {
                return;
            }

            ResolveDependencies();

            if (localPlayer == null || gameManager == null || arenaBounds == null)
            {
                disconnectReason = "Escena online incompleta.";
                return;
            }

            isConnecting = true;
            cancellationSource = new CancellationTokenSource();
            socket = new ClientWebSocket();

            try
            {
                Uri uri = new($"{ProfileService.Instance.GetWebSocketUrl()}?token={Uri.EscapeDataString(ProfileService.Instance.AuthToken)}");
                await socket.ConnectAsync(uri, cancellationSource.Token);
                _ = ReceiveLoopAsync(socket, cancellationSource.Token);
                isConnected = true;
                disconnectReason = string.Empty;
            }
            catch (Exception exception)
            {
                disconnectReason = $"No se pudo conectar: {exception.Message}";
                isConnected = false;
            }
            finally
            {
                isConnecting = false;
            }
        }

        public async Task DisconnectAsync()
        {
            isConnected = false;
            selfId = string.Empty;

            try
            {
                cancellationSource?.Cancel();

                if (socket != null &&
                    (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived))
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                }
            }
            catch
            {
            }
            finally
            {
                socket?.Dispose();
                socket = null;
                cancellationSource?.Dispose();
                cancellationSource = null;
            }
        }

        private void ResolveDependencies()
        {
            gameManager ??= GetComponent<GameManager>() ?? FindAnyObjectByType<GameManager>();
            arenaBounds ??= FindAnyObjectByType<ArenaBounds>();
            localPlayer ??= FindAnyObjectByType<PlayerController>();
            remotePlayersRoot ??= transform;

            if (localPlayer != null)
            {
                localWeapon = localPlayer.GetComponent<Weapon>();
            }
        }

        private async Task ReceiveLoopAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[8192];

            while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
            {
                ArraySegment<byte> segment = new(buffer);
                WebSocketReceiveResult result;
                int totalBytes = 0;

                do
                {
                    result = await webSocket.ReceiveAsync(segment, cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        disconnectReason = "Conexion cerrada por el servidor.";
                        isConnected = false;
                        return;
                    }

                    totalBytes += result.Count;
                    segment = new ArraySegment<byte>(buffer, totalBytes, buffer.Length - totalBytes);
                }
                while (!result.EndOfMessage);

                inboundMessages.Enqueue(Encoding.UTF8.GetString(buffer, 0, totalBytes));
            }
        }

        private void HandleServerMessage(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                return;
            }

            MessageBase baseMessage = JsonUtility.FromJson<MessageBase>(rawMessage);

            switch (baseMessage?.type)
            {
                case "welcome":
                    HandleWelcome(JsonUtility.FromJson<WelcomeEnvelope>(rawMessage));
                    break;
                case "playerJoined":
                case "playerState":
                case "respawn":
                    HandlePlayerMessage(JsonUtility.FromJson<PlayerEnvelope>(rawMessage));
                    break;
                case "playerLeft":
                    HandlePlayerLeft(JsonUtility.FromJson<PlayerLeftEnvelope>(rawMessage));
                    break;
                case "fire":
                    HandleFire(JsonUtility.FromJson<FireEnvelope>(rawMessage));
                    break;
                case "damage":
                    HandleDamage(JsonUtility.FromJson<DamageEnvelope>(rawMessage));
                    break;
            }
        }

        private void HandleWelcome(WelcomeEnvelope message)
        {
            if (message?.payload == null)
            {
                return;
            }

            selfId = message.payload.selfId ?? string.Empty;
            EnsureLocalActor(selfId);

            Vector2 spawnPoint = arenaBounds.GetRandomPoint(6f);
            localPlayer.RespawnAt(spawnPoint);
            SendRespawn(spawnPoint);
            SendPlayerState();

            if (message.payload.players == null)
            {
                return;
            }

            for (int i = 0; i < message.payload.players.Length; i++)
            {
                PlayerSnapshot snapshot = message.payload.players[i];

                if (snapshot == null || snapshot.id == selfId)
                {
                    continue;
                }

                UpsertRemotePlayer(snapshot);
            }
        }

        private void HandlePlayerMessage(PlayerEnvelope message)
        {
            PlayerSnapshot snapshot = message?.payload?.player;

            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.id))
            {
                return;
            }

            if (snapshot.id == selfId)
            {
                gameManager.SetOnlineScore(snapshot.score, snapshot.kills);
                UpdateLocalPresence(snapshot.username, snapshot.kills);
                return;
            }

            UpsertRemotePlayer(snapshot);
        }

        private void HandlePlayerLeft(PlayerLeftEnvelope message)
        {
            string playerId = message?.payload?.playerId;

            if (string.IsNullOrWhiteSpace(playerId))
            {
                return;
            }

            if (remotePlayers.TryGetValue(playerId, out RemotePlayerAvatar avatar))
            {
                actorIndex.Remove(playerId);
                remotePlayers.Remove(playerId);
                Destroy(avatar.gameObject);
            }
        }

        private void HandleFire(FireEnvelope message)
        {
            FirePayload payload = message?.payload;

            if (payload == null || payload.playerId == selfId || localWeapon == null || localWeapon.ProjectilePrefab == null)
            {
                return;
            }

            if (!remotePlayers.TryGetValue(payload.playerId, out RemotePlayerAvatar avatar))
            {
                return;
            }

            Projectile projectile = Instantiate(localWeapon.ProjectilePrefab, new Vector3(payload.x, payload.y, 0f), Quaternion.identity);
            projectile.gameObject.SetActive(true);
            projectile.Launch(
                new Vector2(payload.dirX, payload.dirY),
                avatar.gameObject,
                Faction.Enemy,
                payload.speed,
                payload.ttl,
                payload.damage,
                avatar.GetComponentsInChildren<Collider2D>(true));
        }

        private void HandleDamage(DamageEnvelope message)
        {
            DamagePayload payload = message?.payload;

            if (payload == null || string.IsNullOrWhiteSpace(payload.targetId))
            {
                return;
            }

            if (payload.attackerId == selfId)
            {
                gameManager.SetOnlineScore(payload.attackerScore, payload.attackerKills);
            }

            if (payload.targetId == selfId)
            {
                Health playerHealth = localPlayer != null ? localPlayer.Health : null;

                if (playerHealth != null && !playerHealth.IsDead)
                {
                    float delta = Mathf.Max(0f, playerHealth.CurrentHealth - payload.targetHp);

                    if (delta > 0.01f)
                    {
                        playerHealth.ApplyDamage(new DamageInfo(delta, null, localPlayer.transform.position, Vector2.zero));
                    }
                }

                if (!payload.targetAlive && !gameManager.IsGameOver)
                {
                    gameManager.ForceGameOver();
                }

                return;
            }

            if (remotePlayers.TryGetValue(payload.targetId, out RemotePlayerAvatar avatar))
            {
                avatar.ApplyDamageState(payload.targetHp, payload.targetAlive);
            }
        }

        private void UpsertRemotePlayer(PlayerSnapshot snapshot)
        {
            if (!remotePlayers.TryGetValue(snapshot.id, out RemotePlayerAvatar avatar))
            {
                avatar = RemotePlayerAvatar.Create(remotePlayersRoot, $"Remote_{snapshot.username}");
                NetworkActor actor = avatar.GetComponent<NetworkActor>();
                actor.Configure(snapshot.id, snapshot.userId, snapshot.username, false);
                remotePlayers.Add(snapshot.id, avatar);
                actorIndex[snapshot.id] = actor;
            }

            avatar.ApplyState(snapshot.x, snapshot.y, snapshot.bodyAngle, snapshot.turretAngle, snapshot.hp, snapshot.maxHp, snapshot.alive);
            CombatantPresence presence = avatar.GetComponent<CombatantPresence>();

            if (presence != null)
            {
                presence.SetDisplayName(snapshot.username);
                presence.SetBaseKills(snapshot.kills);
            }
        }

        private void EnsureLocalActor(string networkId)
        {
            if (localPlayer == null)
            {
                return;
            }

            localActor = localPlayer.GetComponent<NetworkActor>();

            if (localActor == null)
            {
                localActor = localPlayer.gameObject.AddComponent<NetworkActor>();
            }

            localActor.Configure(networkId, ProfileService.Instance.CurrentUserId, ProfileService.Instance.CurrentUserName, true);
            NameplateTarget nameplate = localPlayer.GetComponent<NameplateTarget>();

            if (nameplate != null)
            {
                nameplate.SetDisplayName(ProfileService.Instance.CurrentUserName);
            }

            CombatantPresence presence = localPlayer.GetComponent<CombatantPresence>();

            if (presence != null)
            {
                presence.Configure(ProfileService.Instance.CurrentUserName, true, true);
            }

            actorIndex[networkId] = localActor;
        }

        private void SendRespawn(Vector2 position)
        {
            SendJson(
                "{\"type\":\"respawn\",\"payload\":{" +
                $"\"x\":{ToInvariant(position.x)}," +
                $"\"y\":{ToInvariant(position.y)}" +
                "}}");
        }

        private void SendPlayerState()
        {
            if (localPlayer == null || localWeapon == null)
            {
                return;
            }

            Transform turret = localPlayer.GetComponent<TurretAim>()?.TurretTransform;
            Health health = localPlayer.Health;
            Vector3 position = localPlayer.transform.position;
            float turretAngle = turret != null ? turret.eulerAngles.z : 0f;

            SendJson(
                "{\"type\":\"playerState\",\"payload\":{" +
                $"\"x\":{ToInvariant(position.x)}," +
                $"\"y\":{ToInvariant(position.y)}," +
                "\"bodyAngle\":0," +
                $"\"turretAngle\":{ToInvariant(turretAngle)}," +
                $"\"hp\":{ToInvariant(health != null ? health.CurrentHealth : 0f)}," +
                $"\"maxHp\":{ToInvariant(health != null ? health.MaxHealth : 100f)}," +
                $"\"alive\":{(health != null && !health.IsDead ? "true" : "false")}" +
                "}}");
        }

        private void HandleLocalShot(Weapon _, Weapon.ShotData shot)
        {
            if (!isConnected)
            {
                return;
            }

            SendJson(
                "{\"type\":\"fire\",\"payload\":{" +
                $"\"projectileId\":\"{Guid.NewGuid():N}\"," +
                $"\"x\":{ToInvariant(shot.Origin.x)}," +
                $"\"y\":{ToInvariant(shot.Origin.y)}," +
                $"\"dirX\":{ToInvariant(shot.Direction.x)}," +
                $"\"dirY\":{ToInvariant(shot.Direction.y)}," +
                $"\"speed\":{ToInvariant(shot.Speed)}," +
                $"\"damage\":{ToInvariant(shot.Damage)}," +
                $"\"ttl\":{ToInvariant(shot.Lifetime)}" +
                "}}");
        }

        private async void SendJson(string json)
        {
            if (!isConnected || socket == null || socket.State != WebSocketState.Open)
            {
                return;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(json);

            try
            {
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationSource.Token);
            }
            catch (Exception exception)
            {
                disconnectReason = exception.Message;
                isConnected = false;
            }
        }

        private static string EscapeJson(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string ToInvariant(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private void UpdateLocalPresence(string userName, int kills)
        {
            if (localPlayer == null)
            {
                return;
            }

            CombatantPresence presence = localPlayer.GetComponent<CombatantPresence>();

            if (presence != null)
            {
                if (!string.IsNullOrWhiteSpace(userName))
                {
                    presence.SetDisplayName(userName);
                }

                presence.SetBaseKills(kills);
            }
        }
    }
}
