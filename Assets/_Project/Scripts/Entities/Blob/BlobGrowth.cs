using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Entities.Blob
{
    [System.Serializable]
    public struct TierThreshold
    {
        public BlobTier Tier;
        public float MassRequired;
    }

    public class BlobGrowth : MonoBehaviour
    {
        [Header("Tier Eşikleri (Oyun Mantığı)")]
        [SerializeField] private TierThreshold[] _tierThresholds = new TierThreshold[]
        {
            new TierThreshold { Tier = BlobTier.Tiny,   MassRequired = 0f   },
            new TierThreshold { Tier = BlobTier.Small,  MassRequired = 10f  },
            new TierThreshold { Tier = BlobTier.Medium, MassRequired = 30f  },
            new TierThreshold { Tier = BlobTier.Large,  MassRequired = 60f  },
            new TierThreshold { Tier = BlobTier.Giant,  MassRequired = 100f },
        };

        [Header("Görsel Büyüme")]
        [SerializeField] private float _baseScale = 0.5f;
        [SerializeField] private float _growthFactor = 0.5f;
        [SerializeField] private float _growthExponent = 0.4f;
        [SerializeField] private float _scaleSmoothing = 5f;

        [Header("XP & Level")]
        [SerializeField] private float _baseXPThreshold = 20f;
        [SerializeField] private float _xpGrowthPerLevel = 15f;

        public float CurrentMass { get; private set; }
        public BlobTier CurrentTier { get; private set; } = BlobTier.Tiny;
        public float CurrentXP { get; private set; }
        public float XPThreshold { get; private set; }
        public int CurrentLevel { get; private set; }

        // B16 (Sindirim skill'i) + M22 (meta "Mass Kazanımı" bonusu): yenen her şeyden kazanılan
        // mass'ı çarpar. Mass=XP birleşik olduğu için (Karar 7) AddXP de bu çarpılmış miktarı alır.
        private float _massGainMultiplier = 1f;

        // M22: meta "XP Çarpanı" bonusu — mass/boyutu etkilemeden sadece leveling hızını artırır
        // (Mass Kazanımı'ndan bilinçli olarak ayrı, GDD'de de iki ayrı Market kalemi).
        private float _xpBonusMultiplier = 1f;

        private void Start()
        {
            transform.localScale = Vector3.one * _baseScale;
            XPThreshold = _baseXPThreshold;
        }

        private void Update()
        {
            float targetScale = CalculateScale();
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                Vector3.one * targetScale,
                Time.deltaTime * _scaleSmoothing
            );
        }

        public void AddMass(float amount)
        {
            float adjusted = amount * _massGainMultiplier;
            CurrentMass += adjusted;
            RecalculateTier();
            GameEvents.RaiseBlobSizeChanged(CurrentMass);
            AddXP(adjusted);
        }

        public void IncreaseMassGainMultiplier(float amount) => _massGainMultiplier += amount;

        // M22: GameManager.ApplyMetaProgression her run başında (fresh start + restart) çağırır —
        // overwrite (increment değil), böylece bir önceki run'ın Sindirim skill birikimi de temizlenmiş olur.
        public void ApplyMetaBonuses(float massGainBonus, float xpBonus)
        {
            _massGainMultiplier = 1f + massGainBonus;
            _xpBonusMultiplier = 1f + xpBonus;
        }

        private void AddXP(float amount)
        {
            CurrentXP += amount * _xpBonusMultiplier;
            GameEvents.RaiseXPChanged((int)CurrentXP);

            while (CurrentXP >= XPThreshold)
            {
                CurrentXP -= XPThreshold;
                CurrentLevel++;
                XPThreshold = _baseXPThreshold + CurrentLevel * _xpGrowthPerLevel;
                GameEvents.RaiseLevelUp(CurrentLevel);
            }
        }

        // 2026-08-22 bug fix: restart aynı BlobGrowth instance'ını yeniden kullanıyor (sahne reload yok),
        // bu metod olmadan CurrentMass/CurrentTier bir önceki run'ın bitişindeki değerde kalıyordu —
        // GameManager.ClearPreviousRunState() bunu çağırır. _massGainMultiplier (Sindirim skill'i) kasıtlı
        // olarak dokunulmuyor — o ayrı, hâlâ açık bir restart-cleanup eksiği (bkz. CLAUDE.md).
        public void ResetGrowth()
        {
            CurrentMass = 0f;
            CurrentXP = 0f;
            CurrentLevel = 0;
            XPThreshold = _baseXPThreshold;
            transform.localScale = Vector3.one * _baseScale;
            _massGainMultiplier = 1f;
            _xpBonusMultiplier = 1f;

            BlobTier previousTier = CurrentTier;
            CurrentTier = BlobTier.Tiny;

            GameEvents.RaiseBlobSizeChanged(CurrentMass);
            GameEvents.RaiseXPChanged((int)CurrentXP);
            if (previousTier != CurrentTier)
                GameEvents.RaiseBlobTierChanged(CurrentTier);
        }

        public void RemoveMass(float amount)
        {
            CurrentMass = Mathf.Max(0f, CurrentMass - amount);
            RecalculateTier();
            GameEvents.RaiseBlobSizeChanged(CurrentMass);
        }

        public void PunchScale(float punchAmount = 0.15f)
        {
            transform.localScale = Vector3.one * (CalculateScale() * (1f + punchAmount));
        }

        private float CalculateScale()
        {
            return _baseScale * Mathf.Pow(1f + CurrentMass * _growthFactor, _growthExponent);
        }

        private void RecalculateTier()
        {
            BlobTier newTier = BlobTier.Tiny;
            for (int i = _tierThresholds.Length - 1; i >= 0; i--)
            {
                if (CurrentMass >= _tierThresholds[i].MassRequired)
                {
                    newTier = _tierThresholds[i].Tier;
                    break;
                }
            }

            if (newTier != CurrentTier)
            {
                CurrentTier = newTier;
                GameEvents.RaiseBlobTierChanged(CurrentTier);
            }
        }
    }
}
