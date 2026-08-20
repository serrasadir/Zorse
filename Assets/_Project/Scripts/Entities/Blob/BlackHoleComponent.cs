using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Entities.Blob
{
    // B17 evrimi "Kara Delik" (Vakum(max) + Sindirim(max)): vakumla çekilen tier1/2 consumable'lar
    // artık temasa gerek kalmadan, bu iç yarıçapa girince otomatik yutulur.
    public class BlackHoleComponent : MonoBehaviour
    {
        [SerializeField] private float _autoConsumeRadius = 1f;
        private const float CheckInterval = 0.1f;

        private BlobGrowth _growth;
        private BlobConsumption _consumption;
        private float _timer;
        private readonly Collider[] _hits = new Collider[32];

        private void Awake()
        {
            _growth = GetComponent<BlobGrowth>();
            _consumption = GetComponent<BlobConsumption>();
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = CheckInterval;

            int count = Physics.OverlapSphereNonAlloc(transform.position, _autoConsumeRadius, _hits);
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].gameObject == gameObject) continue;

                IConsumable consumable = _hits[i].GetComponent<IConsumable>();
                if (consumable == null) continue;
                if (consumable.RequiredTier > BlobTier.Small) continue; // sadece tier1/2 (Tiny/Small)
                if (_growth != null && consumable.RequiredTier > _growth.CurrentTier) continue;

                _consumption?.ConsumeDirect(consumable, _hits[i].gameObject);
            }
        }
    }
}
