using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Entities
{
    public interface IConsumable
    {
        ConsumableData Data { get; }
        BlobTier RequiredTier { get; }
        // 2026-09-06 (Serra): "yaya ve polisleri vakum çekmesin" — Vakum sadece bunu true dönenleri
        // çeker. Sıradan consumable'larda true, PedestrianController'da false (bkz. override).
        bool IsVacuumable { get; }
        void OnConsumed();
    }
}
