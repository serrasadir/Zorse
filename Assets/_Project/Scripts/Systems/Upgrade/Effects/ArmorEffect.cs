using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // 2026-11-04 (Serra): Zırh yeniden tasarlandı — eskiden gelen hasarı seviye başına %7 azaltıyordu (çalışıyordu
    // ama hiç hissedilmiyordu). Artık kalkan havuzu: hasarı önce kalkan yer, son hasardan birkaç sn sonra dolar.
    // Görsel baloncuk + dikenler ArmorShield'da, havuz BlobHealth'te. (Eski adı DamageReductionEffect; guid aynı,
    // FX_DamageReduction.asset bu script'i kullanmaya devam ediyor.)
    //   Kalkan: 10 + PerLevelValue(10) × seviye → Sv.1 20 ... Sv.7 80, Sv.8 100
    //   Sv.3 dikenler (sn'de 6) · Sv.5 yenilenme 4 → 3 sn · Sv.6 dikenler sn'de 10 · Sv.8 kalkan 100 + kırılınca şok dalgası
    // Kart metinleri Upgrade_Armor.asset'in Level Descriptions listesinde (bu değerlerle uyumlu tutulmalı).
    [CreateAssetMenu(fileName = "ArmorEffect", menuName = "BlobSurvivor/Effects/Armor (Shield)")]
    public class ArmorEffect : UpgradeEffect
    {
        [SerializeField] private float _baseShield = 10f;
        [SerializeField] private float _maxLevelShield = 100f;
        [SerializeField] private float _rechargeDelay = 4f;
        [SerializeField] private float _fastRechargeDelay = 3f;
        [SerializeField] private int _fastRechargeLevel = 5;
        [SerializeField] private int _thornLevel = 3;
        [SerializeField] private float _thornDps = 6f;
        [SerializeField] private int _strongThornLevel = 6;
        [SerializeField] private float _strongThornDps = 10f;
        [SerializeField] private int _shockwaveLevel = 8;
        [SerializeField] private float _shockwaveDamage = 25f;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            BlobHealth health = blobRoot.GetComponent<BlobHealth>();
            if (health == null) return;

            // UpgradeSystem seviyeyi Apply'dan önce artırıyor — bu, az önce ulaşılan seviye.
            int level = UpgradeSystem.Instance != null ? UpgradeSystem.Instance.GetLevel(data) : 1;

            float shield = level >= _shockwaveLevel ? _maxLevelShield : _baseShield + data.PerLevelValue * level;
            float delay = level >= _fastRechargeLevel ? _fastRechargeDelay : _rechargeDelay;
            float thorns = level >= _strongThornLevel ? _strongThornDps : level >= _thornLevel ? _thornDps : 0f;
            float shockwave = level >= _shockwaveLevel ? _shockwaveDamage : 0f;

            ArmorShield armor = blobRoot.GetComponent<ArmorShield>();
            if (armor == null) armor = blobRoot.AddComponent<ArmorShield>();
            armor.Configure(thorns, shockwave);
            health.SetShield(shield, delay);
        }
    }
}
