using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Systems
{
    // Havai Fişek skill'i. Sv.1 silahı kurar; sonra kilometre taşları (kart metinleri Upgrade_FireworkWeapon.asset'te):
    //   Sv.2/4 +hasar (PerLevelValue) · Sv.3 atış hızı · Sv.5 boss/elit ek hasar · Sv.6 son form: çift atış
    [CreateAssetMenu(fileName = "FireworkEffect", menuName = "BlobSurvivor/Effects/Heavy Weapon")]
    public class FireworkEffect : UpgradeEffect
    {
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private float _fireRate = 0.5f;
        [SerializeField] private float _damage = 25f;
        [SerializeField] private float _range = 12f;
        [SerializeField, Range(30f, 360f)] private float _fireArc = 120f;
        [Tooltip("Yön bu açıdan fazla değişince ateş edebilmek için beklenen kurma süresi (sn).")]
        [SerializeField] private float _armTime = 1f;
        [SerializeField] private float _armAngle = 30f;

        [Header("Kilometre taşları")]
        [SerializeField] private int _fireRateLevel = 3;
        [SerializeField] private float _fireRateFactor = 1.3f;
        [SerializeField] private int _priorityDamageLevel = 5;
        [SerializeField] private float _priorityDamageScale = 1.5f;
        [SerializeField] private int _burstLevel = 6;
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
                weapon.Setup(_projectilePrefab, _fireRate, _damage, _range, _fireArc);
                weapon.SetArming(_armTime, _armAngle);
            }

            if (level == _fireRateLevel) weapon.MultiplyFireRate(_fireRateFactor);
            else if (level == _priorityDamageLevel) weapon.SetPriorityDamageScale(_priorityDamageScale);
            else if (level == _burstLevel) weapon.SetBurst(_burstCount);
            else if (level > 1) weapon.IncreaseDamage(data.PerLevelValue);

            // Görünür atar formu yeni mekanikle değişir: tek tüp → kalın tüp → koni nişangah → çift tüp.
            weapon.SetForm(level >= _burstLevel ? 4 : level >= _priorityDamageLevel ? 3 : level >= _fireRateLevel ? 2 : 1);
        }
    }
}
