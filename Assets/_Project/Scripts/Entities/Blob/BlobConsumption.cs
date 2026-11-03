using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Blob
{
    public class BlobConsumption : MonoBehaviour
    {
        private BlobGrowth _blobGrowth;
        private BlobHealth _blobHealth;
        private ScoreSystem _scoreSystem;

        private void Awake()
        {
            _blobGrowth = GetComponent<BlobGrowth>();
            _blobHealth = GetComponent<BlobHealth>();
            // 2026-09-06 (Serra): oyunun en sık çalışan yolu (her yeme) `FindAnyObjectByType` çağırıyordu —
            // Awake'te bir kere cache'lendi (CLAUDE.md'nin kendi kuralına aykırıydı, Temmuz 2026'daki bir
            // kod incelemesinde de flag'lenip hiç düzeltilmemişti).
            _scoreSystem = FindAnyObjectByType<ScoreSystem>();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Blob'un iki collider'ı (trigger + solid) aynı adımda iki çağrı üretebilir; ilki nesneyi zaten yedi.
            if (!other.gameObject.activeInHierarchy) return;

            IConsumable consumable = other.GetComponent<IConsumable>();
            if (consumable != null)
            {
                if (consumable.RequiredTier <= _blobGrowth.CurrentTier)
                    Consume(consumable, other.gameObject);
                else if (consumable.Data.IsHazard)
                    _blobHealth.TakeDamage(consumable.Data.HazardAmount);
                return;
            }

            // Karar 5 (yeme birincil): sürü Tier3+, elit Tier5'te temasla yutulabilir (A12 API'si).
            // B16 (Yırtıcı Çene): ConsumptionBonusComponent varsa efektif tier'ı yükseltip eşiği gevşetir.
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                BlobTier effectiveTier = _blobGrowth.CurrentTier;
                ConsumptionBonusComponent bonus = GetComponent<ConsumptionBonusComponent>();
                // 2026-11-03: Yırtıcı Çene elitleri kapsamaz — Small blob elit yiyordu. Elit yemek Avcı Formu'nun işi.
                if (bonus != null && enemy.Data != null && !enemy.Data.IsElite)
                    effectiveTier = (BlobTier)Mathf.Min((int)BlobTier.Giant, (int)effectiveTier + bonus.TierBonus);

                // TryConsumeByBlob düşmanı havuza döndürür; hasar ondan önce okunur.
                float biteDamage = enemy.GetConsumeDamage(effectiveTier);
                if (enemy.TryConsumeByBlob(effectiveTier, out float massReward))
                {
                    _blobGrowth.AddMass(massReward);
                    _blobGrowth.PunchScale();
                    if (biteDamage > 0f) _blobHealth.TakeDamage(biteDamage);
                    ApplyHealOnConsume();
                }
            }
        }

        // B17 evrimi "Kara Delik" (BlackHoleComponent) temassız otomatik yutma için bu girişi kullanır —
        // normal temas akışıyla (Consume) aynı sonucu üretir, tek fark tetikleyicinin OverlapSphere olması.
        public void ConsumeDirect(IConsumable consumable, GameObject obj) => Consume(consumable, obj);

        private void Consume(IConsumable consumable, GameObject obj)
        {
            if (_blobGrowth == null) return;
            _blobGrowth.AddMass(consumable.MassValue);
            _blobGrowth.PunchScale();
            ApplyHealOnConsume();

            _scoreSystem?.AddScore(consumable.Data.ScoreValue);

            consumable.OnConsumed();

            // B14: pool'a gerçek dönüş — önceden sadece SetActive(false) yapılıyordu, pool Queue'su
            // hiç dolmuyordu (ConsumeAndSplit'teki doğru pattern'in normal yeme akışına taşınması).
            // M24: ReturnToOwner() virtual olduğu için burada spawner tipine bakmaya gerek yok —
            // PedestrianController kendi PedestrianSpawner'ına döner.
            ConsumableBase pooled = obj.GetComponent<ConsumableBase>();
            if (pooled != null)
                pooled.ReturnToOwner();
            else
                obj.SetActive(false);

            Haptics.LightTap(); // 2026-11-02: Handheld.Vibrate() bildirim gibi güçlüydü
        }

        // B16 (Yutuş skill'i): HealOnConsumeComponent varsa her yemede küçük bir heal uygular.
        private void ApplyHealOnConsume()
        {
            HealOnConsumeComponent healOnConsume = GetComponent<HealOnConsumeComponent>();
            if (healOnConsume != null && healOnConsume.HealAmount > 0f)
                _blobHealth?.Heal(healOnConsume.HealAmount);
        }
    }
}
