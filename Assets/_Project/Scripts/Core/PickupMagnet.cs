using UnityEngine;

namespace BlobSurvivor.Core
{
    // Mıknatıs güçlendiricisi (Market): coin ve kalp, blob gövdesinin Radius kadar yakınındaysa ona kayar.
    // Run başında GameManager ayarlar; Radius 0 = kapalı. Toplama yine blob tetikleyicisiyle olur.
    public static class PickupMagnet
    {
        private const float Speed = 9f;

        public static Transform Blob;
        public static float Radius;

        // Yatayda çeker; pozisyon değiştiyse true.
        public static bool Pull(ref Vector3 position)
        {
            if (Radius <= 0f || Blob == null) return false;
            Vector3 to = Blob.position - position;
            to.y = 0f;
            float reach = Radius + Blob.localScale.x * 0.5f;
            float sq = to.sqrMagnitude;
            if (sq > reach * reach || sq < 0.0001f) return false;
            float dist = Mathf.Sqrt(sq);
            position += to / dist * Mathf.Min(dist, Speed * Time.deltaTime);
            return true;
        }
    }
}
