using UnityEngine;

namespace TankArena2D
{
    public sealed class ArenaBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(42f, 28f);
        [SerializeField, Min(0f)] private float inset = 1.5f;

        public Vector2 Size => size;
        public Vector2 Center => transform.position;

        public Rect InnerRect
        {
            get
            {
                Vector2 halfSize = size * 0.5f;
                Vector2 center = Center;

                return Rect.MinMaxRect(
                    center.x - halfSize.x + inset,
                    center.y - halfSize.y + inset,
                    center.x + halfSize.x - inset,
                    center.y + halfSize.y - inset);
            }
        }

        public void Configure(Vector2 newSize, float newInset)
        {
            size = new Vector2(Mathf.Max(8f, newSize.x), Mathf.Max(8f, newSize.y));
            inset = Mathf.Max(0f, newInset);
        }

        public Vector2 ClampInside(Vector2 position, float padding = 0f)
        {
            Rect rect = InnerRect;
            float safePadding = GetSafePadding(rect, padding);

            return new Vector2(
                Mathf.Clamp(position.x, rect.xMin + safePadding, rect.xMax - safePadding),
                Mathf.Clamp(position.y, rect.yMin + safePadding, rect.yMax - safePadding));
        }

        public bool Contains(Vector2 position, float padding = 0f)
        {
            Rect rect = InnerRect;
            float safePadding = GetSafePadding(rect, padding);

            return position.x >= rect.xMin + safePadding &&
                   position.x <= rect.xMax - safePadding &&
                   position.y >= rect.yMin + safePadding &&
                   position.y <= rect.yMax - safePadding;
        }

        public Vector2 GetRandomPoint(float padding = 0f)
        {
            Rect rect = InnerRect;
            float safePadding = GetSafePadding(rect, padding);

            return new Vector2(
                Random.Range(rect.xMin + safePadding, rect.xMax - safePadding),
                Random.Range(rect.yMin + safePadding, rect.yMax - safePadding));
        }

        private static float GetSafePadding(Rect rect, float padding)
        {
            float maxPadding = Mathf.Max(0f, Mathf.Min(rect.width, rect.height) * 0.5f - 0.05f);
            return Mathf.Clamp(padding, 0f, maxPadding);
        }

        private void OnValidate()
        {
            size.x = Mathf.Max(8f, size.x);
            size.y = Mathf.Max(8f, size.y);
            inset = Mathf.Max(0f, inset);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Rect rect = InnerRect;
            Vector3 center = new Vector3(rect.center.x, rect.center.y, 0f);
            Vector3 gizmoSize = new Vector3(rect.width, rect.height, 0f);

            Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.25f);
            Gizmos.DrawCube(center, gizmoSize);
            Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.85f);
            Gizmos.DrawWireCube(center, gizmoSize);
        }
#endif
    }
}
