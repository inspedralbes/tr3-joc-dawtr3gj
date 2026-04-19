using UnityEngine;

namespace TankArena2D
{
    public sealed class CombatantPresence : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField] private bool allowAiTargeting = true;
        [SerializeField] private bool showInLeaderboard = true;

        private int baseKills;
        private int bonusKills;

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

        public int TotalKills => Mathf.Max(0, baseKills + bonusKills);
        public bool AllowAiTargeting => allowAiTargeting;
        public bool ShowInLeaderboard => showInLeaderboard;

        public void Configure(string newDisplayName, bool aiTargetingAllowed, bool includeInLeaderboard)
        {
            displayName = newDisplayName ?? string.Empty;
            allowAiTargeting = aiTargetingAllowed;
            showInLeaderboard = includeInLeaderboard;
            SyncNameplate();
        }

        public void SetDisplayName(string value)
        {
            displayName = value ?? string.Empty;
            SyncNameplate();
        }

        public void SetAllowAiTargeting(bool value)
        {
            allowAiTargeting = value;
        }

        public void SetShowInLeaderboard(bool value)
        {
            showInLeaderboard = value;
        }

        public void SetBaseKills(int value)
        {
            baseKills = Mathf.Max(0, value);
        }

        public void ResetLocalKills()
        {
            bonusKills = 0;
        }

        public void AddKill()
        {
            bonusKills += 1;
        }

        private void Awake()
        {
            SyncNameplate();
        }

        private void SyncNameplate()
        {
            NameplateTarget nameplate = GetComponent<NameplateTarget>();

            if (nameplate != null && !string.IsNullOrWhiteSpace(displayName))
            {
                nameplate.SetDisplayName(displayName);
            }
        }
    }
}
