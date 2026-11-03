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
        // Yenince kazanılan mass — genelde Data.MassValue, Mermo'nun parçaları için instance bazlı (2026-11-03).
        float MassValue { get; }
        void OnConsumed();
    }
}
