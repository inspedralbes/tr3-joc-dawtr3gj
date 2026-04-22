using UnityEngine;

namespace TankArena2D
{
    [RequireComponent(typeof(Health), typeof(FactionMember), typeof(CircleCollider2D))]
    [RequireComponent(typeof(NetworkActor))]
    public sealed class RemotePlayerAvatar : MonoBehaviour
    {
        [SerializeField] private Transform turretTransform;
        [SerializeField] private Transform muzzleTransform;

        private Health health;
        private CircleCollider2D bodyCollider;
        private Renderer[] renderers;
        private Collider2D[] colliders;

        public Transform TurretTransform => turretTransform;
        public Transform MuzzleTransform => muzzleTransform;
        public Health Health => health;

        public static RemotePlayerAvatar Create(Transform parent, string displayName)
        {
            GameObject root = new(displayName);
            root.transform.SetParent(parent, false);

            CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.65f;

            FactionMember faction = root.AddComponent<FactionMember>();
            faction.SetFaction(Faction.Enemy);

            Health health = root.AddComponent<Health>();
            health.Configure(100f, false);

            NetworkActor actor = root.AddComponent<NetworkActor>();
            NameplateTarget nameplate = root.AddComponent<NameplateTarget>();
            nameplate.SetDisplayName(displayName);
            CombatantPresence presence = root.AddComponent<CombatantPresence>();
            presence.Configure(displayName, false, true);
            RemotePlayerAvatar avatar = root.AddComponent<RemotePlayerAvatar>();
            avatar.BuildVisuals();
            avatar.Cache();
            health.SetState(100f, false);
            actor.Configure(string.Empty, string.Empty, displayName, false);
            return avatar;
        }

        private void Awake()
        {
            if (turretTransform == null || muzzleTransform == null)
            {
                BuildVisuals();
            }

            Cache();
        }

        public void ApplyState(float x, float y, float bodyAngle, float turretAngle, float hp, float maxHp, bool alive)
        {
            transform.position = new Vector3(x, y, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, bodyAngle);
            turretTransform.rotation = Quaternion.Euler(0f, 0f, turretAngle);

            if (health.MaxHealth != maxHp)
            {
                health.Configure(maxHp, false);
            }

            health.SetState(hp, !alive);
            SetPresentationActive(alive);
        }

        public void ApplyDamageState(float hp, bool alive)
        {
            health.SetState(hp, !alive);
            SetPresentationActive(alive);
        }

        private void Cache()
        {
            health = GetComponent<Health>();
            bodyCollider = GetComponent<CircleCollider2D>();
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider2D>(true);
            bodyCollider.radius = 0.65f;
        }

        private void BuildVisuals()
        {
            SpriteRenderer body = gameObject.GetComponent<SpriteRenderer>();

            if (body == null)
            {
                body = gameObject.AddComponent<SpriteRenderer>();
            }

            body.sprite = ProceduralSpriteLibrary.Circle;
            body.color = new Color(0.92f, 0.36f, 0.36f, 1f);
            body.sortingOrder = 5;
            transform.localScale = new Vector3(1.35f, 1.35f, 1f);

            if (turretTransform == null)
            {
                GameObject turretRoot = new("Turret");
                turretRoot.transform.SetParent(transform, false);
                turretRoot.transform.localPosition = Vector3.zero;
                turretTransform = turretRoot.transform;

                SpriteRenderer turret = turretRoot.AddComponent<SpriteRenderer>();
                turret.sprite = ProceduralSpriteLibrary.Square;
                turret.color = new Color(0.28f, 0.16f, 0.16f, 1f);
                turret.sortingOrder = 6;
                turretRoot.transform.localScale = new Vector3(1.15f, 0.26f, 1f);

                GameObject muzzleRoot = new("Muzzle");
                muzzleRoot.transform.SetParent(turretRoot.transform, false);
                muzzleRoot.transform.localPosition = new Vector3(0.66f, 0f, 0f);
                muzzleTransform = muzzleRoot.transform;
            }
        }

        private void SetPresentationActive(bool active)
        {
            if (renderers == null || colliders == null)
            {
                Cache();
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = active;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = active;
            }
        }
    }
}
