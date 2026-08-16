using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Entities.Enemies
{
    // A16 (GDD_v2.md Karar 1 + 8, §4): final boss tek faz geçişi yapar.
    // HP eşiğinin altına inince "yenebilir faz"a geçer — EnemyData.RequiresConsumptionToDie
    // sayesinde silah hasarıyla asla ölmez (HP 1'de kilitlenir, bkz. EnemyBase.TakeDamage),
    // bu script sadece HP eşiğini izleyip EnemyBase.SetConsumableOverride(true) çağırır.
    // "Vurarak son faza getirirsin, yutarak bitirirsin." — ölüm sadece TryConsumeByBlob üzerinden olur.
    [RequireComponent(typeof(EnemyBase))]
    public class FinalBossController : MonoBehaviour
    {
        [SerializeField] [Range(0.05f, 0.9f)] private float _edibleHealthFraction = 0.25f;

        private EnemyBase _enemyBase;
        private bool _isEdible;

        private void Awake()
        {
            _enemyBase = GetComponent<EnemyBase>();
        }

        private void OnEnable()
        {
            _isEdible = false;
            _enemyBase.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            _enemyBase.OnDeath -= HandleDeath;
        }

        private void Update()
        {
            if (_isEdible || _enemyBase.MaxHealth <= 0f) return;

            if (_enemyBase.CurrentHealth / _enemyBase.MaxHealth <= _edibleHealthFraction)
            {
                _isEdible = true;
                _enemyBase.SetConsumableOverride(true);
#if UNITY_EDITOR
                Debug.Log("[FinalBossController] Yenebilir faza geçti — artık Tier5 blob tarafından yutulabilir.");
#endif
            }
        }

        private void HandleDeath(EnemyBase boss)
        {
#if UNITY_EDITOR
            Debug.Log("[FinalBossController] Final boss yutuldu — OnFinalBossConsumed ateşleniyor.");
#endif
            GameEvents.RaiseFinalBossConsumed();
        }
    }
}
