using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Systems
{
    // Evrim "Bayram Gecesi" (Havai Fişek 5 + Tetikçi): her roket düşmana çarpınca misketlere bölünür; misketler
    // yakındaki diğer düşmanlara uçar (FireworkProjectile.OnHitEnemy). Boss'a giden tek roketin gücü aynı kalır.
    [CreateAssetMenu(fileName = "FestivalNightEffect", menuName = "BlobSurvivor/Effects/Bayram Gecesi (Evolution)")]
    public class FestivalNightEffect : UpgradeEffect
    {
        [SerializeField] private int _misketCount = 3;
        [Tooltip("Misket hasarı = roketin temel hasarı × bu (Yara Öfkesi çarpanı dahil).")]
        [SerializeField] private float _misketDamageFactor = 0.25f;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            FireworkWeapon weapon = blobRoot.GetComponentInChildren<FireworkWeapon>(); // kart seçiminde, bir kez
            if (weapon == null || weapon.HasCluster) return;
            weapon.EnableCluster(_misketCount, _misketDamageFactor);
            Haptics.Impact(2);
        }
    }
}
