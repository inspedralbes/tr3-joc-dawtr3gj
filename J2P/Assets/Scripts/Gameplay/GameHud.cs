using UnityEngine;

namespace TankArena2D
{
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;

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
            int currentHealth = health != null ? Mathf.CeilToInt(health.CurrentHealth) : 0;
            int maxHealth = health != null ? Mathf.CeilToInt(health.MaxHealth) : 0;
            int enemiesAlive = gameManager.Spawner != null ? gameManager.Spawner.ActiveEnemyCount : 0;

            Rect panelRect = new Rect(Screen.width - 346f, 16f, 330f, 118f);

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.Box(panelRect, GUIContent.none);
            GUI.color = Color.white;

            float contentX = panelRect.x + 12f;
            GUI.Label(new Rect(contentX, 24f, 300f, 22f), "Tank Arena MVP", titleStyle);
            GUI.Label(new Rect(contentX, 50f, 300f, 20f), $"Health: {currentHealth}/{maxHealth}", bodyStyle);
            GUI.Label(new Rect(contentX, 72f, 300f, 20f), $"Wave: {gameManager.CurrentWave}   Enemies: {enemiesAlive}   Kills: {gameManager.TotalKills}", bodyStyle);

            string status = "WASD move  |  Mouse aim  |  Left click shoot";

            if (player != null && !player.IsAlive)
            {
                status = $"Respawn in {Mathf.Max(0f, gameManager.RespawnCountdown):0.0}s";
            }
            else if (enemiesAlive == 0 && gameManager.NextWaveCountdown > 0f)
            {
                status = $"Next wave in {gameManager.NextWaveCountdown:0.0}s";
            }

            GUI.Label(new Rect(contentX, 94f, 300f, 20f), status, bodyStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null && bodyStyle != null)
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
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };
        }
    }
}
