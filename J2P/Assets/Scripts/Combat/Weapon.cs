using UnityEngine;

namespace TankArena2D
{
    public sealed class Weapon : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField, Min(0.05f)] private float fireCooldown = 0.3f;
        [SerializeField, Min(1f)] private float projectileSpeed = 18f;
        [SerializeField, Min(0.1f)] private float projectileLifetime = 2.25f;
        [SerializeField, Min(1f)] private float projectileDamage = 20f;
        [SerializeField] private Transform projectileContainer;

        private FactionMember factionMember;
        private Collider2D[] ownerColliders;
        private float nextShotTime;

        public Transform Muzzle => muzzle;
        public bool CanFire => Time.time >= nextShotTime;
        public float Cooldown => fireCooldown;
        public float CooldownRemainingNormalized => fireCooldown <= 0.001f
            ? 0f
            : Mathf.Clamp01((nextShotTime - Time.time) / fireCooldown);

        private void Awake()
        {
            if (muzzle == null)
            {
                muzzle = transform.Find("Turret/Muzzle");

                if (muzzle == null)
                {
                    muzzle = transform.Find("Muzzle");
                }
            }

            factionMember = GetComponent<FactionMember>();
            ownerColliders = GetComponentsInChildren<Collider2D>(true);
        }

        public void Configure(
            Projectile prefab,
            Transform muzzleTransform,
            float cooldown,
            float speed,
            float lifetime,
            float damage,
            Transform container = null)
        {
            projectilePrefab = prefab;
            muzzle = muzzleTransform;
            fireCooldown = Mathf.Max(0.05f, cooldown);
            projectileSpeed = Mathf.Max(1f, speed);
            projectileLifetime = Mathf.Max(0.1f, lifetime);
            projectileDamage = Mathf.Max(1f, damage);
            projectileContainer = container;
            ownerColliders = GetComponentsInChildren<Collider2D>(true);
        }

        public bool TryFire(Vector2 direction)
        {
            if (!CanFire || projectilePrefab == null || muzzle == null)
            {
                return false;
            }

            nextShotTime = Time.time + fireCooldown;

            Projectile projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity, projectileContainer);
            projectile.gameObject.SetActive(true);
            projectile.Launch(
                direction,
                gameObject,
                factionMember != null ? factionMember.Faction : Faction.Neutral,
                projectileSpeed,
                projectileLifetime,
                projectileDamage,
                ownerColliders);

            return true;
        }
    }
}
