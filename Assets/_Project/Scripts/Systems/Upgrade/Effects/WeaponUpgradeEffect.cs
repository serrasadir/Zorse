using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Systems
{
    // Sopa (aktif, karakterin ana silahı; genel "Silah Gücü" kartı yok, design/gdd/skill-sistemi.md):
    //   Sv.1 hasar +5, menzil +0.5 · Sv.2 ★ arkaya da vurur · Sv.3 ★ yay 180°, hasar +5 · Sv.4 ★ vuruş hızı ×1.3 ·
    //   Sv.5 ★ tam tur (360°), hasar ×1.5. WeaponBase'in sopa yolu: TargetCount 2 = arka, Pierce = 180°,
    //   TargetCount 3 = hızlı, Ultimate = 360°. PerLevelValue (2) × saniyede 2.5 vuruş-eşdeğeri = vuruş başı +5.
    [CreateAssetMenu(fileName = "WeaponUpgradeEffect", menuName = "BlobSurvivor/Effects/Weapon Upgrade")]
    public class WeaponUpgradeEffect : UpgradeEffect
    {
        [Tooltip("Sv.1'de menzile eklenir (gövde yüzeyinden).")]
        [SerializeField] private float _rangeAtLevel1 = 0.5f;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var weapon = WeaponBase.FindPrimary(blobRoot); // Havai Fişek değil, karakter silahı
            if (weapon == null) return;

            int level = LevelOf(data);
            switch (level)
            {
                case 1:
                    weapon.IncreaseDamage(data.PerLevelValue);
                    weapon.IncreaseRange(_rangeAtLevel1);
                    break;
                case 2: weapon.SetTargetCount(2); break;
                case 3: weapon.SetPierce(1); weapon.IncreaseDamage(data.PerLevelValue); break;
                case 4: weapon.SetTargetCount(3); break;
                default: weapon.EnableUltimate(); break;
            }

            // Görünür silah: form yeni mekanikle değişir.
            int form = level >= 5 ? 4 : level >= 4 ? 3 : level >= 2 ? 2 : 1;
            blobRoot.GetComponent<WeaponVisual>()?.SetForm(form);
        }
    }
}
