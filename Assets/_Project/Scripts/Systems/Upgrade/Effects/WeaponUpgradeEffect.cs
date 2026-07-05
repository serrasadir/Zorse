using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Systems
{
    [CreateAssetMenu(fileName = "WeaponUpgradeEffect", menuName = "BlobSurvivor/Effects/Weapon Upgrade")]
    public class WeaponUpgradeEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var weapon = blobRoot.GetComponentInChildren<WeaponBase>();
            if (weapon == null) return;
            weapon.IncreaseDamage(data.PerLevelValue);
        }
    }
}
