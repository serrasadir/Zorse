using UnityEngine;

namespace BlobSurvivor.Core
{
    // Haritanın yürünebilir iç sınırı: CityBounds duvarlarının iç yüzü (bir kez okunur).
    // Collider'sız hareket eden (steering) birimler bununla son kontrol yapar — duvarın içinden
    // itilip suya düşmesinler. CityBounds yoksa sınır yok.
    public static class MapBounds
    {
        private static float s_halfExtent = -1f;

        public static float HalfExtent
        {
            get
            {
                if (s_halfExtent < 0f) s_halfExtent = Read();
                return s_halfExtent;
            }
        }

        public static Vector3 Clamp(Vector3 position, float radius)
        {
            float h = HalfExtent - radius;
            if (h <= 0f || float.IsInfinity(h)) return position;
            position.x = Mathf.Clamp(position.x, -h, h);
            position.z = Mathf.Clamp(position.z, -h, h);
            return position;
        }

        private static float Read()
        {
            GameObject bounds = GameObject.Find("CityBounds");
            if (bounds == null) return float.PositiveInfinity;

            float inner = float.PositiveInfinity;
            foreach (BoxCollider box in bounds.GetComponents<BoxCollider>())
            {
                Bounds b = box.bounds;
                bool alongZ = b.size.x < b.size.z; // x'te ince: doğu/batı duvarı
                float face = alongZ ? Mathf.Min(Mathf.Abs(b.min.x), Mathf.Abs(b.max.x))
                                    : Mathf.Min(Mathf.Abs(b.min.z), Mathf.Abs(b.max.z));
                inner = Mathf.Min(inner, face);
            }
            return inner;
        }
    }
}
