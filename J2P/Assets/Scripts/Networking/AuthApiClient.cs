using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TankArena2D
{
    public static class AuthApiClient
    {
        [Serializable]
        private sealed class CredentialsRequest
        {
            public string username;
            public string password;
        }

        [Serializable]
        private sealed class AuthResponse
        {
            public string token;
            public AuthUser user;
            public string message;
        }

        [Serializable]
        private sealed class AuthUser
        {
            public string id;
            public string username;
        }

        [Serializable]
        private sealed class ErrorResponse
        {
            public string message;
        }

        public readonly struct AuthResult
        {
            public AuthResult(bool success, string message, string userId, string userName, string token)
            {
                Success = success;
                Message = message;
                UserId = userId;
                UserName = userName;
                Token = token;
            }

            public bool Success { get; }
            public string Message { get; }
            public string UserId { get; }
            public string UserName { get; }
            public string Token { get; }
        }

        public static IEnumerator Register(string baseUrl, string userName, string password, Action<AuthResult> completed)
        {
            yield return SendCredentialsRequest($"{BackendSettings.NormalizeApiBaseUrl(baseUrl)}/api/auth/register", userName, password, completed);
        }

        public static IEnumerator Login(string baseUrl, string userName, string password, Action<AuthResult> completed)
        {
            yield return SendCredentialsRequest($"{BackendSettings.NormalizeApiBaseUrl(baseUrl)}/api/auth/login", userName, password, completed);
        }

        private static IEnumerator SendCredentialsRequest(string url, string userName, string password, Action<AuthResult> completed)
        {
            CredentialsRequest payload = new()
            {
                username = userName,
                password = password
            };

            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            using UnityWebRequest request = new(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                completed?.Invoke(new AuthResult(
                    true,
                    string.Empty,
                    response?.user?.id ?? string.Empty,
                    response?.user?.username ?? string.Empty,
                    response?.token ?? string.Empty));
                yield break;
            }

            string message = "No se pudo contactar con el backend.";

            if (!string.IsNullOrWhiteSpace(request.downloadHandler.text))
            {
                ErrorResponse error = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);

                if (!string.IsNullOrWhiteSpace(error?.message))
                {
                    message = error.message;
                }
            }

            completed?.Invoke(new AuthResult(false, message, string.Empty, string.Empty, string.Empty));
        }
    }
}
