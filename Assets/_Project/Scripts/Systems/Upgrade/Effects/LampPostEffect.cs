using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Weapons;

namespace BlobSurvivor.Systems
{
    // Evrim "Lamba Direği" (Sopa 5 + Yara Öfkesi): sopa söküp alınmış sokak lambasına döner — tam tur,
    // menzil ve geri itme artar, öldürdüğünü yutar (StickWeapon.EnableLampPost). Görsel: G_Stick_5.
    [CreateAssetMenu(fileName = "LampPostEffect", menuName = "BlobSurvivor/Effects/Lamba Direği (Evolution)")]
    public class LampPostEffect : UpgradeEffect
    {
        public const int Form = 5;

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            var stick = blobRoot.GetComponentInChildren<StickWeapon>(); // kart seçiminde, bir kez
            if (stick == null || stick.IsLampPost) return;
            stick.EnableLampPost();

            WeaponVisual visual = blobRoot.GetComponent<WeaponVisual>();
            if (visual != null)
            {
                visual.SetForm(Form);
                visual.PlaySwing(360f, true); // kart kapanınca lambayı bir tur döndürerek gösterir
            }
            Haptics.Impact(2);
        }
    }
}
