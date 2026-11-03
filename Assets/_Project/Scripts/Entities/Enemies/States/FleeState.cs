using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    // 2026-11-03 (Serra): blob polisi yiyebilecek boyuttayken polis kaçar (yayaların kaçışıyla aynı fikir).
    // Blob'dan uzağa bir nokta hedeflenir; arada bina varsa EnemyBase'in yol bulması dolanır.
    // Blob algı menzilinin dışına çıkınca ya da artık yenebilir değilse devriyeye döner.
    public class FleeState : IEnemyState
    {
        private const float FleeLookahead = 8f;
        private const float GiveUpRangeFactor = 1.4f;

        public void Enter(EnemyBase enemy) => SetFleeDestination(enemy);

        public void Update(EnemyBase enemy, bool aiTick)
        {
            if (!aiTick) return;

            if (enemy.BlobTransform == null || !enemy.IsEdibleByBlob || IsFarEnough(enemy))
            {
                enemy.ChangeState(new PatrolState());
                return;
            }

            SetFleeDestination(enemy);
        }

        public void Exit(EnemyBase enemy) { }

        private static bool IsFarEnough(EnemyBase enemy)
        {
            float giveUp = enemy.DetectionRange * GiveUpRangeFactor;
            return (enemy.transform.position - enemy.BlobTransform.position).sqrMagnitude > giveUp * giveUp;
        }

        private static void SetFleeDestination(EnemyBase enemy)
        {
            if (enemy.BlobTransform == null) return;

            Vector3 away = enemy.transform.position - enemy.BlobTransform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = enemy.transform.forward;
            enemy.SetDestination(enemy.transform.position + away.normalized * FleeLookahead);
        }
    }
}
