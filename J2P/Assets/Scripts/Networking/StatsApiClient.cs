using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TankArena2D
{
    public static class StatsApiClient
    {
        [Serializable]
        public struct RemoteStats
        {
            public int matchesPlayed;
            public int totalKills;
            public int bestKillsInMatch;
            public int lastKills;
            public int bestScore;
            public int lastScore;
            public float totalPlayTime;
            public float bestSurvivalTime;
            public float lastSurvivalTime;
        }

        [Serializable]
        public struct LeaderboardEntry
        {
            public string userId;
            public string username;
            public RemoteStats stats;
        }

        [Serializable]
        private sealed class LeaderboardEnvelope
        {
            public LeaderboardEntry[] leaderboard;
            public string message;
        }

        [Serializable]
        private sealed class MatchStatsRequest
        {
            public int score;
            public int kills;
            public float survivalTime;
        }

        [Serializable]
        private sealed class MatchStatsEnvelope
        {
            public bool ok;
            public RemoteStats stats;
            public string message;
        }

        [Serializable]
        private sealed class MyStatsEnvelope
        {
            public RemoteUser user;
            public string message;
        }

        [Serializable]
        private sealed class RemoteUser
        {
            public string id;
            public string username;
            public RemoteStats stats;
        }

        [Serializable]
        private sealed class ErrorEnvelope
        {
            public string message;
        }

        public static IEnumerator GetLeaderboard(string baseUrl, Action<IReadOnlyList<LeaderboardEntry>, string> completed)
        {
            using UnityWebRequest request = UnityWebRequest.Get($"{BackendSettings.NormalizeApiBaseUrl(baseUrl)}/api/stats/leaderboard");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                LeaderboardEnvelope envelope = JsonUtility.FromJson<LeaderboardEnvelope>(request.downloadHandler.text);
                completed?.Invoke(envelope?.leaderboard ?? Array.Empty<LeaderboardEntry>(), string.Empty);
                yield break;
            }

            completed?.Invoke(Array.Empty<LeaderboardEntry>(), ResolveError(request, "No se pudo cargar el ranking."));
        }

        public static IEnumerator GetMyStats(string baseUrl, string token, Action<RemoteStats?, string> completed)
        {
            using UnityWebRequest request = UnityWebRequest.Get($"{BackendSettings.NormalizeApiBaseUrl(baseUrl)}/api/stats/me");
            request.SetRequestHeader("Authorization", $"Bearer {token}");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                MyStatsEnvelope envelope = JsonUtility.FromJson<MyStatsEnvelope>(request.downloadHandler.text);
                completed?.Invoke(envelope?.user.stats, string.Empty);
                yield break;
            }

            completed?.Invoke(null, ResolveError(request, "No se pudieron recuperar tus estadisticas."));
        }

        public static IEnumerator SubmitMatch(string baseUrl, string token, int score, int kills, float survivalTime, Action<RemoteStats?, string> completed)
        {
            MatchStatsRequest payload = new()
            {
                score = Mathf.Max(0, score),
                kills = Mathf.Max(0, kills),
                survivalTime = Mathf.Max(0f, survivalTime),
            };

            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            using UnityWebRequest request = new($"{BackendSettings.NormalizeApiBaseUrl(baseUrl)}/api/stats/match", UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {token}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                MatchStatsEnvelope envelope = JsonUtility.FromJson<MatchStatsEnvelope>(request.downloadHandler.text);
                completed?.Invoke(envelope?.stats, string.Empty);
                yield break;
            }

            completed?.Invoke(null, ResolveError(request, "No se pudieron guardar las estadisticas."));
        }

        private static string ResolveError(UnityWebRequest request, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(request.downloadHandler?.text))
            {
                ErrorEnvelope envelope = JsonUtility.FromJson<ErrorEnvelope>(request.downloadHandler.text);

                if (!string.IsNullOrWhiteSpace(envelope?.message))
                {
                    return envelope.message;
                }
            }

            return fallback;
        }
    }
}
