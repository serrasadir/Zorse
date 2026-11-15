using BlobSurvivor.Core;

namespace BlobSurvivor.UI
{
    // Tier'ların oyuncuya görünen Türkçe adları (hud.md §4.2). Sabit string: çağrıda tahsis yok.
    public static class TierNames
    {
        public static string Get(BlobTier tier)
        {
            switch (tier)
            {
                case BlobTier.Tiny: return "MİNİK";
                case BlobTier.Little: return "UFAK";
                case BlobTier.Small: return "KÜÇÜK";
                case BlobTier.Medium: return "ORTA";
                case BlobTier.Large: return "BÜYÜK";
                case BlobTier.Giant: return "KOCAMAN";
                case BlobTier.Colossal: return "DEV";
                default: return "";
            }
        }
    }
}
