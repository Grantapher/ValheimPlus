using System.IO;
using UnityEngine;
using UnityEngine.UI;
using ValheimPlus.Utility;
using ValheimPlus;

namespace ValheimPlus.UI
{
    public static class VPlusMainMenu
    {
        public static Sprite VPlusLogoSprite;
        public static void Load()
        {
            //Load the logo from embedded asset
            Stream logoStream = EmbeddedAsset.LoadEmbeddedAsset("Assets.logo.png");
            if (logoStream == null)
            {
                ValheimPlusPlugin.Logger?.LogWarning("VPlus logo asset not found; skipping logo load.");
                return;
            }

            Texture2D logoTexture = Helper.LoadPng(logoStream);
            logoStream.Dispose();

            if (logoTexture == null)
            {
                ValheimPlusPlugin.Logger?.LogWarning("VPlus logo texture failed to load; skipping logo sprite creation.");
                return;
            }

            VPlusLogoSprite = Sprite.Create(logoTexture, new Rect(0, 0, logoTexture.width, logoTexture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
