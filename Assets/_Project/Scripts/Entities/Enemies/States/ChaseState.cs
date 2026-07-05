using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    public class ChaseState : IEnemyState
    {
        public void Enter(EnemyBase enemy) { }

        public void Update(EnemyBase enemy, bool aiTick)
        {
            if (enemy.BlobTransform == null || enemy.Data == null) return;

            // NavMesh pathfinding pahalı — her frame değil, throttle tick'te yeniden hedeflenir
            if (aiTick)
                enemy.SetDestination(enemy.BlobTransform.position);

            float sqrDist = (enemy.transform.position - enemy.BlobTransform.position).sqrMagnitude;
            if (sqrDist <= enemy.Data.AttackRange * enemy.Data.AttackRange)
                enemy.ChangeState(new AttackState());
            else if (!enemy.CanSeeBlob())
                enemy.ChangeState(new PatrolState());
        }

        public void Exit(EnemyBase enemy) { }
    }
}
