using UnityEngine;
using UnityEngine.SceneManagement;

namespace TankArena2D
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private bool isPaused;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
            }
        }

        private void OnEnable()
        {
            SetPaused(false);
        }

        private void OnDisable()
        {
            SetPaused(false);
        }

        private void Update()
        {
            if (gameManager != null && gameManager.IsGameOver)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetPaused(!isPaused);
            }
        }

        private void OnGUI()
        {
            if (!isPaused)
            {
                return;
            }

            EnsureStyles();

            Rect overlay = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.Box(overlay, GUIContent.none);
            GUI.color = Color.white;

            Rect panel = new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 120f, 360f, 240f);
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(panel.x + 20f, panel.y + 20f, panel.width - 40f, 28f), "Pausa", titleStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 58f, panel.width - 40f, 20f), "ESC para reanudar", bodyStyle);

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 94f, panel.width - 40f, 34f), "Reanudar"))
            {
                SetPaused(false);
            }

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 136f, panel.width - 40f, 34f), "Volver al menu principal"))
            {
                SetPaused(false);
                SceneManager.LoadScene(SceneRoutes.MainMenu);
            }

            if (GUI.Button(new Rect(panel.x + 20f, panel.y + 178f, panel.width - 40f, 34f), "Cerrar sesion"))
            {
                SetPaused(false);
                ProfileService.Instance.Logout();
                SceneManager.LoadScene(SceneRoutes.MainMenu);
            }
        }

        private void SetPaused(bool paused)
        {
            isPaused = paused;
            Time.timeScale = isPaused ? 0f : 1f;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.95f, 1f) }
            };
        }
    }
}
