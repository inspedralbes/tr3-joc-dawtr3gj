using UnityEngine;

namespace TankArena2D
{
    public static class ProceduralSpriteLibrary
    {
        private static Sprite squareSprite;
        private static Sprite circleSprite;
        private static Sprite gridSprite;

        public static Sprite Square => squareSprite ? squareSprite : squareSprite = CreateSquareSprite();
        public static Sprite Circle => circleSprite ? circleSprite : circleSprite = CreateCircleSprite(64);
        public static Sprite Grid => gridSprite ? gridSprite : gridSprite = CreateGridSprite(64);

        private static Sprite CreateSquareSprite()
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return CreateSprite(texture, 1f);
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = distance <= radius ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return CreateSprite(texture, size);
        }

        private static Sprite CreateGridSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool majorLine = x == 0 || y == 0 || x == size / 2 || y == size / 2;
                    bool minorLine = x % 16 == 0 || y % 16 == 0;
                    float shade = majorLine ? 1f : minorLine ? 0.9f : 0.82f;
                    texture.SetPixel(x, y, new Color(shade, shade, shade, 1f));
                }
            }

            texture.Apply();
            return CreateSprite(texture, 16f);
        }

        private static Sprite CreateSprite(Texture2D texture, float pixelsPerUnit)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);

            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
