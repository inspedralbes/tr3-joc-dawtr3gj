using UnityEngine;

namespace TankArena2D
{
    public sealed class NetworkActor : MonoBehaviour
    {
        [SerializeField] private string networkId;
        [SerializeField] private string userId;
        [SerializeField] private string userName;
        [SerializeField] private bool isLocalPlayer;

        public string NetworkId => networkId;
        public string UserId => userId;
        public string UserName => userName;
        public bool IsLocalPlayer => isLocalPlayer;

        public void Configure(string newNetworkId, string newUserId, string newUserName, bool localPlayer)
        {
            networkId = newNetworkId ?? string.Empty;
            userId = newUserId ?? string.Empty;
            userName = newUserName ?? string.Empty;
            isLocalPlayer = localPlayer;
        }
    }
}
