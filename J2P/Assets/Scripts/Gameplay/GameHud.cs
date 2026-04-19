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
            Weapon weapon = player != null ? player.GetComponent<Weapon>() : null;
            int currentHealth = health != null ? Mathf.CeilToInt(health.CurrentHealth) : 0;
            int maxHealth = health != null ? Mathf.CeilToInt(health.MaxHealth) : 0;
            int enemiesAlive = gameManager.Spawner != null ? gameManager.Spawner.ActiveEnemyCount : 0;
            string modeText = gameManager.IsMultiplayer ? "Online PvP" : "Survival";

            Rect panelRect = new Rect(Screen.width - 356f, 16f, 340f, 146f);

            GUI.color = new Color(0f, 0f, 0f, 0.58f);
            GUI.Box(panelRect, GUIContent.none);
            GUI.color = Color.white;

            float contentX = panelRect.x + 12f;
            GUI.Label(new Rect(contentX, 24f, 300f, 22f), "Tank Arena", titleStyle);
            GUI.Label(new Rect(contentX, 50f, 320f, 20f), $"Modo: {modeText}   Health: {currentHealth}/{maxHealth}   Lives: {gameManager.RemainingLives}/{gameManager.MaxPlayerLives}", bodyStyle);
            GUI.Label(new Rect(contentX, 72f, 320f, 20f), $"Enemies: {enemiesAlive}   Kills: {gameManager.PlayerKillCount}   Score: {gameManager.Score}", bodyStyle);
            GUI.Label(new Rect(contentX, 94f, 320f, 20f), $"Time: {gameManager.SurvivalTime:0.0}s   Respawn enemigo: {gameManager.EnemyRespawnCountdown:0.0}s", bodyStyle);

            string ammoText = weapon == null
                ? "Sin arma"
                : weapon.IsReloading
                    ? $"Recargando... {weapon.ReloadRemainingNormalized * weapon.ReloadDuration:0.0}s"
                    : $"Municion: {weapon.AmmoInMagazine}/{weapon.MagazineSize}";

            string status = $"WASD mover  |  R recargar  |  Raton apuntar  |  Click izq disparar  |  {ammoText}";

            if (gameManager.IsGameOver)
            {
                status = "Partida terminada";
            }
            else if (player != null && !player.IsAlive)
            {
                status = $"Reaparicion del jugador en {Mathf.Max(0f, gameManager.RespawnCountdown):0.0}s";
            }

            GUI.Label(new Rect(contentX, 118f, 320f, 20f), status, bodyStyle);
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
                normal = { textColor = new Color(0.92f, 0.95f, 1f) }
            };
        }
    }
}
