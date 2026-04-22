using System;
using UnityEngine;

namespace TankArena2D
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class PowerupPickup : MonoBehaviour
    {
        private const float IconScale = 0.3f;

        private SpriteRenderer baseRenderer;
        private SpriteRenderer iconRenderer;
        private CircleCollider2D triggerCollider;
        private string powerupId;
        private PowerupType powerupType;
        private Action<PowerupPickup, PlayerController> collectedCallback;
        private bool collectionPending;

        public string PowerupId => powerupId;
        public PowerupType PowerupType => powerupType;

        private void Awake()
        {
            triggerCollider = GetComponent<CircleCollider2D>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 0.75f;

            EnsureVisuals();
        }

        public void Initialize(string id, PowerupType type, Action<PowerupPickup, PlayerController> onCollected)
        {
            powerupId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
            powerupType = type;
            collectedCallback = onCollected;
            collectionPending = false;
            gameObject.name = $"Powerup_{type}_{powerupId}";
            EnsureVisuals();
            ApplyVisualStyle(type);
            SetCollectable(true);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collectionPending ||
                other == null ||
                !other.TryGetComponent(out PlayerController player) ||
                !player.IsAlive)
            {
                return;
            }

            collectedCallback?.Invoke(this, player);
        }

        private void OnDestroy()
        {
            collectedCallback = null;
        }

        public void SetCollectable(bool isCollectable)
        {
            collectionPending = !isCollectable;

            if (triggerCollider != null)
            {
                triggerCollider.enabled = isCollectable;
            }

            float alpha = isCollectable ? 0.95f : 0.35f;

            if (baseRenderer != null)
            {
                Color color = baseRenderer.color;
                color.a = alpha;
                baseRenderer.color = color;
            }

            if (iconRenderer != null)
            {
                Color color = iconRenderer.color;
                color.a = isCollectable ? 0.95f : 0.35f;
                iconRenderer.color = color;
            }
        }

        private void EnsureVisuals()
        {
            if (baseRenderer == null)
            {
                baseRenderer = GetComponent<SpriteRenderer>();

                if (baseRenderer == null)
                {
                    baseRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            baseRenderer.sprite = ProceduralSpriteLibrary.Circle;
            baseRenderer.sortingOrder = 2;

            if (iconRenderer == null)
            {
                Transform iconTransform = transform.Find("Icon");

                if (iconTransform == null)
                {
                    GameObject iconObject = new GameObject("Icon");
                    iconObject.transform.SetParent(transform, false);
                    iconRenderer = iconObject.AddComponent<SpriteRenderer>();
                }
                else
                {
                    iconRenderer = iconTransform.GetComponent<SpriteRenderer>();

                    if (iconRenderer == null)
                    {
                        iconRenderer = iconTransform.gameObject.AddComponent<SpriteRenderer>();
                    }
                }
            }

            iconRenderer.transform.localPosition = Vector3.zero;
            iconRenderer.transform.localScale = Vector3.one * IconScale;
            iconRenderer.sortingOrder = 3;
        }

        private void ApplyVisualStyle(PowerupType type)
        {
            switch (type)
            {
                case PowerupType.Heal:
                    baseRenderer.color = new Color(0.18f, 0.8f, 0.35f, 0.95f);
                    iconRenderer.sprite = ProceduralSpriteLibrary.Square;
                    iconRenderer.color = Color.white;
                    iconRenderer.transform.localScale = Vector3.one * IconScale;
                    break;
                case PowerupType.SpeedBoost:
                    baseRenderer.color = new Color(1f, 0.78f, 0.16f, 0.95f);
                    iconRenderer.sprite = ProceduralSpriteLibrary.Circle;
                    iconRenderer.color = new Color(0.16f, 0.18f, 0.1f, 0.95f);
                    iconRenderer.transform.localScale = Vector3.one * (IconScale * 0.9f);
                    break;
                case PowerupType.RapidFire:
                    baseRenderer.color = new Color(1f, 0.36f, 0.2f, 0.95f);
                    iconRenderer.sprite = ProceduralSpriteLibrary.Square;
                    iconRenderer.color = new Color(0.22f, 0.06f, 0.02f, 0.95f);
                    iconRenderer.transform.localScale = new Vector3(IconScale * 0.45f, IconScale, 1f);
                    break;
                default:
                    baseRenderer.color = Color.white;
                    iconRenderer.sprite = ProceduralSpriteLibrary.Square;
                    iconRenderer.color = Color.black;
                    iconRenderer.transform.localScale = Vector3.one * IconScale;
                    break;
            }
        }
    }
}
