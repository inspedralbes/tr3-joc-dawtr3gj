using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TankArena2D
{
    public sealed class MainMenuController : MonoBehaviour
    {
        private enum AuthView
        {
            Login,
            Register
        }

        [SerializeField] private string pendingUserName = string.Empty;
        [SerializeField] private string pendingPassword = string.Empty;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle mutedStyle;
        private GUIStyle buttonStyle;
        private GUIStyle tabStyle;
        private GUIStyle leaderboardNameStyle;
        private string statusMessage = string.Empty;
        private bool requestInFlight;
        private AuthView authView = AuthView.Login;

        private void Awake()
        {
            ProfileService.EnsureInstance();
            pendingUserName = string.IsNullOrWhiteSpace(ProfileService.Instance.CurrentUserName)
                ? pendingUserName
                : ProfileService.Instance.CurrentUserName;

            ProfileService.Instance.RefreshRemoteStats();
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawBackground();

            if (!ProfileService.Instance.IsLoggedIn)
            {
                DrawAuthScreen();
                return;
            }

            DrawHomeScreen();
        }

        private void DrawAuthScreen()
        {
            float cardWidth = Mathf.Min(600f, Screen.width - 80f);
            float cardHeight = 450f;
            Rect card = new(Screen.width * 0.5f - cardWidth * 0.5f, Screen.height * 0.5f - cardHeight * 0.5f, cardWidth, cardHeight);

            DrawCard(card, new Color(0.05f, 0.08f, 0.14f, 0.97f));

            GUI.Label(new Rect(card.x + 28f, card.y + 28f, card.width - 56f, 42f), "J2P", titleStyle);
            GUI.Label(new Rect(card.x + 28f, card.y + 72f, card.width - 56f, 26f), "Accede a tu cuenta y entra en combate.", subtitleStyle);

            Rect loginTab = new(card.x + 28f, card.y + 118f, 140f, 34f);
            Rect registerTab = new(card.x + 176f, card.y + 118f, 140f, 34f);

            GUI.enabled = !requestInFlight;

            if (GUI.Button(loginTab, "Entrar", tabStyle))
            {
                authView = AuthView.Login;
                statusMessage = string.Empty;
            }

            if (GUI.Button(registerTab, "Crear cuenta", tabStyle))
            {
                authView = AuthView.Register;
                statusMessage = string.Empty;
            }

            GUI.enabled = true;

            GUI.color = authView == AuthView.Login ? new Color(1f, 0.69f, 0.17f) : new Color(0.34f, 0.36f, 0.41f);
            GUI.DrawTexture(new Rect(loginTab.x, loginTab.yMax + 2f, loginTab.width, 3f), Texture2D.whiteTexture);
            GUI.color = authView == AuthView.Register ? new Color(1f, 0.69f, 0.17f) : new Color(0.34f, 0.36f, 0.41f);
            GUI.DrawTexture(new Rect(registerTab.x, registerTab.yMax + 2f, registerTab.width, 3f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(card.x + 28f, card.y + 182f, card.width - 56f, 22f), "Nombre de usuario", mutedStyle);
            pendingUserName = GUI.TextField(new Rect(card.x + 28f, card.y + 208f, card.width - 56f, 36f), pendingUserName, 24);

            GUI.Label(new Rect(card.x + 28f, card.y + 260f, card.width - 56f, 22f), "Contraseña", mutedStyle);
            pendingPassword = GUI.PasswordField(new Rect(card.x + 28f, card.y + 286f, card.width - 56f, 36f), pendingPassword, '*', 48);

            string submitLabel = authView == AuthView.Login ? "Iniciar sesión" : "Crear cuenta";

            GUI.enabled = !requestInFlight;

            GUI.backgroundColor = new Color(0.14f, 0.39f, 0.88f, 1f);

            if (GUI.Button(new Rect(card.x + 28f, card.y + 342f, card.width - 56f, 44f), submitLabel, buttonStyle))
            {
                StartAuthRequest();
            }

            GUI.backgroundColor = Color.white;

            GUI.enabled = true;

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                GUI.Label(new Rect(card.x + 28f, card.y + 394f, card.width - 56f, 24f), statusMessage, mutedStyle);
            }
        }

        private void DrawHomeScreen()
        {
            ProfileService profile = ProfileService.Instance;
            Rect header = new(34f, 30f, Screen.width - 68f, 86f);
            DrawCard(header, new Color(0.05f, 0.08f, 0.14f, 0.95f));
            GUI.Label(new Rect(header.x + 22f, header.y + 18f, header.width - 44f, 34f), "J2P", titleStyle);
            GUI.Label(new Rect(header.x + 22f, header.y + 50f, header.width - 44f, 22f), $"Bienvenido, {profile.CurrentUserName}", subtitleStyle);

            float columnWidth = Mathf.Min(320f, (Screen.width - 96f) / 3f);
            float gap = 18f;
            float startX = 34f;
            float top = 132f;
            float height = Screen.height - top - 34f;

            Rect actionsRect = new(startX, top, columnWidth, height);
            Rect profileRect = new(actionsRect.xMax + gap, top, columnWidth, height);
            Rect leaderboardRect = new(profileRect.xMax + gap, top, columnWidth, height);

            DrawActions(actionsRect, profile);
            DrawProfile(profileRect, profile);
            DrawLeaderboard(leaderboardRect, profile);
        }

        private void DrawActions(Rect rect, ProfileService profile)
        {
            DrawCard(rect, new Color(0.05f, 0.08f, 0.14f, 0.95f));
            GUI.Label(new Rect(rect.x + 18f, rect.y + 18f, rect.width - 36f, 28f), "Jugar", cardTitleStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 52f, rect.width - 36f, 20f), "Elige cómo quieres entrar en la arena.", mutedStyle);

            float width = rect.width - 36f;
            float x = rect.x + 18f;
            float y = rect.y + 96f;

            GUI.backgroundColor = new Color(0.14f, 0.39f, 0.88f, 1f);

            if (GUI.Button(new Rect(x, y, width, 52f), "Partida online", buttonStyle))
            {
                profile.SetMatchMode(MatchMode.OnlinePvP);
                SceneManager.LoadScene(SceneRoutes.Game);
            }

            GUI.backgroundColor = new Color(0.08f, 0.19f, 0.35f, 1f);

            if (GUI.Button(new Rect(x, y + 64f, width, 46f), "Supervivencia", buttonStyle))
            {
                profile.SetMatchMode(MatchMode.LocalSurvival);
                SceneManager.LoadScene(SceneRoutes.Game);
            }

            if (GUI.Button(new Rect(x, y + 120f, width, 40f), "Cerrar sesión", buttonStyle))
            {
                profile.Logout();
                pendingPassword = string.Empty;
                statusMessage = string.Empty;
            }

            if (GUI.Button(new Rect(x, y + 170f, width, 40f), "Salir", buttonStyle))
            {
                Application.Quit();
            }

            GUI.backgroundColor = Color.white;
        }

        private void DrawProfile(Rect rect, ProfileService profile)
        {
            DrawCard(rect, new Color(0.05f, 0.08f, 0.14f, 0.95f));
            GUI.Label(new Rect(rect.x + 18f, rect.y + 18f, rect.width - 36f, 28f), "Perfil", cardTitleStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 54f, rect.width - 36f, 30f), profile.CurrentUserName, subtitleStyle);

            ProfileService.ProfileStats stats = profile.CurrentStats;
            float x = rect.x + 18f;
            float y = rect.y + 102f;
            float width = rect.width - 36f;

            GUI.Label(new Rect(x, y, width, 20f), $"Mejor puntuación: {stats.BestScore}", bodyStyle);
            GUI.Label(new Rect(x, y + 28f, width, 20f), $"Mejor partida por bajas: {stats.BestKillsInMatch}", bodyStyle);
            GUI.Label(new Rect(x, y + 56f, width, 20f), $"Partidas jugadas: {stats.MatchesPlayed}", bodyStyle);
            GUI.Label(new Rect(x, y + 84f, width, 20f), $"Bajas totales: {stats.TotalKills}", bodyStyle);
            GUI.Label(new Rect(x, y + 112f, width, 20f), $"Mejor supervivencia: {stats.BestSurvivalTime:0.0}s", bodyStyle);
            GUI.Label(new Rect(x, y + 140f, width, 20f), $"Última partida: {stats.LastScore} pts / {stats.LastKills} bajas", bodyStyle);

            GUI.Label(new Rect(x, y + 192f, width, 24f), "Historial reciente", cardTitleStyle);

            if (profile.RecentMatches.Count == 0)
            {
                GUI.Label(new Rect(x, y + 224f, width, 20f), "Todavía no has jugado partidas con esta cuenta.", mutedStyle);
                return;
            }

            for (int index = 0; index < profile.RecentMatches.Count && index < 5; index++)
            {
                ProfileService.MatchRecord match = profile.RecentMatches[index];
                float lineY = y + 224f + index * 42f;
                GUI.Label(new Rect(x, lineY, width, 18f), $"{match.Score} pts  |  {match.Kills} bajas  |  {match.SurvivalTime:0.0}s", bodyStyle);
                GUI.Label(new Rect(x, lineY + 18f, width, 18f), match.PlayedAtUtc, mutedStyle);
            }
        }

        private void DrawLeaderboard(Rect rect, ProfileService profile)
        {
            DrawCard(rect, new Color(0.05f, 0.08f, 0.14f, 0.95f));
            GUI.Label(new Rect(rect.x + 18f, rect.y + 18f, rect.width - 36f, 28f), "Ranking online", cardTitleStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 52f, rect.width - 36f, 20f), "Top global de la arena.", mutedStyle);

            IReadOnlyList<ProfileService.LeaderboardEntry> leaderboard = profile.GetLeaderboard();

            if (leaderboard.Count == 0)
            {
                GUI.Label(new Rect(rect.x + 18f, rect.y + 92f, rect.width - 36f, 20f), "Cargando clasificación...", mutedStyle);
                return;
            }

            for (int index = 0; index < leaderboard.Count && index < 8; index++)
            {
                ProfileService.LeaderboardEntry entry = leaderboard[index];
                float lineY = rect.y + 92f + index * 58f;
                GUI.Label(new Rect(rect.x + 18f, lineY, rect.width - 36f, 20f), $"{index + 1}. {entry.UserName}", leaderboardNameStyle);
                GUI.Label(new Rect(rect.x + 18f, lineY + 20f, rect.width - 36f, 18f), $"Score {entry.BestScore}  |  Kills {entry.BestKillsInMatch}", bodyStyle);
                GUI.Label(new Rect(rect.x + 18f, lineY + 38f, rect.width - 36f, 18f), $"Partidas {entry.MatchesPlayed}  |  Tiempo {entry.BestSurvivalTime:0.0}s", mutedStyle);
            }
        }

        private void StartAuthRequest()
        {
            requestInFlight = true;
            statusMessage = authView == AuthView.Login ? "Entrando..." : "Creando cuenta...";

            IEnumerator request = authView == AuthView.Login
                ? AuthApiClient.Login(ProfileService.Instance.ApiBaseUrl, pendingUserName, pendingPassword, OnAuthCompleted)
                : AuthApiClient.Register(ProfileService.Instance.ApiBaseUrl, pendingUserName, pendingPassword, OnAuthCompleted);

            StartCoroutine(HandleAuthRequest(request));
        }

        private IEnumerator HandleAuthRequest(IEnumerator request)
        {
            yield return request;
            requestInFlight = false;
        }

        private void OnAuthCompleted(AuthApiClient.AuthResult result)
        {
            if (!result.Success)
            {
                statusMessage = result.Message;
                return;
            }

            ProfileService.Instance.CompleteRemoteLogin(result.UserId, result.UserName, result.Token);
            ProfileService.Instance.RefreshRemoteStats();
            statusMessage = string.Empty;
            pendingPassword = string.Empty;
        }

        private static void DrawBackground()
        {
            GUI.color = new Color(0.02f, 0.04f, 0.08f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.06f, 0.16f, 0.32f, 0.65f);
            GUI.DrawTexture(new Rect(-120f, 80f, 360f, 360f), Texture2D.whiteTexture);
            GUI.color = new Color(0.10f, 0.32f, 0.62f, 0.20f);
            GUI.DrawTexture(new Rect(Screen.width - 420f, -60f, 500f, 280f), Texture2D.whiteTexture);
            GUI.color = new Color(0.02f, 0.10f, 0.22f, 0.40f);
            GUI.DrawTexture(new Rect(Screen.width * 0.35f, Screen.height - 180f, 380f, 220f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawCard(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.Box(rect, GUIContent.none);
            GUI.color = new Color(0.19f, 0.54f, 0.98f, 0.95f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3f), Texture2D.whiteTexture);
            GUI.color = new Color(0f, 0f, 0f, 0.24f);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.98f, 0.98f) }
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.67f, 0.83f, 1f) }
            };

            cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.35f, 0.72f, 1f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.95f, 0.97f, 1f) }
            };

            mutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.60f, 0.72f, 0.86f) }
            };

            leaderboardNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                fixedHeight = 0f,
            };

            tabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
            };
        }
    }
}
