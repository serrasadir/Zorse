using UnityEngine;

namespace BlobSurvivor.Data
{
    // M24 (GDD_v2.md Karar 14): ConsumableData'yı miras alır — RequiredTier hem "blob beni yiyebilir
    // mi" hem "blob'tan korkmalıyım mı" eşiği olarak aynı alan, ayrı bir field gerekmedi. Yalnızca
    // wander/kaçış hareketi + bob için gereken ek alanlar eklendi.
    [CreateAssetMenu(fileName = "PedestrianData", menuName = "BlobSurvivor/Pedestrian Data")]
    public class PedestrianData : ConsumableData
    {
        [Header("Yaya Hareketi (M24)")]
        [SerializeField] private float _moveSpeed = 1.2f;
        [SerializeField] private float _fleeSpeed = 3f;
        [SerializeField] private float _wanderRadius = 8f;
        [SerializeField] private float _fleeTriggerDistance = 6f;

        [Header("Bob (animasyonsuz yürüme hissi)")]
        [SerializeField] private float _bobHeight = 0.08f;
        [SerializeField] private float _bobSpeed = 6f;

        public float MoveSpeed => _moveSpeed;
        public float FleeSpeed => _fleeSpeed;
        public float WanderRadius => _wanderRadius;
        public float FleeTriggerDistance => _fleeTriggerDistance;
        public float BobHeight => _bobHeight;
        public float BobSpeed => _bobSpeed;
    }
}
