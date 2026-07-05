using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    public class AttackState : IEnemyState
    {
        private float _attackTimer;

        public void Enter(EnemyBase enemy)
        {
            enemy.StopMoving();
            _attackTimer = 0f;
        }

        public void Update(EnemyBase enemy, bool aiTick)
        {
            if (enemy.BlobTransform == null || enemy.Data == null) return;

            _attackTimer += Time.deltaTime;
            if (_attackTimer >= enemy.Data.AttackCooldown)
            {
                _attackTimer = 0f;
                enemy.PerformAttack();
            }

            float sqrDist = (enemy.transform.position - enemy.BlobTransform.position).sqrMagnitude;
            float leaveRange = enemy.Data.AttackRange * 1.2f;
            if (sqrDist > leaveRange * leaveRange)
                enemy.ChangeState(new ChaseState());
        }

        public void Exit(EnemyBase enemy) { }
    }
}
