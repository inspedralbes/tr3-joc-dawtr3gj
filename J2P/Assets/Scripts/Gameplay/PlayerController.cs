using UnityEngine;

namespace TankArena2D
{
    [RequireComponent(typeof(TankMovement2D), typeof(TurretAim), typeof(Weapon))]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private bool holdToFire = true;

        private TankMovement2D movement;
        private TurretAim turretAim;
        private Weapon weapon;
        private Health health;
        private Collider2D[] cachedColliders;
        private Renderer[] cachedRenderers;

        public Health Health => health;
        public bool IsAlive => health != null && !health.IsDead;

        private void Awake()
        {
            movement = GetComponent<TankMovement2D>();
            turretAim = GetComponent<TurretAim>();
            weapon = GetComponent<Weapon>();
            health = GetComponent<Health>();
            cachedColliders = GetComponentsInChildren<Collider2D>(true);
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            ApplyProfileName();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        public void Configure(Camera camera, bool allowHoldToFire = true)
        {
            playerCamera = camera;
            holdToFire = allowHoldToFire;
        }

        public void RespawnAt(Vector2 position)
        {
            transform.position = position;
            health.Revive();
            weapon.RefillMagazine();
            SetPresentationActive(true);
            movement.StopImmediate();
            enabled = true;
        }

        private void Update()
        {
            if (health == null || health.IsDead)
            {
                return;
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            Vector2 moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            movement.SetMoveInput(moveInput);

            AimAtMouse();

            bool firePressed = holdToFire ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);

            if (firePressed)
            {
                if (!weapon.TryFire(turretAim.Forward) &&
                    weapon.IsMagazineEmpty &&
                    weapon.AutoReloadOnEmpty)
                {
                    weapon.StartReload();
                }
            }
        }

        private void AimAtMouse()
        {
            if (playerCamera == null)
            {
                return;
            }

            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = Mathf.Abs(playerCamera.transform.position.z);
            Vector3 worldPosition = playerCamera.ScreenToWorldPoint(mousePosition);
            turretAim.AimAtWorldPoint(worldPosition);
        }

        private void HandleDeath(Health _, DamageInfo __)
        {
            movement.StopImmediate();
            SetPresentationActive(false);
            enabled = false;
        }

        private void ApplyProfileName()
        {
            if (!ProfileService.HasInstance || !ProfileService.Instance.IsLoggedIn)
            {
                return;
            }

            string userName = ProfileService.Instance.CurrentUserName;
            NameplateTarget nameplate = GetComponent<NameplateTarget>();

            if (nameplate != null)
            {
                nameplate.SetDisplayName(userName);
            }

            CombatantPresence presence = GetComponent<CombatantPresence>();

            if (presence != null)
            {
                presence.SetDisplayName(userName);
            }
        }

        private void SetPresentationActive(bool isActive)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                cachedRenderers[i].enabled = isActive;
            }

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                cachedColliders[i].enabled = isActive;
            }
        }
    }
}
