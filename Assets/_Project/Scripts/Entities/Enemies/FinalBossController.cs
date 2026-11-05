using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Entities.Enemies
{
    // Final boss = polis helikopteri (2026-11-04, Serra). "Vur, alçalt, büyü, yut":
    // - Helikopter havada uçar; kökü (NavMeshAgent) yerde blob'u kovalar, gövde (collider + model) yukarıdadır.
    // - Vurdukça kademe kademe alçalır (_stages: can oranı → irtifa). Can artmadığı için geri yükselmez.
    // - Silahla ölmez (EnemyData.RequiresConsumptionToDie, HP 1'de kilitlenir).
    // - Yutulmanın tek koşulu UZANMAK: blob'un tepesi helikoptere değdiği an, hangi kademede olursa olsun
    //   yutulur (Giant tier şartı EnemyData'da, uzanabilen blob zaten Giant). Çok büyüyen oyuncu erken bitirir.
    // Yutulunca BossDeathEffect yutulma sahnesini oynatır, bitince GameEvents.RaiseFinalBossConsumed →
    // GameManager run'ı "tamamlandı" olarak kapatır.
    [RequireComponent(typeof(EnemyBase))]
    public class FinalBossController : MonoBehaviour
    {
        [System.Serializable]
        public struct AltitudeStage
        {
            [Range(0f, 1f)] public float HealthFraction; // can bu oranın altına inince...
            public float Altitude;                        // ...gövdenin alt ucu (kızaklar) yerden bu kadar yükseğe iner
        }

        // Blob tepesi ≈ 1.1 × ölçek (gövde yerde, tetikleyici yarıçapı 0.6). Mevcut büyüme formülüyle:
        // 5 birim ≈ 1340 mass (Giant'ın biraz üstü), 6 ≈ 1950, 7 ≈ 2700, 8 ≈ 3600.
        [SerializeField] private AltitudeStage[] _stages =
        {
            new AltitudeStage { HealthFraction = 1f,    Altitude = 8f },
            new AltitudeStage { HealthFraction = 0.75f, Altitude = 7f },
            new AltitudeStage { HealthFraction = 0.5f,  Altitude = 6f },
            new AltitudeStage { HealthFraction = 0.25f, Altitude = 5f },
        };
        [SerializeField] private float _descendSpeed = 1.5f; // birim/sn — kademe geçişi yumuşak

        // Kamera çerçevelemesi için: sahnede aynı anda tek final boss var.
        public static FinalBossController Active { get; private set; }

        private EnemyBase _enemyBase;
        private CapsuleCollider _body;
        private Vector3 _bodyBaseCenter; // prefab'daki (irtifa 0) gövde merkezi
        private float _altitude;
        private int _stageIndex;

        // Kızakların yerden yüksekliği (HelicopterVisual modeli buna göre konumlar).
        public float CurrentAltitude => _altitude;
        // Son kademeye inildi — HelicopterVisual rotoru yavaşlatıp "hasarlı" gösterir.
        public bool IsEdible => _stageIndex >= _stages.Length - 1;
        // Gövde merkezi (dünya) — kamera çerçevelemesi.
        public Vector3 BodyCenter => _enemyBase.AimPoint;

        private void Awake()
        {
            _enemyBase = GetComponent<EnemyBase>();
            _body = GetComponent<CapsuleCollider>();
            if (_body != null) _bodyBaseCenter = _body.center;
        }

        private void OnEnable()
        {
            _stageIndex = 0;
            _altitude = _stages.Length > 0 ? _stages[0].Altitude : 0f;
            _enemyBase.OnDeath += HandleDeath;
            Active = this;
            ApplyAltitude();
        }

        private void OnDisable()
        {
            _enemyBase.OnDeath -= HandleDeath;
            if (Active == this) Active = null;
        }

        private void Update()
        {
            // Uzanabilen blob her an yutabilir; EnemyBase.OnEnable bunu sıfırladığı için her kare tazelenir.
            _enemyBase.SetConsumableOverride(true);

            if (_enemyBase.MaxHealth > 0f)
            {
                float fraction = _enemyBase.CurrentHealth / _enemyBase.MaxHealth;
                while (_stageIndex + 1 < _stages.Length && fraction <= _stages[_stageIndex + 1].HealthFraction)
                {
                    _stageIndex++;
#if UNITY_EDITOR
                    Debug.Log($"[FinalBossController] Alçalıyor: kademe {_stageIndex}, irtifa {_stages[_stageIndex].Altitude}.");
#endif
                }
            }

            float target = _stages.Length > 0 ? _stages[_stageIndex].Altitude : 0f;
            if (!Mathf.Approximately(_altitude, target))
                _altitude = Mathf.MoveTowards(_altitude, target, _descendSpeed * Time.deltaTime);
            ApplyAltitude();
        }

        private void ApplyAltitude()
        {
            if (_body != null)
            {
                Vector3 center = _bodyBaseCenter + Vector3.up * _altitude;
                if (_body.center != center) _body.center = center;
                _enemyBase.AirborneHeight = Mathf.Max(0.01f, center.y);
            }
            else
            {
                _enemyBase.AirborneHeight = Mathf.Max(0.01f, _altitude);
            }
        }

        private void HandleDeath(EnemyBase boss)
        {
#if UNITY_EDITOR
            Debug.Log("[FinalBossController] Final boss yutuldu — yutulma sahnesi (BossDeathEffect), sonra OnFinalBossConsumed.");
#endif
            // Run'ı bitiren olay sahne bitince BossSpawner → BossDeathEffect.PlaySwallow'dan ateşlenir.
        }
    }
}
