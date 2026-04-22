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
            ResolveGameManager();
        }

        private void OnGUI()
        {
            ResolveGameManager();

            if (gameManager == null || !gameManager.IsGameOver)
            {
                return;
            }

            EnsureStyles();

            Rect overlay = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.color = new Color(0.01f, 0.03f, 0.07f, 0.82f);
            GUI.Box(overlay, GUIContent.none);
            GUI.color = Color.white;

            Rect panel = new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 190f, 460f, 380f);
            GUI.color = new Color(0.05f, 0.08f, 0.14f, 0.97f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = new Color(0.19f, 0.54f, 0.98f, 0.95f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, 34f), "Fin de partida", titleStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 70f, panel.width - 48f, 26f), $"Puntuación final: {gameManager.Score}", highlightStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 104f, panel.width - 48f, 22f), $"Bajas totales: {gameManager.TotalKills}", bodyStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 130f, panel.width - 48f, 22f), $"Eliminaciones tuyas: {gameManager.PlayerKillCount}", bodyStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 156f, panel.width - 48f, 22f), $"Tiempo sobrevivido: {gameManager.SurvivalTime:0.0}s", bodyStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 182f, panel.width - 48f, 22f), $"Vidas consumidas: {gameManager.MaxPlayerLives}", bodyStyle);

            GUI.backgroundColor = new Color(0.14f, 0.39f, 0.88f, 1f);

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + 238f, panel.width - 48f, 42f), "Volver a jugar"))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneRoutes.Game);
            }

            GUI.backgroundColor = new Color(0.08f, 0.19f, 0.35f, 1f);

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + 288f, panel.width - 48f, 42f), "Volver al menú"))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneRoutes.MainMenu);
            }

            if (GUI.Button(new Rect(panel.x + 24f, panel.y + 338f, panel.width - 48f, 30f), "Cerrar sesión"))
            {
                Time.timeScale = 1f;
                ProfileService.Instance.Logout();
                SceneManager.LoadScene(SceneRoutes.MainMenu);
            }

            GUI.backgroundColor = Color.white;
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
                normal = { textColor = new Color(0.35f, 0.72f, 1f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };
        }

        private void ResolveGameManager()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }
        }
    }
}
