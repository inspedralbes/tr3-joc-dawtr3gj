using UnityEngine;

namespace TankArena2D
{
    public sealed class Weapon : MonoBehaviour
    {
        public readonly struct ShotData
        {
            public ShotData(Vector2 origin, Vector2 direction, float speed, float lifetime, float damage, GameObject owner)
            {
                Origin = origin;
                Direction = direction;
                Speed = speed;
                Lifetime = lifetime;
                Damage = damage;
                Owner = owner;
            }

            public Vector2 Origin { get; }
            public Vector2 Direction { get; }
            public float Speed { get; }
            public float Lifetime { get; }
            public float Damage { get; }
            public GameObject Owner { get; }
        }

        [SerializeField] private Transform muzzle;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField, Min(0.05f)] private float fireCooldown = 0.3f;
        [SerializeField, Min(1f)] private float projectileSpeed = 18f;
        [SerializeField, Min(0.1f)] private float projectileLifetime = 2.25f;
        [SerializeField, Min(1f)] private float projectileDamage = 20f;
        [SerializeField, Min(1)] private int magazineSize = 8;
        [SerializeField, Min(0.1f)] private float reloadDuration = 1.5f;
        [SerializeField] private bool autoReloadOnEmpty = true;
        [SerializeField, Min(0f)] private float autoReloadDelay = 0.45f;
        [SerializeField] private Transform projectileContainer;

        private FactionMember factionMember;
        private Collider2D[] ownerColliders;
        private float nextShotTime;
        private float reloadEndTime;
        private float lastShotTime = float.NegativeInfinity;
        private int ammoInMagazine;
        private float externalCooldownMultiplier = 1f;

        public event System.Action<Weapon, ShotData> Fired;

        public Transform Muzzle => muzzle;
        public Projectile ProjectilePrefab => projectilePrefab;
        public bool CanFire => !IsReloading && Time.time >= nextShotTime && ammoInMagazine > 0;
        public float Cooldown => fireCooldown;
        public float EffectiveCooldown => fireCooldown * externalCooldownMultiplier;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public float ProjectileDamage => projectileDamage;
        public int MagazineSize => magazineSize;
        public int AmmoInMagazine => ammoInMagazine;
        public bool IsMagazineEmpty => ammoInMagazine <= 0;
        public bool IsReloading => reloadEndTime > 0f && Time.time < reloadEndTime;
        public bool AutoReloadOnEmpty => autoReloadOnEmpty;
        public float ReloadDuration => reloadDuration;
        public float CooldownRemainingNormalized => EffectiveCooldown <= 0.001f
            ? 0f
            : Mathf.Clamp01((nextShotTime - Time.time) / EffectiveCooldown);
        public float ReloadRemainingNormalized => reloadDuration <= 0.001f || !IsReloading
            ? 0f
            : Mathf.Clamp01((reloadEndTime - Time.time) / reloadDuration);

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
            ammoInMagazine = Mathf.Max(1, magazineSize);
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
            ammoInMagazine = Mathf.Clamp(ammoInMagazine <= 0 ? magazineSize : ammoInMagazine, 0, magazineSize);
        }

        public bool TryFire(Vector2 direction)
        {
            if (projectilePrefab == null || muzzle == null)
            {
                return false;
            }

            if (IsReloading)
            {
                return false;
            }

            if (ammoInMagazine <= 0)
            {
                if (autoReloadOnEmpty)
                {
                    StartReload();
                }

                return false;
            }

            if (Time.time < nextShotTime)
            {
                return false;
            }

            nextShotTime = Time.time + EffectiveCooldown;
            lastShotTime = Time.time;
            ammoInMagazine = Mathf.Max(0, ammoInMagazine - 1);
            Vector2 safeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

            Projectile projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity, projectileContainer);
            projectile.gameObject.SetActive(true);
            projectile.Launch(
                safeDirection,
                gameObject,
                factionMember != null ? factionMember.Faction : Faction.Neutral,
                projectileSpeed,
                projectileLifetime,
                projectileDamage,
                ownerColliders);
            Fired?.Invoke(this, new ShotData(muzzle.position, safeDirection, projectileSpeed, projectileLifetime, projectileDamage, gameObject));

            if (ammoInMagazine <= 0 && autoReloadOnEmpty)
            {
                StartReload();
            }

            return true;
        }

        public bool StartReload()
        {
            if (IsReloading || ammoInMagazine >= magazineSize)
            {
                return false;
            }

            nextShotTime = Mathf.Max(nextShotTime, Time.time + 0.05f);
            reloadEndTime = Time.time + reloadDuration;
            return true;
        }

        public void RefillMagazine()
        {
            ammoInMagazine = magazineSize;
            reloadEndTime = 0f;
        }

        public void SetExternalCooldownMultiplier(float multiplier)
        {
            externalCooldownMultiplier = Mathf.Max(0.05f, multiplier);
        }

        private void Update()
        {
            if (reloadEndTime > 0f && Time.time >= reloadEndTime)
            {
                RefillMagazine();
                return;
            }

            if (!IsReloading &&
                autoReloadOnEmpty &&
                ammoInMagazine < magazineSize &&
                Time.time >= lastShotTime + autoReloadDelay)
            {
                StartReload();
            }
        }
    }
}
