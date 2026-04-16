using UnityEngine;
using UnityEngine.SceneManagement;

namespace TankArena2D
{
    public sealed class GameOverOverlay : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle highlightStyle;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }
        }

        private void OnGUI()
        {
            if (gameManager == null || !gameManager.IsGameOver)
            {
                return;
            }

            EnsureStyles();

            Rect overlay = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.Box(overlay, GUIContent.none);
            GUI.color = Color.white;

            Rect panel = new Rect(Screen.width * 0.5f - 210f, Screen.height * 0.5f - 180f, 420f, 360f);
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.94f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, 34f), "Fin de partida", titleStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 70f, panel.width - 48f, 26f), $"Puntuacion final: {gameManager.Score}", highlightStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 102f, panel.width - 48f, 22f), $"Bajas: {gameManager.TotalKills}", bodyStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 126f, panel.width - 48f, 22f), $"Tiempo sobrevivido: {gameManager.SurvivalTime:0.0}s", bodyStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 150f, panel.width - 48f, 22f), $"Enemigos eliminados por ti: {gameManager.PlayerKillCount}", bodyStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 174f, panel.width - 48f, 22f), $"Vidas agotadas: {gameManager.MaxPlayerLives}", bodyStyle);

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + 224f, panel.width - 48f, 36f), "Reintentar"))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneRoutes.Game);
            }

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + 268f, panel.width - 48f, 36f), "Volver al menu principal"))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneRoutes.MainMenu);
            }

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + 312f, panel.width - 48f, 30f), "Cerrar sesion"))
            {
                Time.timeScale = 1f;
                ProfileService.Instance.Logout();
                SceneManager.LoadScene(SceneRoutes.MainMenu);
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            highlightStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.87f, 0.42f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };
        }
    }
}
