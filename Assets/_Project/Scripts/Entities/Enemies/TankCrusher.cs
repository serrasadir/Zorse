using UnityEngine;
using BlobSurvivor.Entities.Vehicles;

namespace BlobSurvivor.Entities.Enemies
{
    // 2026-11-04 (Serra: "miniboss arabaların, ağaçların içinden geçiyor"): tank NavMeshAgent ile yürür, NavMesh'te
    // sadece binalar yasak — sokak eşyaları ve hareketli arabalar yok, ajan fizik collider'larına bakmaz.
    // Etrafından dolanmak yerine tank ezer: üstünden geçtiği arabalar havuza döner, küçük şehir eşyaları
    // (ağaç, lamba, durak...) gizlenir (CityLevel run sonunda geri açar). Binalar ezilmez, tank onları NavMesh ile dolanır.
    // EnemyData.CrushesObstacles açık düşmana EnemyBase.SetData ekler (prefab'a elle bağlamaya gerek yok).
    [RequireComponent(typeof(EnemyBase))]
    public class TankCrusher : MonoBehaviour
    {
        private const float CheckInterval = 0.1f;
        // Tank gövdesinden biraz küçük: kenara sürtünen eşya ezilmesin, gerçekten altına giren ezilsin.
        private const float RadiusFactor = 0.85f;
        // ConsumableTier1-5 (9-13, şehir eşyaları) + Hazard (15, arabalar)
        private const int CrushMask = (1 << 9) | (1 << 10) | (1 << 11) | (1 << 12) | (1 << 13) | (1 << 15);

        private readonly Collider[] _hits = new Collider[16];
        private CapsuleCollider _body;
        private float _groundOffset;
        private float _timer;

        private void Awake()
        {
            _body = GetComponent<CapsuleCollider>();
            UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            _groundOffset = agent != null ? agent.baseOffset : 1f;
            _timer = Random.Range(0f, CheckInterval);
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = CheckInterval;

            float radius = (_body != null ? _body.radius * transform.lossyScale.x : 1f) * RadiusFactor;
            Vector3 ground = transform.position - Vector3.up * _groundOffset;
            int count = Physics.OverlapCapsuleNonAlloc(ground + Vector3.up * 0.3f, ground + Vector3.up * 2f,
                radius, _hits, CrushMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                if (hit == null || !hit.gameObject.activeInHierarchy) continue;

                CarController car = hit.GetComponentInParent<CarController>();
                if (car != null)
                {
                    car.Crush();
                    continue;
                }

                // Engel kutusu olan (bina) ezilmez; yaya ve havuz consumable'ları CityConsumable değil, dokunulmaz.
                CityConsumable prop = hit.GetComponentInParent<CityConsumable>();
                if (prop != null && !prop.BlocksProjectiles)
                    prop.gameObject.SetActive(false);
            }
        }
    }
}
