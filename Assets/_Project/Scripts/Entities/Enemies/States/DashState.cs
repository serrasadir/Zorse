using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    // Motorlu polis: durur ve blob'a döner (hazırlık, geri yaslanır), sonra o yönde düz çizgide dalar.
    // Dalışta bir kez temas hasarı verir; duvara çarpınca ya da süre bitince kovalamaya döner.
    public class DashState : IEnemyState
    {
        private const float WindupPoseDistance = -0.25f, WindupPosePitch = -16f;
        private const float DashPoseDistance = 0.45f, DashPosePitch = 22f;

        private bool _dashing;
        private float _timer;
        private Vector3 _direction;
        private bool _hit;

        public void Enter(EnemyBase enemy)
        {
            enemy.StopMoving();
            _dashing = false;
            _timer = 0f;
            _hit = false;
            _direction = enemy.DirectionToBlob();
            enemy.PlayPose(WindupPoseDistance, WindupPosePitch);
        }

        public void Update(EnemyBase enemy, bool aiTick)
        {
            if (enemy.Data == null || enemy.IsStunned) { End(enemy); return; }
            _timer += Time.deltaTime;

            if (!_dashing)
            {
                _direction = enemy.DirectionToBlob(); // hazırlıkta hedefi izler, dalışta kilitli
                enemy.FaceTowards(_direction);
                if (_timer < enemy.Data.DashWindup) return;
                _dashing = true;
                _timer = 0f;
                enemy.PlayPose(DashPoseDistance, DashPosePitch);
            }

            bool free = enemy.DashMove(_direction, enemy.Data.DashSpeed);

            if (!_hit)
            {
                float range = enemy.EffectiveAttackRange();
                if (enemy.FlatSqrDistanceToBlob() <= range * range)
                {
                    _hit = true;
                    enemy.PerformAttack();
                    enemy.NextMeleeAttackTime = Time.time + enemy.Data.AttackCooldown;
                }
            }

            if (!free || _timer >= enemy.Data.DashDuration) End(enemy);
        }

        private static void End(EnemyBase enemy)
        {
            enemy.NextDashTime = Time.time + (enemy.Data != null ? enemy.Data.DashCooldown : 3f);
            enemy.ChangeState(new ChaseState());
        }

        public void Exit(EnemyBase enemy) { }
    }
}
