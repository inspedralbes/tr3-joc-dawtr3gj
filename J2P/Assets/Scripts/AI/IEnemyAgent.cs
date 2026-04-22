using UnityEngine;

namespace TankArena2D
{
    public interface IEnemyAgent
    {
        void SetTarget(Transform newTarget);
        void ResetAgent(Vector2 position);
    }
}
