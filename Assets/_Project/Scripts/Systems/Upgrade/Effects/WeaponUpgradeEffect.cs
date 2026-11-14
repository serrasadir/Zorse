using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Systems
{
    // Sopa skill'i. Kilometre taşı seviyelerinde yeni yetenek, aradakiler +hasar/+menzil:
    //   Sv.3 arkaya da vurur · Sv.5 yay 180° · Sv.7 vuruş hızı ×1.3 · Sv.8 tam tur (360°) + hasar ×1.5
    // Kart açıklamaları Upgrade_Weapon.asset'in Level Descriptions listesinde (bu seviyelerle uyumlu tutulmalı).
    [CreateAssetMenu(fileName = "WeaponUpgradeEffect", menuName = "BlobSurvivor/Effects/Weapon Upgrade")]
    public class WeaponUpgradeEffect : UpgradeEffect
    {
        [SerializeField] private int _backSwingLevel = 3;
        [SerializeField] private int _wideArcLevel = 5;
        [SerializeField] private int _fastSwingLevel = 7;
        [SerializeField] private int _fullCircleLevel = 8;
        [Tooltip("+hasar seviyelerinde menzile eklenir (gövde yüzeyinden). Hasar: Upgrade_Weapon → Per Level Value.")]
        [SerializeField] private float _rangePerDamageLevel = 0.5f;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var stick = blobRoot.GetComponentInChildren<StickWeapon>(); // kart seçiminde, karede bir değil
            if (stick == null) return;

            // UpgradeSystem seviyeyi Apply'dan önce artırıyor — bu, az önce ulaşılan seviye.
            int level = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.GetLevel(data) : 1;

            if (level == _backSwingLevel) stick.EnableBackSwing();
            else if (level == _wideArcLevel) stick.EnableWideArc();
            else if (level == _fastSwingLevel) stick.EnableFastSwing();
            else if (level == _fullCircleLevel) stick.EnableFullCircle();
            else stick.AddDamage(data.PerLevelValue, _rangePerDamageLevel);

            // Görünür sopa: form yeni mekanikle değişir, +hasar seviyeleri modeli değiştirmez.
            int form = level >= _fullCircleLevel ? 4 : level >= _fastSwingLevel ? 3 : level >= _backSwingLevel ? 2 : 1;
            blobRoot.GetComponent<WeaponVisual>()?.SetForm(form);
        }
    }
}
