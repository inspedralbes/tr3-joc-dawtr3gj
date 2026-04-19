using UnityEngine;

namespace TankArena2D
{
    [RequireComponent(typeof(Weapon))]
    public sealed class ReloadFeedback : MonoBehaviour
    {
        [SerializeField] private Vector3 anchorOffset = new(0f, 1.45f, 0f);
        [SerializeField] private Color barBackgroundColor = new(0.03f, 0.06f, 0.11f, 0.72f);
        [SerializeField] private Color barFillColor = new(0.20f, 0.60f, 1f, 1f);
        [SerializeField] private Color spinnerColor = new(0.62f, 0.84f, 1f, 1f);

        private Weapon weapon;
        private Transform root;
        private Transform fill;
        private Transform spinner;

        private void Awake()
        {
            weapon = GetComponent<Weapon>();
            BuildVisuals();
            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (weapon == null)
            {
                return;
            }

            bool visible = weapon.IsReloading;
            SetVisible(visible);

            if (!visible)
            {
                return;
            }

            root.position = transform.position + anchorOffset;
            float progress = 1f - weapon.ReloadRemainingNormalized;
            fill.localScale = new Vector3(Mathf.Clamp01(progress), 1f, 1f);
            spinner.localRotation = Quaternion.Euler(0f, 0f, -Time.time * 540f);
        }

        private void BuildVisuals()
        {
            root = new GameObject("ReloadFx").transform;
            root.SetParent(transform, false);

            SpriteRenderer background = CreateBar("BarBack", root, barBackgroundColor, new Vector2(0.92f, 0.12f), 30);
            background.transform.localPosition = Vector3.zero;

            SpriteRenderer bar = CreateBar("BarFill", root, barFillColor, new Vector2(0.92f, 0.12f), 31);
            bar.transform.localPosition = Vector3.zero;
            fill = bar.transform;

            SpriteRenderer spinnerRenderer = CreateBar("Spinner", root, spinnerColor, new Vector2(0.18f, 0.18f), 32);
            spinnerRenderer.sprite = ProceduralSpriteLibrary.Circle;
            spinnerRenderer.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            spinner = spinnerRenderer.transform;
        }

        private static SpriteRenderer CreateBar(string name, Transform parent, Color color, Vector2 size, int sortingOrder)
        {
            GameObject node = new(name);
            node.transform.SetParent(parent, false);
            SpriteRenderer renderer = node.AddComponent<SpriteRenderer>();
            renderer.sprite = ProceduralSpriteLibrary.Square;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            node.transform.localScale = new Vector3(size.x, size.y, 1f);
            return renderer;
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.gameObject.SetActive(visible);
            }
        }
    }
}
