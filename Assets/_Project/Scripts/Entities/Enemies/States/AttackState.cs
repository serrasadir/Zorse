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

        public void Update(EnemyBase enemy)
        {
            if (enemy.BlobTransform == null || enemy.Data == null) return;

            _attackTimer += Time.deltaTime;
            if (_attackTimer >= enemy.Data.AttackCooldown)
            {
                _attackTimer = 0f;
                enemy.PerformAttack();
            }

            float dist = Vector3.Distance(enemy.transform.position, enemy.BlobTransform.position);
            if (dist > enemy.Data.AttackRange * 1.2f)
                enemy.ChangeState(new ChaseState());
        }

        public void Exit(EnemyBase enemy) { }
    }
}
