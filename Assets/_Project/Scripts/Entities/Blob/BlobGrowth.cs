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

        public float CurrentMass { get; private set; }
        public BlobTier CurrentTier { get; private set; } = BlobTier.Tiny;

        private void Start()
        {
            transform.localScale = Vector3.one * _baseScale;
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
            CurrentMass += amount;
            RecalculateTier();
            GameEvents.RaiseBlobSizeChanged(CurrentMass);
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
