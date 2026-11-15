using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Systems
{
    // Havai Fişek (aktif). Sv.1 silahı kurar (2 sn de 1 roket, 25 hasar); sonra (kart metinleri Upgrade_Firework.asset):
    //   Sv.2 +hasar (PerLevelValue) · Sv.3 atış hızı ×1.3 · Sv.4 boss/elit ×1.5 + hasar · Sv.5 son form: çift atış
    [CreateAssetMenu(fileName = "FireworkEffect", menuName = "BlobSurvivor/Effects/Heavy Weapon")]
    public class FireworkEffect : UpgradeEffect
    {
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _fireRate = 0.5f;
        [SerializeField] private float _damage = 25f;
        [SerializeField] private float _range = 8f;

        [Header("Kilometre taşları")]
        [SerializeField] private int _fireRateLevel = 3;
        [SerializeField] private float _fireRateFactor = 1.3f;
        [SerializeField] private int _priorityDamageLevel = 4;
        [SerializeField] private float _priorityDamageScale = 1.5f;
        [SerializeField] private int _burstLevel = 5;
        [SerializeField] private int _burstCount = 2;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            int level = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.GetLevel(data) : 1;
            FireworkWeapon weapon = blobRoot.GetComponentInChildren<FireworkWeapon>();

            if (weapon == null)
            {
                // Run başına tek seferlik kurulum; restart'ta GameManager.ClearPreviousRunState siler.
                var go = new GameObject("FireworkWeapon");
                go.transform.SetParent(blobRoot.transform, false); // Awake parent'tan BlobVisual'ı bulur
                weapon = go.AddComponent<FireworkWeapon>();
                weapon.Setup(_projectilePrefab, _fireRate, _damage, _range);
            }

            if (level == _fireRateLevel) weapon.MultiplyFireRate(_fireRateFactor);
            else if (level == _priorityDamageLevel) { weapon.SetPriorityDamageScale(_priorityDamageScale); weapon.IncreaseDamage(data.PerLevelValue); }
            else if (level == _burstLevel) weapon.SetBurst(_burstCount);
            else if (level > 1) weapon.IncreaseDamage(data.PerLevelValue);

            // Görünür atar formu yeni mekanikle değişir: tek tüp → kalın tüp → koni nişangah → çift tüp.
            weapon.SetForm(level >= _burstLevel ? 4 : level >= _priorityDamageLevel ? 3 : level >= _fireRateLevel ? 2 : 1);
        }
    }
}
