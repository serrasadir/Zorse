using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities;

namespace BlobSurvivor.Entities.Weapons
{
    public class PistolProjectile : Projectile
    {
        [SerializeField] private int _splitCount = 3;

        // 2026-11-03 (Mermo pasifi "büyük lokmayı parçala"): eskiden sadece havuzdan doğan Medium+ consumable'ları
        // parçalıyordu — havuz neredeyse hep Tiny ürettiği için pasif fiilen hiç çalışmıyordu. Artık blob'un
        // TAM 1 tier üstündeki her parçalanabilir obje (şehir objeleri dahil) blob'un yiyebileceği parçalara
        // bölünür. Daha büyükleri delip geçer — tabanca dev binaları silemez (eski bug buydu).
        protected override bool OnHitOther(Collider other)
        {
            // Bina engel kutusu (Environment) child'dır — parçalanacak obje parent'ta.
            ConsumableBase consumable = other.GetComponentInParent<ConsumableBase>();
            if (consumable == null || !consumable.CanBeSplit) return false;
            if ((int)consumable.RequiredTier != (int)GameEvents.CurrentBlobTier + 1) return false;

            ConsumableSpawner.Instance?.ConsumeAndSplit(consumable, _splitCount);
            ReturnToPool();
            return true;
        }
    }
}
