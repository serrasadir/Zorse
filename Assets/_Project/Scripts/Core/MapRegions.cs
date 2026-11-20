using UnityEngine;

namespace BlobSurvivor.Core
{
    // Bölgeli harita (kale): güneyden kuzeye bölgeler, sınırları z çizgileri (duvar + kapı). Bir şey (düşman, yaya,
    // yerdeki yiyecek, görev sandığı) sadece blob'un bulunduğu bölgede, oyun alanının içinde (|x| < Side) doğar; duvarın
    // öbür yanında doğup takılı kalmaz. Bölge yoksa (şehir) her yer serbest. CastleMap bileşeni sahneye göre kurar.
    public static class MapRegions
    {
        private static float[] s_lines; // küçükten büyüğe z
        private static float s_side = float.PositiveInfinity;
        private static Transform s_blob;
        private const float WallMargin = 1.4f; // duvara bu kadar yakın doğmasın
        private const float SideMargin = 1.2f;

        public static bool Active => s_lines != null && s_lines.Length > 0;
        public static float Side => s_side;
        public static int ZoneCount => Active ? s_lines.Length + 1 : 1;

        public static void Set(float[] zLines, float side)
        {
            s_lines = zLines;
            s_side = side > 0f ? side : float.PositiveInfinity;
            s_blob = null;
            if (s_lines != null) System.Array.Sort(s_lines);
        }

        public static void Clear()
        {
            s_lines = null;
            s_side = float.PositiveInfinity;
        }

        public static int ZoneOf(Vector3 p)
        {
            if (!Active) return 0;
            int zone = 0;
            foreach (float z in s_lines)
                if (p.z > z) zone++;
            return zone;
        }

        // Bu bölgenin kuzey sınırı (son bölgede yok).
        public static bool TryGetExitLine(int zone, out float z)
        {
            z = 0f;
            if (!Active || zone < 0 || zone >= s_lines.Length) return false;
            z = s_lines[zone];
            return true;
        }

        public static Vector3 BlobPosition
        {
            get
            {
                if (s_blob == null)
                {
                    GameObject blob = GameObject.FindWithTag("Blob"); // bölge kurulumundan sonra bir kez
                    if (blob == null) return Vector3.zero;
                    s_blob = blob.transform;
                }
                return s_blob.position;
            }
        }

        // Rastgele doğma adayını blob'un bölgesine ve oyun alanına çeker (bölge yoksa dokunmaz): aday çoğu zaman
        // reddedilmesin, yerdeki yiyecek ve düşman şehirdeki kadar yoğun doğsun.
        public static Vector3 ClampToZone(Vector3 p)
        {
            if (!Active) return p;
            float lim = s_side - SideMargin - 0.3f;
            p.x = Reflect(p.x, -lim, lim);
            int zone = ZoneOf(BlobPosition);
            float lo = zone > 0 ? s_lines[zone - 1] + WallMargin + 0.3f : float.NegativeInfinity;
            float hi = zone < s_lines.Length ? s_lines[zone] - WallMargin - 0.3f : float.PositiveInfinity;
            p.z = Reflect(p.z, lo, hi);
            return p;
        }

        // Sınırı aşan adayı içeri yansıtır (kırpmak hepsini duvar dibine yığardı), sonra kırpar.
        private static float Reflect(float v, float lo, float hi)
        {
            if (v > hi) v = hi - (v - hi);
            else if (v < lo) v = lo + (lo - v);
            return Mathf.Clamp(v, lo, hi);
        }

        // Aday nokta blob'la aynı bölgede, oyun alanında ve duvardan yeterince uzakta mı (bölge yoksa hep evet).
        public static bool AllowsSpawn(Vector3 p)
        {
            if (!Active) return true;
            if (Mathf.Abs(p.x) > s_side - SideMargin) return false;
            foreach (float z in s_lines)
                if (Mathf.Abs(p.z - z) < WallMargin) return false;
            return ZoneOf(p) == ZoneOf(BlobPosition);
        }
    }
}
