using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    // Elit polisin tabanca saldırısı: dur, lazerle takip et, yönü kilitle, seriyi kilitli çizgiye ateşle.
    // Son andaki kilit oyuncuya kaçma şansı verir; lazerin kalınlaşması bu anı renkten bağımsız gösterir.
    public class RangedAttackState : IEnemyState
    {
        private enum Phase { Cooldown, Tracking, Locked, Burst }

        private const float TrackingLineWidth = 0.05f;
        private const float LockedLineWidth = 0.16f;
        private const float LeaveRangeMultiplier = 1.2f;

        private Phase _phase;
        private float _timer;
        private Vector3 _aimDirection;
        private float _aimLength;
        private int _shotsRemaining;

        public void Enter(EnemyBase enemy)
        {
            enemy.StopMoving();

            if (Time.time >= enemy.NextRangedAttackTime)
                BeginTracking(enemy);
            else
                _phase = Phase.Cooldown;
        }

        public void Update(EnemyBase enemy, bool aiTick)
        {
            if (enemy.BlobTransform == null || enemy.Data == null) return;

            switch (_phase)
            {
                case Phase.Cooldown: UpdateCooldown(enemy, aiTick); break;
                case Phase.Tracking: UpdateTracking(enemy, aiTick); break;
                case Phase.Locked: UpdateLocked(enemy); break;
                case Phase.Burst: UpdateBurst(enemy); break;
            }
        }

        public void Exit(EnemyBase enemy) => enemy.HideAimLine();

        private void BeginTracking(EnemyBase enemy)
        {
            _phase = Phase.Tracking;
            _timer = 0f;
            _aimDirection = enemy.AttackDirectionToBlob();
            _aimLength = enemy.ComputeAimLength(_aimDirection);
        }

        private void UpdateCooldown(EnemyBase enemy, bool aiTick)
        {
            enemy.FaceTowards(enemy.DirectionToBlob());
            if (!aiTick) return;

            if (!CanKeepAiming(enemy))
                enemy.ChangeState(new ChaseState());
            else if (Time.time >= enemy.NextRangedAttackTime)
                BeginTracking(enemy);
        }

        private void UpdateTracking(EnemyBase enemy, bool aiTick)
        {
            if (aiTick && !CanKeepAiming(enemy))
            {
                enemy.ChangeState(new ChaseState());
                return;
            }

            _aimDirection = enemy.AttackDirectionToBlob();
            enemy.FaceTowards(enemy.DirectionToBlob());
            if (aiTick) _aimLength = enemy.ComputeAimLength(_aimDirection);
            enemy.ShowAimLine(_aimDirection, _aimLength, TrackingLineWidth);

            _timer += Time.deltaTime;
            if (_timer < enemy.Data.AimDuration - enemy.Data.AimLockDuration) return;

            _phase = Phase.Locked;
            _timer = 0f;
            _aimLength = enemy.ComputeAimLength(_aimDirection);
        }

        private void UpdateLocked(EnemyBase enemy)
        {
            // Kilitli yöne bakar (havadaki düşmanın aşağı eğik yönü yataya indirilir, kök eğilmesin).
            Vector3 face = new Vector3(_aimDirection.x, 0f, _aimDirection.z);
            if (face.sqrMagnitude > 0.0001f) enemy.FaceTowards(face);
            enemy.ShowAimLine(_aimDirection, _aimLength, LockedLineWidth);

            _timer += Time.deltaTime;
            if (_timer < enemy.Data.AimLockDuration) return;

            enemy.HideAimLine();
            _phase = Phase.Burst;
            _timer = 0f;
            _shotsRemaining = enemy.Data.AttackHitCount;
        }

        private void UpdateBurst(EnemyBase enemy)
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            enemy.FireProjectile(_aimDirection);
            _shotsRemaining--;
            _timer = enemy.Data.AttackHitInterval;

            if (_shotsRemaining > 0) return;

            enemy.NextRangedAttackTime = Time.time + enemy.Data.AttackCooldown;
            _phase = Phase.Cooldown;
        }

        private static bool CanKeepAiming(EnemyBase enemy)
        {
            float leaveRange = enemy.EffectiveAttackRange() * LeaveRangeMultiplier;
            float sqrDist = enemy.FlatSqrDistanceToBlob();
            return sqrDist <= leaveRange * leaveRange && enemy.HasLineOfSightToBlob();
        }
    }
}
