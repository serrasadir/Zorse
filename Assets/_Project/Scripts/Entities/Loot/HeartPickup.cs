using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Systems;

namespace BlobSurvivor.Entities.Loot
{
    // GDD_v2.md §9 "Kalp (can)" toplanabiliri (2026-11-02). Düşman ölünce EnemyData'daki ihtimalle düşer.
    // Sahne objesi gerekmez: havuz PoolManager'da prefab başına tek, ilk düşüşte kurulur.
    // Can doluyken alınmaz, yerde bekler. Belli süre sonra kaybolur; son saniyelerde yanıp söner.
    public class HeartPickup : MonoBehaviour
    {
        [SerializeField] private float _healAmount = 20f;
        [SerializeField] private float _lifetime = 20f;
        [SerializeField] private float _blinkDuration = 3f;
        [SerializeField] private float _height = 0.5f;
        [SerializeField] private float _bobHeight = 0.15f;
        [SerializeField] private float _bobSpeed = 3f;
        [SerializeField] private float _spinSpeed = 90f;
        [SerializeField] private Transform _model;

        private const int InitialPoolSize = 6;
        private const float BlinkInterval = 0.15f;
        private static readonly Vector3 DropOffset = new Vector3(0.6f, 0f, 0f); // coin/sandıkla üst üste binmesin

        private ObjectPool<HeartPickup> _pool;
        private Vector3 _basePosition;
        private float _age;

        public static void Spawn(HeartPickup prefab, Vector3 position)
        {
            if (prefab == null || PoolManager.Instance == null) return;

            ObjectPool<HeartPickup> pool = PoolManager.Instance.CreatePool(prefab, InitialPoolSize);
            HeartPickup heart = pool.Get(position, Quaternion.identity);
            heart._pool = pool;
            heart._basePosition = new Vector3(position.x, heart._height, position.z) + DropOffset;
            heart.transform.position = heart._basePosition;
        }

        private void OnEnable()
        {
            // Coin ile aynı: ConsumableTier1 layer'ını paylaşır, Blob bu layer'la her zaman çarpışır.
            gameObject.layer = LayerMask.NameToLayer("ConsumableTier1");
            _age = 0f;
            if (_model != null) _model.gameObject.SetActive(true);
            GameEvents.OnCharacterSelected += HandleRestart;
        }

        private void OnDisable() => GameEvents.OnCharacterSelected -= HandleRestart;

        private void HandleRestart(CharacterData _) => ReturnToPool();

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _lifetime)
            {
                ReturnToPool();
                return;
            }

            transform.position = _basePosition + Vector3.up * (Mathf.Sin(_age * _bobSpeed) * _bobHeight);
            transform.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.World);

            if (_model != null && _age > _lifetime - _blinkDuration)
                _model.gameObject.SetActive(Mathf.FloorToInt(_age / BlinkInterval) % 2 == 0);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Blob")) return;

            BlobHealth health = other.GetComponent<BlobHealth>();
            if (health == null || health.CurrentHealth >= health.MaxHealth) return;

            health.Heal(_healAmount);
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (!gameObject.activeSelf) return;
            if (_pool != null) _pool.Return(this);
            else gameObject.SetActive(false);
        }
    }
}
