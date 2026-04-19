using UnityEngine;

namespace TankArena2D
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float speed = 18f;
        [SerializeField, Min(0.1f)] private float lifetime = 2.5f;
        [SerializeField, Min(1f)] private float damage = 15f;

        private Rigidbody2D rb;
        private Collider2D projectileCollider;
        private GameObject owner;
        private Faction ownerFaction = Faction.Neutral;
        private bool launched;
        private float despawnTime;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            projectileCollider = GetComponent<Collider2D>();

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (projectileCollider != null)
            {
                projectileCollider.isTrigger = false;
            }
        }

        public void Configure(float newSpeed, float newLifetime, float newDamage)
        {
            speed = Mathf.Max(1f, newSpeed);
            lifetime = Mathf.Max(0.1f, newLifetime);
            damage = Mathf.Max(1f, newDamage);
        }

        public void Launch(
            Vector2 direction,
            GameObject ownerObject,
            Faction faction,
            float newSpeed,
            float newLifetime,
            float newDamage,
            Collider2D[] ignoredColliders)
        {
            owner = ownerObject;
            ownerFaction = faction;
            speed = Mathf.Max(1f, newSpeed);
            lifetime = Mathf.Max(0.1f, newLifetime);
            damage = Mathf.Max(1f, newDamage);
            launched = true;
            despawnTime = Time.time + lifetime;

            Vector2 safeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            transform.right = safeDirection;
            rb.linearVelocity = safeDirection * speed;

            if (projectileCollider != null && ignoredColliders != null)
            {
                for (int i = 0; i < ignoredColliders.Length; i++)
                {
                    Collider2D ignored = ignoredColliders[i];

                    if (ignored != null)
                    {
                        Physics2D.IgnoreCollision(projectileCollider, ignored, true);
                    }
                }
            }
        }

        private void Update()
        {
            if (launched && Time.time >= despawnTime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!launched || other == null)
            {
                return;
            }

            HandleImpact(other, other.ClosestPoint(transform.position));
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!launched || collision == null || collision.collider == null)
            {
                return;
            }

            ContactPoint2D contact = collision.contactCount > 0
                ? collision.GetContact(0)
                : default;

            Vector2 hitPoint = collision.contactCount > 0
                ? contact.point
                : collision.collider.ClosestPoint(transform.position);

            HandleImpact(collision.collider, hitPoint);
        }

        private void HandleImpact(Collider2D other, Vector2 hitPoint)
        {
            if (other == null)
            {
                return;
            }

            if (other.isTrigger && other.GetComponentInParent<Health>() == null)
            {
                return;
            }

            if (BelongsToOwner(other))
            {
                return;
            }

            FactionMember otherFaction = other.GetComponentInParent<FactionMember>();

            if (otherFaction != null &&
                otherFaction.Faction != Faction.Neutral &&
                otherFaction.Faction == ownerFaction)
            {
                return;
            }

            Health health = other.GetComponentInParent<Health>();
            NetworkActor targetActor = other.GetComponentInParent<NetworkActor>();
            NetworkActor ownerActor = owner != null ? owner.GetComponent<NetworkActor>() : null;

            if (targetActor != null && MultiplayerClient.Active != null)
            {
                if (ownerActor != null && ownerActor.IsLocalPlayer && !targetActor.IsLocalPlayer)
                {
                    MultiplayerClient.Active.ReportDamage(targetActor.NetworkId, damage, hitPoint);
                }

                Destroy(gameObject);
                return;
            }

            if (health != null && !health.IsDead)
            {
                health.ApplyDamage(new DamageInfo(damage, owner, hitPoint, rb.linearVelocity.normalized));
                Destroy(gameObject);
                return;
            }

            Destroy(gameObject);
        }

        private bool BelongsToOwner(Collider2D other)
        {
            return owner != null &&
                   (other.transform == owner.transform || other.transform.IsChildOf(owner.transform));
        }
    }
}
