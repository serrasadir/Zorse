using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Systems
{
    // Sopa skill'i (5 seviye, skill-sistemi.md): Sv.1 hasar + menzil · Sv.2 yay 180° · Sv.3 arkaya da vurur + hasar ·
    // Sv.4 vuruş hızı ×1.3 · Sv.5 tam tur (360°) + hasar ×1.5. Hasar: Upgrade_Weapon → Per Level Value (vuruş başı).
    // Kart açıklamaları Upgrade_Weapon.asset'te (SkillSetSetup yazar; bu seviyelerle uyumlu tutulmalı).
    [CreateAssetMenu(fileName = "WeaponUpgradeEffect", menuName = "BlobSurvivor/Effects/Weapon Upgrade")]
    public class WeaponUpgradeEffect : UpgradeEffect
    {
        [SerializeField] private int _backSwingLevel = 3;
        [SerializeField] private int _wideArcLevel = 2;
        [SerializeField] private int _fastSwingLevel = 4;
        [SerializeField] private int _fullCircleLevel = 5;
        [Tooltip("Bu seviyelerde +hasar (Per Level Value) ve +menzil gelir; mekanik seviyesiyle aynıysa ikisi birden.")]
        [SerializeField] private int[] _damageLevels = { 1, 3 };
        [Tooltip("+hasar seviyelerinde menzile eklenir (gövde yüzeyinden).")]
        [SerializeField] private float _rangePerDamageLevel = 0.5f;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var stick = blobRoot.GetComponentInChildren<StickWeapon>(); // kart seçiminde, karede bir değil
            if (stick == null) return;

            // UpgradeSystem seviyeyi Apply'dan önce artırıyor: bu, az önce ulaşılan seviye.
            int level = LevelOf(data);

            if (level == _backSwingLevel) stick.EnableBackSwing();
            if (level == _wideArcLevel) stick.EnableWideArc();
            if (level == _fastSwingLevel) stick.EnableFastSwing();
            if (level == _fullCircleLevel) stick.EnableFullCircle();
            if (System.Array.IndexOf(_damageLevels, level) >= 0) stick.AddDamage(data.PerLevelValue, _rangePerDamageLevel);

            // Görünür sopa: form yeni mekanikle değişir.
            int form = level >= _fullCircleLevel ? 4 : level >= _fastSwingLevel ? 3 : level >= _wideArcLevel ? 2 : 1;
            blobRoot.GetComponent<WeaponVisual>()?.SetForm(form);
        }
    }
}
