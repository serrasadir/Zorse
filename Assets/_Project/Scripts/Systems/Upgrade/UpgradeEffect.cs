using UnityEngine;
using BlobSurvivor.Data;

namespace BlobSurvivor.Systems
{
    public abstract class UpgradeEffect : ScriptableObject
    {
        public abstract void Apply(GameObject blobRoot, UpgradeData data);
    }
}
