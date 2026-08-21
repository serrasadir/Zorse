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
        private int _consumedCount;

        private void Awake()
        {
            _blobGrowth = GetComponent<BlobGrowth>();
            _blobHealth = GetComponent<BlobHealth>();
        }

        private void OnTriggerEnter(Collider other)
        {
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
                if (bonus != null)
                    effectiveTier = (BlobTier)Mathf.Min((int)BlobTier.Giant, (int)effectiveTier + bonus.TierBonus);

                if (enemy.TryConsumeByBlob(effectiveTier, out float massReward))
                {
                    _blobGrowth.AddMass(massReward);
                    _blobGrowth.PunchScale();
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
            _blobGrowth.AddMass(consumable.Data.MassValue);
            _blobGrowth.PunchScale();
            ApplyHealOnConsume();

            ScoreSystem scoreSystem = FindAnyObjectByType<ScoreSystem>();
            scoreSystem?.AddScore(consumable.Data.ScoreValue);

            _consumedCount++;
            GameEvents.RaiseConsumedCountChanged(_consumedCount);

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

#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
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
