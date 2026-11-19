using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Zırh (pasif, VS "Armor" gibi): gelen her darbeden sabit hasar düşer (−1 … −5; darbenin en az %30'u geçer,
    // Market Zırh'ıyla toplanır). Yenilenen kalkan havuzu yok. ★ Sv.3: darbe sonrası dokunulmazlık uzar.
    // ★ Sv.5: darbe alınca etraftaki düşmanlar 2 kat uzağa sekilir. Görsel ArmorShield (form 0..3), düşülen
    // hasarda kalkan parlar. Eski adı DamageReductionEffect; guid aynı.
    [CreateAssetMenu(fileName = "ArmorEffect", menuName = "BlobSurvivor/Effects/Armor")]
    public class ArmorEffect : UpgradeEffect
    {
        [SerializeField] private float[] _reductionByLevel = { 1f, 2f, 3f, 4f, 5f };
        [SerializeField] private float _invulnerabilityBonus = 0.25f; // Sv.3+: 0.35 → 0.6 sn
        [SerializeField] private float _pushMultiplier = 2f;          // Sv.5

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            BlobHealth health = blobRoot.GetComponent<BlobHealth>();
            if (health == null) return;

            int level = LevelOf(data);
            ArmorShield armor = GetOrAdd<ArmorShield>(blobRoot);
            // Kalkan modeli seviyeyle değişir: kapak → tabela → menhol → altın menhol.
            int form = level >= 5 ? 3 : level >= 4 ? 2 : level >= 3 ? 1 : 0;
            armor.SetForm(form);
            health.SkillArmor = At(_reductionByLevel, level);
            health.HitInvulnerabilityBonus = level >= 3 ? _invulnerabilityBonus : 0f;
            health.HitPushMultiplier = level >= 5 ? _pushMultiplier : 1f;
        }
    }
}
