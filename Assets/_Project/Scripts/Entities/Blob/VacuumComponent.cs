using System.Collections.Generic;
using UnityEngine;
using BlobSurvivor.Entities;

namespace BlobSurvivor.Entities.Blob
{
    public class VacuumComponent : MonoBehaviour
    {
        [SerializeField] private float _pullSpeed = 6f;
        [SerializeField] private float _baseRadius = 3f;

        // 2026-09-06 (Serra): Temmuz 2026'da bir kod incelemesinde flag'lenmiş ama hiç düzeltilmemiş bir
        // eksikti — sorgu LayerMask'siz her frame çalışıyordu (CLAUDE.md'nin kendi throttle/LayerMask
        // kuralına aykırı, Enemy/Ground/Environment collider'ları da boşuna taranıyordu). Aynı dosyada
        // "yaya/polis çekilmesin" düzeltmesi yapılırken ikisi birlikte ele alındı: LayerMask artık sadece
        // ConsumableTier1-5'i tarıyor (Enemy zaten IConsumable değil, ekstra güvenlik), throttle standart
        // 0.15s tick'e bağlandı.
        private const float UpdateInterval = 0.15f;
        private static int s_consumableMask = -1;
        private float _updateTimer;

        public float Radius { get; private set; }

        private void Reset() => Radius = _baseRadius;
        private void OnEnable()
        {
            if (Radius <= 0f) Radius = _baseRadius;
            _updateTimer = Random.Range(0f, UpdateInterval);
        }

        private BlobGrowth _growth;
        private readonly Collider[] _hits = new Collider[64];
        // Ağır sorgu (OverlapSphere) sadece throttle tick'inde çalışır, sonucu burada cache'lenir —
        // hareketin kendisi (MoveTowards) her frame bu cache üzerinden uygulanır (EnemyBase'in
        // separation/avoidance'ı cache'leyip her frame uygulama deseniyle aynı, aksi halde nesneler
        // 0.15s'de bir "sıçrayarak" hareket eder, akıcı çekilmez).
        private readonly List<Transform> _pulledTargets = new List<Transform>();

        private void Awake()
        {
            _growth = GetComponent<BlobGrowth>();

            if (s_consumableMask < 0)
            {
                s_consumableMask = LayerMask.GetMask(
                    "ConsumableTier1", "ConsumableTier2", "ConsumableTier3", "ConsumableTier4", "ConsumableTier5");
            }
        }

        private void Update()
        {
            if (Radius <= 0f) return;

            _updateTimer -= Time.deltaTime;
            if (_updateTimer <= 0f)
            {
                _updateTimer = UpdateInterval;
                RefreshPulledTargets();
            }

            float step = _pullSpeed * Time.deltaTime;
            for (int i = 0; i < _pulledTargets.Count; i++)
            {
                Transform target = _pulledTargets[i];
                if (target == null) continue;
                target.position = Vector3.MoveTowards(target.position, transform.position, step);
            }
        }

        private void RefreshPulledTargets()
        {
            _pulledTargets.Clear();

            int count = Physics.OverlapSphereNonAlloc(transform.position, Radius, _hits, s_consumableMask);
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].gameObject == gameObject) continue;

                var consumable = _hits[i].GetComponent<IConsumable>();
                if (consumable == null || !consumable.IsVacuumable) continue;
                if (_growth != null && consumable.RequiredTier > _growth.CurrentTier) continue;

                _pulledTargets.Add(_hits[i].transform);
            }
        }

        public void IncreaseRadius(float amount) => Radius += amount;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.2f);
            Gizmos.DrawSphere(transform.position, Radius);
        }
    }
}
