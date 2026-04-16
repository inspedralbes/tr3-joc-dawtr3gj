using UnityEngine;

namespace TankArena2D
{
    public static class EnemySteeringUtility
    {
        public static Vector2 BuildCombatMove(
            TankPerception2D perception,
            Vector2 toTarget,
            float attackRange,
            float preferredDistance,
            float retreatDistance,
            float strafeSign,
            float strafeStrength)
        {
            if (perception == null || toTarget.sqrMagnitude < 0.0001f)
            {
                return Vector2.zero;
            }

            float distance = toTarget.magnitude;
            Vector2 directionToTarget = toTarget / distance;
            Vector2 desired = Vector2.zero;

            if (!perception.HasLineOfSight || distance > preferredDistance)
            {
                desired += directionToTarget;
            }
            else if (distance < retreatDistance)
            {
                desired -= directionToTarget * 0.9f;
            }

            if (distance <= attackRange * 1.15f)
            {
                desired += Vector2.Perpendicular(directionToTarget) * strafeSign * strafeStrength;
            }

            return Vector2.ClampMagnitude(perception.GetBestDirection(desired), 1f);
        }

        public static Vector2 BuildSearchMove(TankPerception2D perception, Vector2 toLastKnownPosition, float moveStrength = 1f)
        {
            if (perception == null || toLastKnownPosition.sqrMagnitude < 0.04f)
            {
                return Vector2.zero;
            }

            return Vector2.ClampMagnitude(
                perception.GetBestDirection(toLastKnownPosition.normalized) * Mathf.Clamp01(moveStrength),
                1f);
        }

        public static Vector2 BuildPatrolMove(TankPerception2D perception, Vector2 patrolDirection, float moveStrength = 0.6f)
        {
            if (perception == null)
            {
                return Vector2.zero;
            }

            Vector2 desired = patrolDirection.sqrMagnitude > 0.0001f
                ? patrolDirection.normalized
                : Vector2.right;

            return Vector2.ClampMagnitude(
                perception.GetBestDirection(desired) * Mathf.Clamp01(moveStrength),
                1f);
        }
    }
}
