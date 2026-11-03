using UnityEngine;
using UnityEngine.AI;

namespace BlobSurvivor.Systems
{
    // EnemySpawner/ConsumableSpawner/PedestrianSpawner üçünün de aynı ihtiyacı vardı: greybox
    // şehir objeleri (bina vb.) NavMesh'te delik açtığında rastgele bir aday nokta o deliğin
    // içine denk gelebilir — bu, adayı en yakın geçerli NavMesh noktasına snap eder, bulamazsa
    // (örn. bina tam ortasına denk geldi ve sampleRadius yetmedi) false döner, çağıran taraf o
    // spawn denemesini atlar (bir sonraki tick'te yeni bir rastgele nokta denenir).
    public static class SpawnPositionUtility
    {
        // 2026-11-03 (Serra: "consumable'lar binaların içinde spawn oluyor"): NavMesh render mesh'lerden bake
        // ediliyor; kapalı kutu binaların içi ve çatısı da yürünebilir sayılabiliyor. NavMesh noktası tek başına
        // yetmez — yer seviyesinde olmalı ve orada katı bir şey (bina engeli, şehir objesi, harita duvarı) olmamalı.
        private const float MaxGroundY = 0.6f;     // daha yüksekteki NavMesh noktası çatı/teras demek
        private const float ClearanceRadius = 0.35f;
        private const float ClearanceCheckY = 0.5f;
        // Environment(16) + ConsumableTier1-5(9-13); trigger'lar (yenebilir objeler, pickup'lar) sayılmaz.
        private const int BlockingMask = (1 << 16) | (1 << 9) | (1 << 10) | (1 << 11) | (1 << 12) | (1 << 13);

        public static bool TryFindNavMeshPosition(Vector3 candidate, float yOverride, float sampleRadius, out Vector3 result)
        {
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas)
                && hit.position.y <= MaxGroundY
                && IsClear(hit.position))
            {
                result = new Vector3(hit.position.x, yOverride, hit.position.z);
                return true;
            }

            result = Vector3.zero;
            return false;
        }

        public static bool IsClear(Vector3 position) =>
            !Physics.CheckSphere(new Vector3(position.x, ClearanceCheckY, position.z), ClearanceRadius,
                BlockingMask, QueryTriggerInteraction.Ignore);

        // Büyük harita (GDD_v2 Karar 15): tüm haritaya eşit dağıtmak 136x136'da 15 polisi oyuncudan
        // koparır. Blob'un etrafındaki kare pencerede eşit dağılır, pencere harita sınırına kırpılır.
        // Küçük haritada (mapHalfExtent <= range) pencere tüm haritayı kapsar: eski davranışın aynısı.
        public static Vector3 RandomPointNearBlob(Vector3 blobPos, float range, float mapHalfExtent, float y)
        {
            float cx = Mathf.Clamp(blobPos.x, -mapHalfExtent, mapHalfExtent);
            float cz = Mathf.Clamp(blobPos.z, -mapHalfExtent, mapHalfExtent);
            return new Vector3(
                Random.Range(Mathf.Max(-mapHalfExtent, cx - range), Mathf.Min(mapHalfExtent, cx + range)),
                y,
                Random.Range(Mathf.Max(-mapHalfExtent, cz - range), Mathf.Min(mapHalfExtent, cz + range)));
        }
    }
}
