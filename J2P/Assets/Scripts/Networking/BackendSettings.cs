using UnityEngine;

namespace TankArena2D
{
    public static class BackendSettings
    {
        public const string DefaultApiBaseUrl = "http://127.0.0.1:3100";

        public static string NormalizeApiBaseUrl(string url)
        {
            string resolved = string.IsNullOrWhiteSpace(url) ? DefaultApiBaseUrl : url.Trim();
            return resolved.TrimEnd('/');
        }

        public static string ToWebSocketUrl(string apiBaseUrl)
        {
            string normalized = NormalizeApiBaseUrl(apiBaseUrl);

            if (normalized.StartsWith("https://"))
            {
                return $"wss://{normalized[8..]}/ws";
            }

            if (normalized.StartsWith("http://"))
            {
                return $"ws://{normalized[7..]}/ws";
            }

            return $"ws://{normalized}/ws";
        }
    }
}
