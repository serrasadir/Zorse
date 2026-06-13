using System.Collections.Generic;
using UnityEngine;

namespace BlobSurvivor.Systems
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        private readonly Dictionary<Object, object> _pools = new Dictionary<Object, object>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public ObjectPool<T> CreatePool<T>(T prefab, int initialSize) where T : MonoBehaviour
        {
            if (_pools.TryGetValue(prefab, out object existing))
                return existing as ObjectPool<T>;

            Transform poolParent = new GameObject($"Pool_{prefab.name}").transform;
            poolParent.SetParent(transform);
            var pool = new ObjectPool<T>(prefab, initialSize, poolParent);
            _pools[prefab] = pool;
            return pool;
        }

        public ObjectPool<T> GetPool<T>(T prefab) where T : MonoBehaviour
        {
            if (_pools.TryGetValue(prefab, out object pool))
                return pool as ObjectPool<T>;
            return null;
        }
    }
}
