using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // B16 (Yutuş skill'i): her yemede (consumable ya da düşman) BlobConsumption bu miktarda heal uygular.
    public class HealOnConsumeComponent : MonoBehaviour
    {
        public float HealAmount { get; private set; }

        public void IncreaseHealAmount(float amount) => HealAmount += amount;
    }
}
