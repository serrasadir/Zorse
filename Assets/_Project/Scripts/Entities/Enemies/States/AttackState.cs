using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    public class AttackState : IEnemyState
    {
        private float _attackTimer;
        private int _burstHitsRemaining;
        private float _burstTimer;

        public void Enter(EnemyBase enemy)
        {
            enemy.StopMoving();
            _attackTimer = 0f;
            _burstHitsRemaining = 0;
        }

        public void Update(EnemyBase enemy, bool aiTick)
        {
            if (enemy.BlobTransform == null || enemy.Data == null) return;

            if (_burstHitsRemaining > 0)
                UpdateBurst(enemy);
            else
                UpdateCooldown(enemy);

            float sqrDist = (enemy.transform.position - enemy.BlobTransform.position).sqrMagnitude;
            float leaveRange = enemy.Data.AttackRange * 1.2f;
            if (sqrDist > leaveRange * leaveRange)
                enemy.ChangeState(new ChaseState());
        }

        private void UpdateCooldown(EnemyBase enemy)
        {
            _attackTimer += Time.deltaTime;
            if (_attackTimer >= enemy.Data.AttackCooldown)
            {
                _attackTimer = 0f;
                // Elit düşman: 3'lü seri vuruş (GDD 5.1). Normal düşman: tek vuruş.
                if (enemy.Data.IsElite && enemy.Data.AttackHitCount > 1)
                {
                    _burstHitsRemaining = enemy.Data.AttackHitCount;
                    _burstTimer = 0f;
                }
                else
                {
                    enemy.PerformAttack();
                }
            }
        }

        private void UpdateBurst(EnemyBase enemy)
        {
            _burstTimer -= Time.deltaTime;
            if (_burstTimer <= 0f)
            {
                enemy.PerformAttack();
                _burstHitsRemaining--;
                _burstTimer = enemy.Data.AttackHitInterval;
            }
        }

        public void Exit(EnemyBase enemy) { }
    }
}
