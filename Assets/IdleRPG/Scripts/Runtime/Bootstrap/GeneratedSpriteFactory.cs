using UnityEngine;

namespace IdleRPG.Runtime.Bootstrap
{
    public static class GeneratedSpriteFactory
    {
        public static Sprite CreateUnitSprite()
        {
            Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.name = "Generated Unit Texture";
            texture.filterMode = FilterMode.Point;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
            sprite.name = "Generated Unit Sprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        public static Sprite CreateDiamondTileSprite()
        {
            const int width = 32;
            const int height = 16;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "Generated Diamond Tile Texture";
            texture.filterMode = FilterMode.Point;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[width * height];
            Color transparent = new Color(1f, 1f, 1f, 0f);
            Color opaque = Color.white;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalizedX = Mathf.Abs((x + 0.5f) / width - 0.5f) * 2f;
                    float normalizedY = Mathf.Abs((y + 0.5f) / height - 0.5f) * 2f;
                    pixels[y * width + x] = normalizedX + normalizedY <= 1f ? opaque : transparent;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), height);
            sprite.name = "Generated Diamond Tile Sprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        public static Sprite CreateSquareTileSprite()
        {
            const int width = 30;
            const int height = 30;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "Generated Square Tile Texture";
            texture.filterMode = FilterMode.Point;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color32[] pixels = new Color32[width * height];
            Color32 opaque = new Color32(255, 255, 255, 255);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = opaque;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), height);
            sprite.name = "Generated Square Tile Sprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
