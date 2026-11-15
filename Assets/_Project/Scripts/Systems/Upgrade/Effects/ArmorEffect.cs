using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.Systems
{
    // Zırh (aktif): kalkan havuzu, hasarı önce kalkan yer, son hasardan sonra yenilenir. HASAR VERMEZ (dikenler ve
    // şok dalgası kalktı, design/gdd/skill-sistemi.md). Kalkan 20 / 35 + yenilenme 3 sn / 50 + ★ kırılınca 1 sn
    // dokunulmazlık / 65 + 1 sn'de dolar / 80 + ★ ikinci katman (kırılınca bir kez anında dolar, 30 sn'de bir).
    // Havuz BlobHealth'te, görsel ArmorShield'da (form 0..3). Eski adı DamageReductionEffect; guid aynı.
    [CreateAssetMenu(fileName = "ArmorEffect", menuName = "BlobSurvivor/Effects/Armor (Shield)")]
    public class ArmorEffect : UpgradeEffect
    {
        [SerializeField] private float[] _shieldByLevel = { 20f, 35f, 50f, 65f, 80f };
        [SerializeField] private float[] _rechargeDelayByLevel = { 4f, 3f, 3f, 3f, 3f };
        [SerializeField] private float[] _rechargeDurationByLevel = { 2f, 2f, 2f, 1f, 1f };
        [SerializeField] private float _breakInvulnerability = 1f;   // Sv.3
        [SerializeField] private float _secondLayerCooldown = 30f;   // Sv.5

        public override void Apply(GameObject blobRoot, UpgradeData data)
        {
            BlobHealth health = blobRoot.GetComponent<BlobHealth>();
            if (health == null) return;

            int level = LevelOf(data);
            ArmorShield armor = GetOrAdd<ArmorShield>(blobRoot);
            // Kalkan modeli mekanikle birlikte değişir: kapak → tabela → menhol → altın menhol.
            int form = level >= 5 ? 3 : level >= 4 ? 2 : level >= 3 ? 1 : 0;
            armor.Configure(0f, 0f, form);
            health.SetShield(At(_shieldByLevel, level), At(_rechargeDelayByLevel, level), At(_rechargeDurationByLevel, level),
                level >= 3 ? _breakInvulnerability : 0f, level >= 5 ? _secondLayerCooldown : 0f);
        }
    }
}
