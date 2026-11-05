using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Enemies;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Blob
{
    public class BlobConsumption : MonoBehaviour
    {
        private BlobGrowth _blobGrowth;
        private BlobHealth _blobHealth;
        private ScoreSystem _scoreSystem;
        private EatEffects _eatEffects;

        private void Awake()
        {
            _blobGrowth = GetComponent<BlobGrowth>();
            _blobHealth = GetComponent<BlobHealth>();
            // 2026-09-06 (Serra): oyunun en sık çalışan yolu (her yeme) `FindAnyObjectByType` çağırıyordu —
            // Awake'te bir kere cache'lendi (CLAUDE.md'nin kendi kuralına aykırıydı, Temmuz 2026'daki bir
            // kod incelemesinde de flag'lenip hiç düzeltilmemişti).
            _scoreSystem = FindAnyObjectByType<ScoreSystem>();
            _eatEffects = GetComponent<EatEffects>();
            if (_eatEffects == null) _eatEffects = gameObject.AddComponent<EatEffects>();
        }

        // Önle yeme: blob'un arkasındaki RearAngle'lık dilim (popo) yemez; arkadan değen obje içinden geçer,
        // yüz ona dönünce (OnTriggerStay) yenir. Vakumla çekilenler ve Kara Delik kural dışı.
        private const float RearAngle = 120f;
        private static readonly float MinFrontDot = Mathf.Cos((180f - RearAngle * 0.5f) * Mathf.Deg2Rad);

        private struct Contact { public IConsumable Consumable; public EnemyBase Enemy; }
        private readonly System.Collections.Generic.Dictionary<Collider, Contact> _contacts =
            new System.Collections.Generic.Dictionary<Collider, Contact>();
        private BlobVisual _visual;
        private VacuumComponent _vacuum;

        private Contact Lookup(Collider other)
        {
            if (!_contacts.TryGetValue(other, out Contact c))
            {
                c = new Contact { Consumable = other.GetComponent<IConsumable>(), Enemy = other.GetComponent<EnemyBase>() };
                _contacts[other] = c;
            }
            return c;
        }

        // Temas noktası blob'un önünde mi? Görünümü olmayan (yüzsüz küre) blob her yönden yer.
        public bool CanEatFrom(Vector3 point)
        {
            if (_visual == null) _visual = GetComponent<BlobVisual>();
            if (_visual == null || !_visual.HasFacing) return true;
            Vector3 dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return true; // obje merkeze girmiş
            return Vector3.Dot(_visual.Forward, dir.normalized) >= MinFrontDot;
        }

        private bool CanEat(Collider other, IConsumable consumable)
        {
            if (CanEatFrom(other.ClosestPoint(transform.position))) return true;
            if (consumable == null || !consumable.IsVacuumable) return false;
            if (_vacuum == null) _vacuum = GetComponent<VacuumComponent>();
            return _vacuum != null && _vacuum.Reach > 0f;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Blob'un iki collider'ı (trigger + solid) aynı adımda iki çağrı üretebilir; ilki nesneyi zaten yedi.
            if (!other.gameObject.activeInHierarchy) return;

            Contact c = Lookup(other);
            if (c.Consumable != null && c.Consumable.RequiredTier > _blobGrowth.CurrentTier)
            {
                if (c.Consumable.Data.IsHazard) _blobHealth.TakeDamage(c.Consumable.Data.HazardAmount);
                return;
            }
            TryEat(other, c);
        }

        // Arkadan değip yenmeyen obje: yüz ona dönünce yenir. Sadece temas süren birkaç obje için çalışır.
        private void OnTriggerStay(Collider other)
        {
            if (!other.gameObject.activeInHierarchy) return;
            Contact c = Lookup(other);
            if (c.Consumable != null && c.Consumable.RequiredTier > _blobGrowth.CurrentTier) return;
            TryEat(other, c);
        }

        private void TryEat(Collider other, Contact c)
        {
            if (c.Consumable != null)
            {
                if (CanEat(other, c.Consumable)) Consume(c.Consumable, other.gameObject);
                return;
            }

            // Karar 5 (yeme birincil): sürü Tier3+, elit Tier5'te temasla yutulabilir (A12 API'si).
            // B16 (Yırtıcı Çene): ConsumptionBonusComponent varsa efektif tier'ı yükseltip eşiği gevşetir.
            EnemyBase enemy = c.Enemy;
            if (enemy == null) return;

            BlobTier effectiveTier = _blobGrowth.CurrentTier;
            ConsumptionBonusComponent bonus = ConsumptionBonusComponent.ActiveTierBonus > 0 ? GetComponent<ConsumptionBonusComponent>() : null;
            // 2026-11-03: Yırtıcı Çene elitleri kapsamaz — Small blob elit yiyordu. Elit yemek Avcı Formu'nun işi.
            if (bonus != null && enemy.Data != null && !enemy.Data.IsElite)
                effectiveTier = (BlobTier)Mathf.Min((int)TierLayers.MaxTier, (int)effectiveTier + bonus.TierBonus);

            if (!enemy.CanBeConsumedBy(effectiveTier)) return;
            if (!enemy.IsBoss && !CanEat(other, null)) return;

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

        // B17 evrimi "Kara Delik" (BlackHoleComponent) temassız otomatik yutma için bu girişi kullanır —
        // normal temas akışıyla (Consume) aynı sonucu üretir, tek fark tetikleyicinin OverlapSphere olması.
        public void ConsumeDirect(IConsumable consumable, GameObject obj) => Consume(consumable, obj);

        // Hareketli araba (CarController kendi havuzuna döner). Yutuş tasarım gereği arabada çalışmaz.
        public void ConsumeVehicle(float mass, int score, BlobTier tier, Bounds bounds, Color crumbA, Color crumbB)
        {
            if (_blobGrowth == null) return;
            _blobGrowth.AddMass(mass);
            _blobGrowth.PunchScale();
            _scoreSystem?.AddScore(score);
            _eatEffects.Play(tier, bounds, crumbA, crumbB);
        }

        private void Consume(IConsumable consumable, GameObject obj)
        {
            if (_blobGrowth == null) return;
            _blobGrowth.AddMass(consumable.MassValue);
            _blobGrowth.PunchScale();
            ApplyHealOnConsume();

            _scoreSystem?.AddScore(consumable.Data.ScoreValue);

            // Efekt obje kapanmadan önce: pasif renderer'ın bounds'u boş gelir. Medium altı sadece titreşim.
            ConsumableData data = consumable.Data;
            Renderer renderer = consumable.RequiredTier >= BlobTier.Medium ? obj.GetComponentInChildren<Renderer>() : null;
            if (renderer != null && data != null)
                _eatEffects.Play(consumable.RequiredTier, renderer.bounds, data.CrumbColorA, data.CrumbColorB);
            else
                Haptics.LightTap(); // 2026-11-02: Handheld.Vibrate() bildirim gibi güçlüydü

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
