using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Blob
{
    // B17 evrimi "Avcı Formu" (Yırtıcı Çene(max) + Silah Gücü(max)): silahla vurulan düşmanlar kısa
    // süreliğine tier şartı olmadan yutulabilir hale gelir. 2026-11-03: elitler de dahil (Yırtıcı Çene
    // artık elitleri kapsamıyor, elit yemek bu evrimin ödülü). Bosslar PreventConsumption ile korunur.
    public class AvciFormuComponent : MonoBehaviour
    {
        [SerializeField] private float _markDuration = 3f;

        private void OnEnable() => EnemyBase.OnAnyEnemyDamaged += HandleEnemyDamaged;
        private void OnDisable() => EnemyBase.OnAnyEnemyDamaged -= HandleEnemyDamaged;

        private void HandleEnemyDamaged(EnemyBase enemy)
        {
            if (enemy != null)
                enemy.MarkTemporarilyConsumable(_markDuration);
        }
    }
}
