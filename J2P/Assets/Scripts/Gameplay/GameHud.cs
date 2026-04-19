using UnityEngine;
using System;

namespace TankArena2D
{
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;

        public void Configure(GameManager manager)
        {
            gameManager = manager;
        }

        private void OnGUI()
        {
            if (gameManager == null)
            {
                return;
            }

            EnsureStyles();

            PlayerController player = gameManager.Player;
            Health health = player != null ? player.Health : null;
            Weapon weapon = player != null ? player.GetComponent<Weapon>() : null;
            int currentHealth = health != null ? Mathf.CeilToInt(health.CurrentHealth) : 0;
            int maxHealth = health != null ? Mathf.CeilToInt(health.MaxHealth) : 0;
            int enemiesAlive = gameManager.Spawner != null ? gameManager.Spawner.ActiveEnemyCount : 0;
            string modeText = gameManager.IsMultiplayer ? "Online PvP" : "Supervivencia";

            Rect panelRect = new Rect(Screen.width - 376f, 16f, 360f, 158f);

            GUI.color = new Color(0.03f, 0.06f, 0.11f, 0.88f);
            GUI.Box(panelRect, GUIContent.none);
            GUI.color = new Color(0.19f, 0.54f, 0.98f, 0.9f);
            GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float contentX = panelRect.x + 12f;
            GUI.Label(new Rect(contentX, 24f, 300f, 22f), "J2P", titleStyle);
            GUI.Label(new Rect(contentX, 50f, 330f, 20f), $"Modo: {modeText}   Vida: {currentHealth}/{maxHealth}   Vidas: {gameManager.RemainingLives}/{gameManager.MaxPlayerLives}", bodyStyle);
            GUI.Label(new Rect(contentX, 74f, 330f, 20f), $"En arena: {enemiesAlive}   Bajas: {gameManager.PlayerKillCount}   Score: {gameManager.Score}", bodyStyle);
            GUI.Label(new Rect(contentX, 98f, 330f, 20f), $"Tiempo: {gameManager.SurvivalTime:0.0}s   Respawn bot: {gameManager.EnemyRespawnCountdown:0.0}s", bodyStyle);

            string ammoText = weapon == null
                ? "Sin arma"
                : weapon.IsReloading
                    ? $"Recargando... {weapon.ReloadRemainingNormalized * weapon.ReloadDuration:0.0}s"
                    : $"Municion: {weapon.AmmoInMagazine}/{weapon.MagazineSize}";

            string status = $"{ammoText}";

            if (gameManager.IsGameOver)
            {
                status = "Partida terminada";
            }
            else if (player != null && !player.IsAlive)
            {
                status = $"Reaparicion del jugador en {Mathf.Max(0f, gameManager.RespawnCountdown):0.0}s";
            }

            GUI.Label(new Rect(contentX, 124f, 330f, 20f), status, smallStyle);

            DrawNameplates();
            DrawTopFive();
        }

        private void DrawNameplates()
        {
            if (Camera.main == null)
            {
                return;
            }

            NameplateTarget[] targets = FindObjectsByType<NameplateTarget>(FindObjectsSortMode.None);

            for (int index = 0; index < targets.Length; index++)
            {
                NameplateTarget target = targets[index];

                if (target == null || !target.ShouldDisplay)
                {
                    continue;
                }

                Vector3 screen = Camera.main.WorldToScreenPoint(target.transform.position + target.WorldOffset);

                if (screen.z <= 0f)
                {
                    continue;
                }

                float width = 120f;
                Rect rect = new Rect(screen.x - width * 0.5f, Screen.height - screen.y - 34f, width, 18f);
                GUI.Label(rect, target.DisplayName, smallStyle);
            }
        }

        private void DrawTopFive()
        {
            CombatantPresence[] combatants = FindObjectsByType<CombatantPresence>(FindObjectsSortMode.None);

            if (combatants == null || combatants.Length == 0)
            {
                return;
            }

            Array.Sort(combatants, (left, right) =>
            {
                if (left == null && right == null)
                {
                    return 0;
                }

                if (left == null)
                {
                    return 1;
                }

                if (right == null)
                {
                    return -1;
                }

                return right.TotalKills.CompareTo(left.TotalKills);
            });

            Rect panel = new Rect(18f, 18f, 250f, 180f);
            GUI.color = new Color(0.03f, 0.06f, 0.11f, 0.88f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = new Color(0.19f, 0.54f, 0.98f, 0.9f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 12f, panel.y + 12f, panel.width - 24f, 22f), "Top 5", titleStyle);

            int row = 0;

            for (int index = 0; index < combatants.Length && row < 5; index++)
            {
                CombatantPresence entry = combatants[index];

                if (entry == null || !entry.ShowInLeaderboard)
                {
                    continue;
                }

                float lineY = panel.y + 42f + row * 24f;
                GUI.Label(new Rect(panel.x + 12f, lineY, panel.width - 24f, 20f), $"{row + 1}. {entry.DisplayName}  |  {entry.TotalKills}", bodyStyle);
                row++;
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null && bodyStyle != null && smallStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.88f, 0.94f, 1f) }
            };

            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.64f, 0.82f, 1f) }
            };
        }
    }
}
