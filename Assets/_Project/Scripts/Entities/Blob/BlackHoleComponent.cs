using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Entities.Blob
{
    // B17 evrimi "Kara Delik" (Vakum(max) + Sindirim(max)): blob'un çevresindeki, yiyebildiği her şey
    // temasa gerek kalmadan otomatik yutulur.
    // 2026-11-03: eskiden sabit 1 birim yarıçap + sadece Tiny/Small'du — bu evrime ulaşıldığında blob zaten
    // Large/Giant olduğu için yarıçap gövdenin içinde kalıyor, yenenler de önemsiz mass veriyordu.
    // Artık yarıçap blob'un o anki gövdesiyle ölçeklenir ve tier eşiği blob'un kendi tier'ı.
    public class BlackHoleComponent : MonoBehaviour
    {
        [SerializeField] private float _radiusMultiplier = 2f; // gövde yarıçapının katı
        private const float CheckInterval = 0.1f;
        // ConsumableTier1-5 (9-13) — polis/çevre/zemin taranmasın
        private const int ConsumableMask = (1 << 9) | (1 << 10) | (1 << 11) | (1 << 12) | (1 << 13);

        private BlobGrowth _growth;
        private BlobConsumption _consumption;
        private float _bodyLocalRadius = 0.5f;
        private float _timer;
        private readonly Collider[] _hits = new Collider[32];

        private void Awake()
        {
            _growth = GetComponent<BlobGrowth>();
            _consumption = GetComponent<BlobConsumption>();

            foreach (SphereCollider c in GetComponents<SphereCollider>())
            {
                if (!c.isTrigger)
                {
                    _bodyLocalRadius = c.radius;
                    break;
                }
            }
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = CheckInterval;

            if (_growth == null || _consumption == null) return;

            float radius = transform.localScale.x * _bodyLocalRadius * _radiusMultiplier;
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _hits, ConsumableMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                GameObject target = _hits[i].gameObject;
                if (!target.activeInHierarchy) continue; // bu tarama içinde zaten yenmiş olabilir

                IConsumable consumable = _hits[i].GetComponent<IConsumable>();
                if (consumable == null || consumable.Data == null) continue;
                if (consumable.Data.IsHazard) continue;
                if (consumable.RequiredTier > _growth.CurrentTier) continue;

                _consumption.ConsumeDirect(consumable, target);
            }
        }
    }
}
