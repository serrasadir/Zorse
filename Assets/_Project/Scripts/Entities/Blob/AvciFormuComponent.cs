using UnityEngine;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.Entities.Blob
{
    // B17 evrimi "Avcı Formu" (Yırtıcı Çene(max) + Silah Gücü(max)): silahla vurulan sürü
    // düşmanları kısa süreliğine tier şartı olmadan yutulabilir hale gelir.
    public class AvciFormuComponent : MonoBehaviour
    {
        [SerializeField] private float _markDuration = 3f;

        private void OnEnable() => EnemyBase.OnAnyEnemyDamaged += HandleEnemyDamaged;
        private void OnDisable() => EnemyBase.OnAnyEnemyDamaged -= HandleEnemyDamaged;

        private void HandleEnemyDamaged(EnemyBase enemy)
        {
            if (enemy != null && !enemy.Data.IsElite)
                enemy.MarkTemporarilyConsumable(_markDuration);
        }
    }
}
