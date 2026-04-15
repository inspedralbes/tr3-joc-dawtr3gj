using UnityEngine;

namespace TankArena2D
{
    public enum Faction
    {
        Neutral,
        Player,
        Enemy
    }

    public sealed class FactionMember : MonoBehaviour
    {
        [SerializeField] private Faction faction = Faction.Neutral;

        public Faction Faction => faction;

        public void SetFaction(Faction value)
        {
            faction = value;
        }

        public bool IsHostileTo(FactionMember other)
        {
            return other != null &&
                   faction != Faction.Neutral &&
                   other.faction != Faction.Neutral &&
                   faction != other.faction;
        }

        public bool IsSameFaction(FactionMember other)
        {
            return other != null && faction == other.faction;
        }
    }
}
