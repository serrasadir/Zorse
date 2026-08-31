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
        public static bool TryFindNavMeshPosition(Vector3 candidate, float yOverride, float sampleRadius, out Vector3 result)
        {
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
            {
                result = new Vector3(hit.position.x, yOverride, hit.position.z);
                return true;
            }

            result = Vector3.zero;
            return false;
        }
    }
}
