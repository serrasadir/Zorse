using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Systems
{
    // 2026-11-03 (Serra: "oyuncu hissetsin, level aldığıma değiyor desin"): Silah Gücü artık her seviyede
    // "+hasar" değil — kilometre taşı seviyelerinde yeni bir yetenek açılır, aradakiler hasar verir.
    //   Sv.3 çift hedef · Sv.5 delip geçme · Sv.7 üçlü hedef · Sv.8 karaktere özel son form
    // Kart açıklamaları Upgrade_Weapon.asset'in Level Descriptions listesinde (bu seviyelerle uyumlu tutulmalı).
    [CreateAssetMenu(fileName = "WeaponUpgradeEffect", menuName = "BlobSurvivor/Effects/Weapon Upgrade")]
    public class WeaponUpgradeEffect : UpgradeEffect
    {
        [SerializeField] private int _doubleTargetLevel = 3;
        [SerializeField] private int _pierceLevel = 5;
        [SerializeField] private int _tripleTargetLevel = 7;
        [SerializeField] private int _ultimateLevel = 8;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var weapon = blobRoot.GetComponentInChildren<WeaponBase>();
            if (weapon == null) return;

            // UpgradeSystem seviyeyi Apply'dan önce artırıyor — bu, az önce ulaşılan seviye.
            int level = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.GetLevel(data) : 1;

            if (level == _doubleTargetLevel) weapon.SetTargetCount(2);
            else if (level == _pierceLevel) weapon.SetPierce(1);
            else if (level == _tripleTargetLevel) weapon.SetTargetCount(3);
            else if (level == _ultimateLevel) weapon.EnableUltimate();
            else weapon.IncreaseDamage(data.PerLevelValue);
        }
    }
}
