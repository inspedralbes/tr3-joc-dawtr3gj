using UnityEngine;

namespace TankArena2D
{
    public sealed class MinimapOverlay : MonoBehaviour
    {
        [SerializeField] private RenderTexture minimapTexture;
        [SerializeField] private string title = "Minimap";
        [SerializeField] private Vector2 panelPosition = new Vector2(16f, 16f);
        [SerializeField] private Vector2 panelSize = new Vector2(236f, 252f);
        [SerializeField] private Vector2 texturePadding = new Vector2(12f, 34f);

        private GUIStyle titleStyle;

        public void Configure(RenderTexture texture, string overlayTitle = "Minimap")
        {
            minimapTexture = texture;
            title = overlayTitle;
        }

        private void OnGUI()
        {
            if (minimapTexture == null)
            {
                return;
            }

            EnsureStyles();

            Rect outer = new Rect(panelPosition.x, panelPosition.y, panelSize.x, panelSize.y);
            Rect inner = new Rect(
                panelPosition.x + texturePadding.x,
                panelPosition.y + texturePadding.y,
                panelSize.x - texturePadding.x * 2f,
                panelSize.y - texturePadding.y - 12f);

            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.Box(outer, GUIContent.none);
            GUI.color = Color.white;

            GUI.Label(new Rect(outer.x + 12f, outer.y + 8f, outer.width - 24f, 22f), title, titleStyle);
            GUI.DrawTexture(inner, minimapTexture, ScaleMode.StretchToFill, false);
            GUI.Box(inner, GUIContent.none);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        private void OnDestroy()
        {
            if (minimapTexture != null)
            {
                minimapTexture.Release();
            }
        }
    }
}
