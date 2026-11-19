using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Evrim "Geğirme" (Sindirim Gazı 5 + Hızlı Metabolizma): gaz alanı periyodik olarak patlar (DigestGas.EnableBurp).
    [CreateAssetMenu(fileName = "BurpEffect", menuName = "BlobSurvivor/Effects/Geğirme (Evolution)")]
    public class BurpEffect : UpgradeEffect
    {
        [SerializeField] private float _interval = 6f;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            DigestGas gas = blobRoot.GetComponent<DigestGas>(); // kart seçiminde, bir kez
            if (gas == null || gas.BurpInterval > 0f) return;
            gas.EnableBurp(_interval);
            Haptics.Impact(2);
        }
    }
}
