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
    }
}
