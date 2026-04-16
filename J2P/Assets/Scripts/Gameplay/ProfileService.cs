using System;
using System.Collections.Generic;
using UnityEngine;

namespace TankArena2D
{
    public sealed class ProfileService : MonoBehaviour
    {
        [Serializable]
        public struct ProfileStats
        {
            public int MatchesPlayed;
            public int TotalKills;
            public int BestKillsInMatch;
            public int LastKills;
            public int BestScore;
            public int LastScore;
            public float TotalPlayTime;
            public float BestSurvivalTime;
            public float LastSurvivalTime;
        }

        [Serializable]
        public struct MatchRecord
        {
            public string UserName;
            public int Score;
            public int Kills;
            public float SurvivalTime;
            public string PlayedAtUtc;
        }

        [Serializable]
        public struct LeaderboardEntry
        {
            public string UserName;
            public int BestScore;
            public int BestKillsInMatch;
            public int MatchesPlayed;
            public float BestSurvivalTime;
        }

        [Serializable]
        private struct MatchHistoryCollection
        {
            public MatchRecord[] Matches;
        }

        [Serializable]
        private struct UserCollection
        {
            public string[] Users;
        }

        private const string CurrentUserKey = "tankarena.current_user";
        private const string UsersKey = "tankarena.users";
        private const int MaxStoredMatches = 8;

        private static ProfileService instance;

        private ProfileStats currentStats;
        private readonly List<MatchRecord> recentMatches = new List<MatchRecord>();
        private float currentMatchStartTime;
        private bool matchRunning;

        public static ProfileService Instance => EnsureInstance();
        public static bool HasInstance => instance != null;
        public string CurrentUserName { get; private set; }
        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(CurrentUserName);
        public ProfileStats CurrentStats => currentStats;
        public IReadOnlyList<MatchRecord> RecentMatches => recentMatches;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static ProfileService EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindAnyObjectByType<ProfileService>();

            if (instance != null)
            {
                DontDestroyOnLoad(instance.gameObject);
                instance.Initialize();
                return instance;
            }

            GameObject root = new GameObject("ProfileService");
            instance = root.AddComponent<ProfileService>();
            DontDestroyOnLoad(root);
            instance.Initialize();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        public bool Login(string userName)
        {
            string normalized = NormalizeUserName(userName);

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            CurrentUserName = normalized;
            RegisterUser(CurrentUserName);
            PlayerPrefs.SetString(CurrentUserKey, CurrentUserName);
            LoadStats();
            LoadMatchHistory();
            return true;
        }

        public void Logout()
        {
            EndMatch(0, 0f, 0, false);
            CurrentUserName = string.Empty;
            currentStats = default;
            recentMatches.Clear();
            PlayerPrefs.DeleteKey(CurrentUserKey);
            PlayerPrefs.Save();
        }

        public void BeginMatch()
        {
            if (!IsLoggedIn || matchRunning)
            {
                return;
            }

            matchRunning = true;
            currentMatchStartTime = Time.unscaledTime;
        }

        public void EndMatch(int score, float survivalTime, int kills, bool countMatch = true)
        {
            if (!IsLoggedIn || !matchRunning)
            {
                return;
            }

            matchRunning = false;

            if (!countMatch)
            {
                return;
            }

            float resolvedSurvivalTime = survivalTime > 0f
                ? survivalTime
                : Mathf.Max(0f, Time.unscaledTime - currentMatchStartTime);

            currentStats.MatchesPlayed += 1;
            currentStats.TotalKills += Mathf.Max(0, kills);
            currentStats.LastKills = Mathf.Max(0, kills);
            currentStats.BestKillsInMatch = Mathf.Max(currentStats.BestKillsInMatch, currentStats.LastKills);
            currentStats.LastScore = Mathf.Max(0, score);
            currentStats.BestScore = Mathf.Max(currentStats.BestScore, currentStats.LastScore);
            currentStats.LastSurvivalTime = resolvedSurvivalTime;
            currentStats.BestSurvivalTime = Mathf.Max(currentStats.BestSurvivalTime, resolvedSurvivalTime);
            currentStats.TotalPlayTime += resolvedSurvivalTime;

            MatchRecord record = new MatchRecord
            {
                UserName = CurrentUserName,
                Score = currentStats.LastScore,
                Kills = currentStats.LastKills,
                SurvivalTime = resolvedSurvivalTime,
                PlayedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            recentMatches.Insert(0, record);

            if (recentMatches.Count > MaxStoredMatches)
            {
                recentMatches.RemoveRange(MaxStoredMatches, recentMatches.Count - MaxStoredMatches);
            }

            SaveStats();
            SaveMatchHistory();
        }

        public IReadOnlyList<LeaderboardEntry> GetLeaderboard()
        {
            List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
            List<string> users = GetAllUsers();

            for (int index = 0; index < users.Count; index++)
            {
                string user = users[index];
                string json = PlayerPrefs.GetString(GetStatsKey(user), string.Empty);

                if (string.IsNullOrWhiteSpace(json))
                {
                    continue;
                }

                ProfileStats stats = JsonUtility.FromJson<ProfileStats>(json);
                entries.Add(new LeaderboardEntry
                {
                    UserName = user,
                    BestScore = stats.BestScore,
                    BestKillsInMatch = stats.BestKillsInMatch,
                    MatchesPlayed = stats.MatchesPlayed,
                    BestSurvivalTime = stats.BestSurvivalTime
                });
            }

            entries.Sort((left, right) =>
            {
                int scoreCompare = right.BestScore.CompareTo(left.BestScore);

                if (scoreCompare != 0)
                {
                    return scoreCompare;
                }

                int killsCompare = right.BestKillsInMatch.CompareTo(left.BestKillsInMatch);

                if (killsCompare != 0)
                {
                    return killsCompare;
                }

                return right.BestSurvivalTime.CompareTo(left.BestSurvivalTime);
            });

            return entries;
        }

        private void Initialize()
        {
            if (!string.IsNullOrWhiteSpace(CurrentUserName))
            {
                return;
            }

            CurrentUserName = NormalizeUserName(PlayerPrefs.GetString(CurrentUserKey, string.Empty));

            if (IsLoggedIn)
            {
                LoadStats();
                LoadMatchHistory();
            }
        }

        private void LoadStats()
        {
            string json = PlayerPrefs.GetString(GetStatsKey(CurrentUserName), string.Empty);
            currentStats = string.IsNullOrWhiteSpace(json)
                ? default
                : JsonUtility.FromJson<ProfileStats>(json);
        }

        private void LoadMatchHistory()
        {
            recentMatches.Clear();
            string json = PlayerPrefs.GetString(GetHistoryKey(CurrentUserName), string.Empty);

            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            MatchHistoryCollection collection = JsonUtility.FromJson<MatchHistoryCollection>(json);

            if (collection.Matches == null)
            {
                return;
            }

            recentMatches.AddRange(collection.Matches);
        }

        private void SaveStats()
        {
            if (!IsLoggedIn)
            {
                return;
            }

            PlayerPrefs.SetString(GetStatsKey(CurrentUserName), JsonUtility.ToJson(currentStats));
            PlayerPrefs.Save();
        }

        private void SaveMatchHistory()
        {
            if (!IsLoggedIn)
            {
                return;
            }

            MatchHistoryCollection collection = new MatchHistoryCollection
            {
                Matches = recentMatches.ToArray()
            };

            PlayerPrefs.SetString(GetHistoryKey(CurrentUserName), JsonUtility.ToJson(collection));
            PlayerPrefs.Save();
        }

        private void RegisterUser(string userName)
        {
            List<string> users = GetAllUsers();
            bool alreadyExists = false;

            for (int index = 0; index < users.Count; index++)
            {
                if (string.Equals(users[index], userName, StringComparison.OrdinalIgnoreCase))
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                users.Add(userName);
                UserCollection collection = new UserCollection
                {
                    Users = users.ToArray()
                };

                PlayerPrefs.SetString(UsersKey, JsonUtility.ToJson(collection));
                PlayerPrefs.Save();
            }
        }

        private List<string> GetAllUsers()
        {
            string json = PlayerPrefs.GetString(UsersKey, string.Empty);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            UserCollection collection = JsonUtility.FromJson<UserCollection>(json);
            return collection.Users != null ? new List<string>(collection.Users) : new List<string>();
        }

        private static string NormalizeUserName(string userName)
        {
            return string.IsNullOrWhiteSpace(userName) ? string.Empty : userName.Trim();
        }

        private static string GetStatsKey(string userName)
        {
            return $"tankarena.profile.{userName.ToLowerInvariant()}";
        }

        private static string GetHistoryKey(string userName)
        {
            return $"tankarena.profile.history.{userName.ToLowerInvariant()}";
        }
    }
}
