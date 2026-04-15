using UnityEngine;

namespace TankArena2D
{
    public readonly struct DamageInfo
    {
        public DamageInfo(float amount, GameObject source, Vector2 hitPoint, Vector2 direction)
        {
            Amount = amount;
            Source = source;
            HitPoint = hitPoint;
            Direction = direction;
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public Vector2 HitPoint { get; }
        public Vector2 Direction { get; }
    }
}
