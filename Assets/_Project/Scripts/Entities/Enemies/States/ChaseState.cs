using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    public class ChaseState : IEnemyState
    {
        public void Enter(EnemyBase enemy) { }

        public void Update(EnemyBase enemy)
        {
            if (enemy.BlobTransform == null || enemy.Data == null) return;

            enemy.SetDestination(enemy.BlobTransform.position);

            float dist = Vector3.Distance(enemy.transform.position, enemy.BlobTransform.position);
            if (dist <= enemy.Data.AttackRange)
                enemy.ChangeState(new AttackState());
            else if (!enemy.CanSeeBlob())
                enemy.ChangeState(new PatrolState());
        }

        public void Exit(EnemyBase enemy) { }
    }
}
