using UnityEngine;

namespace TankArena2D
{
    public sealed class TurretAim : MonoBehaviour
    {
        [SerializeField] private Transform turretTransform;

        public Transform TurretTransform => turretTransform != null ? turretTransform : transform;
        public Vector2 Forward => TurretTransform.right;

        private void Awake()
        {
            if (turretTransform == null)
            {
                turretTransform = transform.Find("Turret");

                if (turretTransform == null && transform.childCount > 0)
                {
                    turretTransform = transform.GetChild(0);
                }
            }
        }

        public void SetTurret(Transform turret)
        {
            turretTransform = turret;
        }

        public void AimAtWorldPoint(Vector2 worldPoint)
        {
            AimInDirection(worldPoint - (Vector2)TurretTransform.position);
        }

        public void AimInDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            TurretTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
