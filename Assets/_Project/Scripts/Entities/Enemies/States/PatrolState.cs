using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    public class PatrolState : IEnemyState
    {
        private float _waypointTimer;
        private const float WaypointInterval = 3f;
        private const float PatrolRadius = 8f;

        public void Enter(EnemyBase enemy)
        {
            _waypointTimer = 0f;
            SetRandomDestination(enemy);
        }

        public void Update(EnemyBase enemy, bool aiTick)
        {
            _waypointTimer += Time.deltaTime;
            if (_waypointTimer >= WaypointInterval)
            {
                _waypointTimer = 0f;
                SetRandomDestination(enemy);
            }

            if (enemy.CanSeeBlob())
                enemy.ChangeState(new ChaseState());
        }

        public void Exit(EnemyBase enemy) { }

        private void SetRandomDestination(EnemyBase enemy)
        {
            Vector2 random = Random.insideUnitCircle * PatrolRadius;
            Vector3 destination = enemy.transform.position + new Vector3(random.x, 0f, random.y);
            enemy.SetDestination(destination);
        }
    }
}
