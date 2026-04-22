using UnityEngine;

namespace TankArena2D
{
    public enum ArenaObstacleType
    {
        Cover,
        Boundary
    }

    public sealed class ArenaObstacle : MonoBehaviour
    {
        [SerializeField] private ArenaObstacleType obstacleType = ArenaObstacleType.Cover;

        public ArenaObstacleType ObstacleType => obstacleType;

        public void Configure(ArenaObstacleType type)
        {
            obstacleType = type;
        }
    }
}
