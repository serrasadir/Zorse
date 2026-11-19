using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Evrim "Sabun Sörfü" (Kaygan Zemin 5 + Hız): kendi eski izinde hızlanırsın; izdeki düşmanlar kayar,
    // savrulur, hasar alır ve sersemler (çevik kuvvetin kalkanı iner). SlipTrail.EnableSurf.
    [CreateAssetMenu(fileName = "SoapSurfEffect", menuName = "BlobSurvivor/Effects/Sabun Sörfü (Evolution)")]
    public class SoapSurfEffect : UpgradeEffect
    {
        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            SlipTrail trail = blobRoot.GetComponent<SlipTrail>(); // kart seçiminde, bir kez
            if (trail == null || trail.Surf) return;
            trail.EnableSurf();
            Haptics.Impact(2);
        }
    }
}
