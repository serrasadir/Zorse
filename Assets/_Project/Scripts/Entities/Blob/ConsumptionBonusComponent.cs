using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // B16 (Yırtıcı Çene skill'i): düşman yutma tier eşiğini gevşetir — BlobConsumption bu bonus'u
    // enemy.TryConsumeByBlob()'a geçirilen efektif tier'a ekler (asıl consumable yeme tier'ını etkilemez).
    public class ConsumptionBonusComponent : MonoBehaviour
    {
        public int TierBonus { get; private set; }

        public void IncreaseTierBonus(int amount) => TierBonus += amount;
    }
}
