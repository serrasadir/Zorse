using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.Entities
{
    public interface IConsumable
    {
        ConsumableData Data { get; }
        BlobTier RequiredTier { get; }
        void OnConsumed();
    }
}
