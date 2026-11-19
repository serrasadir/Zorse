using TMPro;
using UnityEngine;

namespace BlobSurvivor.UI
{
    // UI_Glyphs sprite asset'i (Tools → Blob.io → UI Glif ve Şekillerini Kur). Font'ta olmayan işaretler buradan:
    // metne Apply ile bağlanır, <sprite> etiketi tint=1 ile yazının rengini alır.
    public static class UiGlyphs
    {
        public const string Star = "<sprite name=\"yildiz\" tint=1>";

        private static TMP_SpriteAsset s_asset;

        public static TMP_SpriteAsset Asset
        {
            get
            {
                if (s_asset == null) s_asset = Resources.Load<TMP_SpriteAsset>("Sprite Assets/UI_Glyphs");
                return s_asset;
            }
        }

        public static void Apply(TMP_Text text)
        {
            if (text != null && Asset != null) text.spriteAsset = Asset;
        }
    }
}
