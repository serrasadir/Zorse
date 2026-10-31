using UnityEngine;

namespace BlobSurvivor.Entities
{
    // Şehre elle yerleştirilmiş, havuza ait olmayan yenilebilir obje (bina, araç, sokak eşyası).
    // ConsumableBase kök objenin layer'ını tier layer'ına çektiği için binalar Environment'tan çıkar;
    // polis/yaya engel sorguları Environment'a baktığından _obstacle ayrı bir child collider'dır.
    public class CityConsumable : ConsumableBase
    {
        [SerializeField] private Collider _obstacle;

        private static Collider[] s_blobColliders;

        public override bool IsVacuumable => false;

        // Şehir objeleri sadece yutulur: mermi yoksa tier'a bakmadan binayı silerdi.
        public override bool CanBeSplit => false;

        public override void ReturnToOwner() => gameObject.SetActive(false);

        private void Start() => IgnoreBlob();

        // Engel yalnızca polis/yaya sorguları için; blob'u kök collider durdurur veya yutar.
        // Unity bu ayarı collider kapanınca sıfırlar, CityLevel geri açarken tekrar çağırır.
        public void IgnoreBlob()
        {
            if (_obstacle == null) return;

            if (s_blobColliders == null || s_blobColliders.Length == 0 || s_blobColliders[0] == null)
            {
                GameObject blob = GameObject.FindWithTag("Blob");
                if (blob == null) return;
                s_blobColliders = blob.GetComponentsInChildren<Collider>(true);
            }

            foreach (Collider blobCollider in s_blobColliders)
                Physics.IgnoreCollision(_obstacle, blobCollider, true);
        }
    }
}
