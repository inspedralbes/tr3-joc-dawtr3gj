using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TankArena2D
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string pendingUserName = "Jugador";
        [SerializeField] private string pendingPassword = string.Empty;
        [SerializeField] private string pendingBackendUrl = BackendSettings.DefaultApiBaseUrl;

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle sectionStyle;
        private string loginError;
        private bool requestInFlight;

        private void Awake()
        {
            ProfileService.EnsureInstance();
            pendingBackendUrl = ProfileService.Instance.ApiBaseUrl;
            pendingUserName = string.IsNullOrWhiteSpace(ProfileService.Instance.CurrentUserName)
                ? pendingUserName
                : ProfileService.Instance.CurrentUserName;
        }

        private void OnGUI()
        {
            EnsureStyles();

            Rect panel = new(Screen.width * 0.5f - 450f, 40f, 900f, Screen.height - 80f);
            GUI.color = new Color(0f, 0f, 0f, 0.58f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(panel.x + 28f, panel.y + 20f, panel.width - 56f, 36f), "Tank Arena", titleStyle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 58f, panel.width - 56f, 24f), "Login remoto, partida local y PvP online sobre backend Node + MongoDB", subtitleStyle);

            if (!ProfileService.Instance.IsLoggedIn)
            {
                DrawLogin(panel);
                return;
            }

            DrawMainMenu(panel);
        }

        private void DrawLogin(Rect panel)
        {
            Rect loginBox = new(panel.x + 28f, panel.y + 118f, panel.width - 56f, 320f);
            DrawSection(loginBox, "Acceso");

            GUI.Label(new Rect(loginBox.x + 20f, loginBox.y + 48f, 220f, 24f), "Backend URL", bodyStyle);
            pendingBackendUrl = GUI.TextField(new Rect(loginBox.x + 20f, loginBox.y + 76f, loginBox.width - 40f, 30f), pendingBackendUrl, 96);

            GUI.Label(new Rect(loginBox.x + 20f, loginBox.y + 118f, 220f, 24f), "Nombre de usuario", bodyStyle);
            pendingUserName = GUI.TextField(new Rect(loginBox.x + 20f, loginBox.y + 146f, loginBox.width - 40f, 30f), pendingUserName, 24);

            GUI.Label(new Rect(loginBox.x + 20f, loginBox.y + 188f, 220f, 24f), "Contrasena", bodyStyle);
            pendingPassword = GUI.PasswordField(new Rect(loginBox.x + 20f, loginBox.y + 216f, loginBox.width - 40f, 30f), pendingPassword, '*', 48);

            float halfWidth = (loginBox.width - 52f) * 0.5f;

            GUI.enabled = !requestInFlight;

            if (GUI.Button(new Rect(loginBox.x + 20f, loginBox.y + 262f, halfWidth, 38f), "Registrarse"))
            {
                ProfileService.Instance.SetApiBaseUrl(pendingBackendUrl);
                StartCoroutine(HandleAuthRequest(AuthApiClient.Register(ProfileService.Instance.ApiBaseUrl, pendingUserName, pendingPassword, OnAuthCompleted)));
            }

            if (GUI.Button(new Rect(loginBox.x + 32f + halfWidth, loginBox.y + 262f, halfWidth, 38f), "Iniciar sesion"))
            {
                ProfileService.Instance.SetApiBaseUrl(pendingBackendUrl);
                StartCoroutine(HandleAuthRequest(AuthApiClient.Login(ProfileService.Instance.ApiBaseUrl, pendingUserName, pendingPassword, OnAuthCompleted)));
            }

            GUI.enabled = true;

            if (!string.IsNullOrWhiteSpace(loginError))
            {
                GUI.Label(new Rect(loginBox.x + 20f, loginBox.y + 304f, loginBox.width - 40f, 20f), loginError, bodyStyle);
            }

            GUI.Label(
                new Rect(panel.x + 28f, panel.y + 466f, panel.width - 56f, 120f),
                "Flujo actual:\n- registro/login con backend\n- persistencia local de token y estadisticas\n- survival local\n- PvP online con WebSocket\n- training separado",
                bodyStyle);
        }

        private void DrawMainMenu(Rect panel)
        {
            ProfileService profile = ProfileService.Instance;
            ProfileService.ProfileStats stats = profile.CurrentStats;
            IReadOnlyList<ProfileService.MatchRecord> recentMatches = profile.RecentMatches;
            IReadOnlyList<ProfileService.LeaderboardEntry> leaderboard = profile.GetLeaderboard();

            Rect left = new(panel.x + 24f, panel.y + 108f, 270f, panel.height - 132f);
            Rect center = new(left.xMax + 18f, panel.y + 108f, 270f, panel.height - 132f);
            Rect right = new(center.xMax + 18f, panel.y + 108f, 270f, panel.height - 132f);

            DrawActions(left, profile);
            DrawCurrentProfile(center, profile.CurrentUserName, stats, recentMatches);
            DrawLeaderboard(right, leaderboard);
        }

        private void DrawActions(Rect rect, ProfileService profile)
        {
            DrawSection(rect, "Acciones");

            GUI.Label(new Rect(rect.x + 18f, rect.y + 44f, rect.width - 36f, 22f), $"Sesion: {profile.CurrentUserName}", bodyStyle);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 68f, rect.width - 36f, 20f), $"Backend: {profile.ApiBaseUrl}", smallStyle);

            float width = rect.width - 36f;
            float x = rect.x + 18f;
            float y = rect.y + 104f;

            if (GUI.Button(new Rect(x, y, width, 42f), "Iniciar partida online"))
            {
                profile.SetMatchMode(MatchMode.OnlinePvP);
                SceneManager.LoadScene(SceneRoutes.Game);
            }

            if (GUI.Button(new Rect(x, y + 52f, width, 38f), "Iniciar partida local"))
            {
                profile.SetMatchMode(MatchMode.LocalSurvival);
                SceneManager.LoadScene(SceneRoutes.Game);
            }

            if (GUI.Button(new Rect(x, y + 98f, width, 36f), "Abrir entrenamiento"))
            {
                SceneManager.LoadScene(SceneRoutes.Training);
            }

            if (GUI.Button(new Rect(x, y + 142f, width, 36f), "Cerrar sesion"))
            {
                profile.Logout();
                pendingPassword = string.Empty;
            }

            if (GUI.Button(new Rect(x, y + 186f, width, 36f), "Salir"))
            {
                Application.Quit();
            }

            GUI.Label(
                new Rect(x, rect.y + 330f, width, 180f),
                "Notas:\n- online usa el backend configurado arriba\n- local mantiene bots y puntuacion survival\n- online desactiva bots y usa otros jugadores conectados",
                bodyStyle);
        }

        private void DrawCurrentProfile(
            Rect rect,
            string userName,
            ProfileService.ProfileStats stats,
            IReadOnlyList<ProfileService.MatchRecord> recentMatches)
        {
            DrawSection(rect, "Tu Perfil");

            float x = rect.x + 18f;
            float width = rect.width - 36f;
            float y = rect.y + 44f;

            GUI.Label(new Rect(x, y, width, 22f), userName, subtitleStyle);
            GUI.Label(new Rect(x, y + 32f, width, 20f), $"Partidas: {stats.MatchesPlayed}", bodyStyle);
            GUI.Label(new Rect(x, y + 56f, width, 20f), $"Bajas totales: {stats.TotalKills}", bodyStyle);
            GUI.Label(new Rect(x, y + 80f, width, 20f), $"Mejor partida por bajas: {stats.BestKillsInMatch}", bodyStyle);
            GUI.Label(new Rect(x, y + 104f, width, 20f), $"Mejor puntuacion: {stats.BestScore}", bodyStyle);
            GUI.Label(new Rect(x, y + 128f, width, 20f), $"Ultima partida: {stats.LastScore} pts / {stats.LastKills} bajas", bodyStyle);
            GUI.Label(new Rect(x, y + 152f, width, 20f), $"Tiempo total: {stats.TotalPlayTime:0.0}s", bodyStyle);
            GUI.Label(new Rect(x, y + 176f, width, 20f), $"Mejor supervivencia: {stats.BestSurvivalTime:0.0}s", bodyStyle);
            GUI.Label(new Rect(x, y + 200f, width, 20f), $"Sesion remota: {(ProfileService.Instance.HasAuthenticatedSession ? "Activa" : "No")}", bodyStyle);

            GUI.Label(new Rect(x, y + 238f, width, 22f), "Historial reciente", sectionStyle);

            if (recentMatches.Count == 0)
            {
                GUI.Label(new Rect(x, y + 266f, width, 20f), "Sin partidas registradas todavia.", smallStyle);
                return;
            }

            for (int index = 0; index < recentMatches.Count && index < 6; index++)
            {
                ProfileService.MatchRecord match = recentMatches[index];
                float lineY = y + 266f + index * 38f;
                GUI.Label(new Rect(x, lineY, width, 18f), $"{index + 1}. {match.Score} pts  |  {match.Kills} bajas  |  {match.SurvivalTime:0.0}s", smallStyle);
                GUI.Label(new Rect(x, lineY + 16f, width, 16f), match.PlayedAtUtc, smallStyle);
            }
        }

        private void DrawLeaderboard(Rect rect, IReadOnlyList<ProfileService.LeaderboardEntry> leaderboard)
        {
            DrawSection(rect, "Ranking Local");

            float x = rect.x + 18f;
            float width = rect.width - 36f;
            float y = rect.y + 44f;

            if (leaderboard.Count == 0)
            {
                GUI.Label(new Rect(x, y, width, 20f), "No hay perfiles con puntuaciones aun.", bodyStyle);
                return;
            }

            for (int index = 0; index < leaderboard.Count && index < 8; index++)
            {
                ProfileService.LeaderboardEntry entry = leaderboard[index];
                float lineY = y + index * 58f;
                GUI.Label(new Rect(x, lineY, width, 20f), $"{index + 1}. {entry.UserName}", bodyStyle);
                GUI.Label(new Rect(x, lineY + 18f, width, 18f), $"Best score: {entry.BestScore}  |  Best kills: {entry.BestKillsInMatch}", smallStyle);
                GUI.Label(new Rect(x, lineY + 34f, width, 18f), $"Matches: {entry.MatchesPlayed}  |  Best time: {entry.BestSurvivalTime:0.0}s", smallStyle);
            }
        }

        private IEnumerator HandleAuthRequest(IEnumerator request)
        {
            requestInFlight = true;
            loginError = "Conectando...";
            yield return request;
            requestInFlight = false;
        }

        private void OnAuthCompleted(AuthApiClient.AuthResult result)
        {
            if (!result.Success)
            {
                loginError = result.Message;
                return;
            }

            ProfileService.Instance.CompleteRemoteLogin(result.UserId, result.UserName, result.Token);
            loginError = string.Empty;
            pendingPassword = string.Empty;
        }

        private void DrawSection(Rect rect, string title)
        {
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.84f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 18f, rect.y + 14f, rect.width - 36f, 22f), title, sectionStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.92f, 0.95f, 1f) }
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.86f, 0.44f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.95f, 1f) }
            };

            smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.88f, 0.96f) }
            };
        }
    }
}
