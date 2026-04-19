using UnityEngine;

namespace TankArena2D
{
    public sealed class NameplateTarget : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField] private Vector3 worldOffset = new(0f, 1.55f, 0f);

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    return displayName;
                }

                NetworkActor actor = GetComponent<NetworkActor>();

                if (actor != null && !string.IsNullOrWhiteSpace(actor.UserName))
                {
                    return actor.UserName;
                }

                return gameObject.name;
            }
        }

        public Vector3 WorldOffset => worldOffset;

        public bool ShouldDisplay
        {
            get
            {
                Health health = GetComponent<Health>();
                return health == null || !health.IsDead;
            }
        }

        public void SetDisplayName(string value)
        {
            displayName = value;
        }
    }
}
