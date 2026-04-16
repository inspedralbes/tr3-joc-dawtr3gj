using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TankArena2D
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string pendingUserName = "Jugador";

        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle sectionStyle;
        private string loginError;

        private void Awake()
        {
            ProfileService.EnsureInstance();
        }

        private void OnGUI()
        {
            EnsureStyles();

            Rect panel = new Rect(Screen.width * 0.5f - 420f, 50f, 840f, Screen.height - 100f);
            GUI.color = new Color(0f, 0f, 0f, 0.58f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(panel.x + 28f, panel.y + 20f, panel.width - 56f, 36f), "Tank Arena", titleStyle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 58f, panel.width - 56f, 24f), "Menu principal, perfiles locales e historial de partidas", subtitleStyle);

            if (!ProfileService.Instance.IsLoggedIn)
            {
                DrawLogin(panel);
                return;
            }

            DrawMainMenu(panel);
        }

        private void DrawLogin(Rect panel)
        {
            Rect loginBox = new Rect(panel.x + 28f, panel.y + 118f, panel.width - 56f, 220f);
            DrawSection(loginBox, "Login");

            GUI.Label(new Rect(loginBox.x + 20f, loginBox.y + 48f, 220f, 24f), "Nombre de usuario", bodyStyle);
            pendingUserName = GUI.TextField(new Rect(loginBox.x + 20f, loginBox.y + 76f, loginBox.width - 40f, 30f), pendingUserName, 24);

            if (GUI.Button(new Rect(loginBox.x + 20f, loginBox.y + 122f, loginBox.width - 40f, 38f), "Iniciar sesion"))
            {
                if (!ProfileService.Instance.Login(pendingUserName))
                {
                    loginError = "Introduce un nombre valido.";
                }
                else
                {
                    loginError = string.Empty;
                }
            }

            if (!string.IsNullOrWhiteSpace(loginError))
            {
                GUI.Label(new Rect(loginBox.x + 20f, loginBox.y + 168f, loginBox.width - 40f, 20f), loginError, bodyStyle);
            }

            GUI.Label(
                new Rect(panel.x + 28f, panel.y + 364f, panel.width - 56f, 80f),
                "El perfil se guarda en local con PlayerPrefs. Cada usuario mantiene sus mejores puntuaciones, bajas y un historial corto de partidas recientes.",
                bodyStyle);
        }

        private void DrawMainMenu(Rect panel)
        {
            ProfileService profile = ProfileService.Instance;
            ProfileService.ProfileStats stats = profile.CurrentStats;
            IReadOnlyList<ProfileService.MatchRecord> recentMatches = profile.RecentMatches;
            IReadOnlyList<ProfileService.LeaderboardEntry> leaderboard = profile.GetLeaderboard();

            Rect left = new Rect(panel.x + 24f, panel.y + 108f, 252f, panel.height - 132f);
            Rect center = new Rect(left.xMax + 18f, panel.y + 108f, 252f, panel.height - 132f);
            Rect right = new Rect(center.xMax + 18f, panel.y + 108f, 252f, panel.height - 132f);

            DrawActions(left, profile);
            DrawCurrentProfile(center, profile.CurrentUserName, stats, recentMatches);
            DrawLeaderboard(right, leaderboard);
        }

        private void DrawActions(Rect rect, ProfileService profile)
        {
            DrawSection(rect, "Acciones");

            GUI.Label(new Rect(rect.x + 18f, rect.y + 44f, rect.width - 36f, 22f), $"Sesion: {profile.CurrentUserName}", bodyStyle);

            float width = rect.width - 36f;
            float x = rect.x + 18f;
            float y = rect.y + 84f;

            if (GUI.Button(new Rect(x, y, width, 42f), "Iniciar partida"))
            {
                SceneManager.LoadScene(SceneRoutes.Game);
            }

            if (GUI.Button(new Rect(x, y + 52f, width, 36f), "Abrir entrenamiento"))
            {
                SceneManager.LoadScene(SceneRoutes.Training);
            }

            if (GUI.Button(new Rect(x, y + 96f, width, 36f), "Cerrar sesion"))
            {
                profile.Logout();
            }

            if (GUI.Button(new Rect(x, y + 140f, width, 36f), "Salir"))
            {
                Application.Quit();
            }

            GUI.Label(
                new Rect(x, rect.y + 280f, width, 160f),
                "Flujo actual:\n- menu principal\n- partida survival\n- pausa con ESC\n- game over con reintento\n- estadisticas persistentes por usuario",
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

            GUI.Label(new Rect(x, y + 220f, width, 22f), "Historial reciente", sectionStyle);

            if (recentMatches.Count == 0)
            {
                GUI.Label(new Rect(x, y + 248f, width, 20f), "Sin partidas registradas todavia.", smallStyle);
                return;
            }

            for (int index = 0; index < recentMatches.Count && index < 6; index++)
            {
                ProfileService.MatchRecord match = recentMatches[index];
                float lineY = y + 248f + index * 38f;
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
