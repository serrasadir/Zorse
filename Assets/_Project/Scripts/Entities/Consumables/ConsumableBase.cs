using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Entities
{
    public class ConsumableBase : MonoBehaviour, IConsumable
    {
        [SerializeField] private ConsumableData _data;
        private Collider _collider;

        public ConsumableData Data => _data;
        public BlobTier RequiredTier => _data != null ? _data.RequiredTier : BlobTier.Tiny;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        // M25: SetData spawner tarafından OnEnable'dan SONRA çağrılıyor (bkz. OnEnable'daki not) —
        // o yüzden layer/trigger durumunu burada da güncelliyoruz, sadece OnEnable'a güvenmiyoruz.
        public void SetData(ConsumableData data)
        {
            _data = data;
            AssignLayer();
            UpdateTriggerState(GameEvents.CurrentBlobTier);
        }

        public virtual void OnConsumed() { }

        // M24: alt sınıflar (ör. PedestrianController) ConsumableSpawner'dan farklı bir pool sahibine
        // dönebilir — BlobConsumption bunu spawner tipine bakmadan çağırır.
        public virtual void ReturnToOwner() => ConsumableSpawner.Instance?.ReturnToPool(this);

        // M24: PedestrianController kendi OnEnable'ında wander state'i sıfırlamak için bunu override
        // edip base.OnEnable()'ı çağırıyor — virtual olmasa Unity'nin iki ayrı OnEnable'ı da çağırıp
        // çağırmayacağı garanti değil, bu yüzden standart C# override kullanıldı.
        protected virtual void OnEnable()
        {
            AssignLayer();
            UpdateTriggerState(GameEvents.CurrentBlobTier);
            GameEvents.OnBlobTierChanged += UpdateTriggerState;
        }

        protected virtual void OnDisable()
        {
            GameEvents.OnBlobTierChanged -= UpdateTriggerState;
        }

        private void AssignLayer()
        {
            if (_data == null) return;
            int layerIndex = 8 + (int)_data.RequiredTier;
            gameObject.layer = layerIndex;
        }

        // M25: Blob'un boyu yetmediği consumable'lara "çarpması" için — trigger yerine solid collider'a
        // geçiyoruz, Blob'daki ikinci (solid) Sphere Collider bunu fiziksel olarak engelliyor. Blob
        // tier'ı yükselip yutulabilir hale gelince tekrar trigger'a dönüyor. Sadece OnBlobTierChanged
        // (koşu başına birkaç kez) ateşlendiğinde çalışıyor — per-frame maliyeti yok.
        private void UpdateTriggerState(BlobTier blobTier)
        {
            if (_collider == null) return;

            // Hazard'lar (ör. dikenli obje) tier'a bakılmaksızın hep trigger kalır — BlobConsumption'daki
            // "yeterince büyük değilsen hasar al" dalı (else if IsHazard) trigger'a bağlı, solid yapılırsa
            // Blob sadece fiziksel olarak çarpar, hasar hiç tetiklenmez. Bu M25'in kapsamı dışında.
            bool isHazard = _data != null && _data.IsHazard;
            _collider.isTrigger = isHazard || RequiredTier <= blobTier;
        }
    }
}
