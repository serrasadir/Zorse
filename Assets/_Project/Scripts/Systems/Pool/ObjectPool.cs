using System.Collections.Generic;
using UnityEngine;

namespace BlobSurvivor.Systems
{
    public class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _pool = new Queue<T>();

        public ObjectPool(T prefab, int initialSize, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < initialSize; i++)
                _pool.Enqueue(CreateInstance());
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.gameObject.SetActive(true);
            return obj;
        }

        public void Return(T obj)
        {
            // Zaten havuzdaysa (pasif) tekrar kuyruğa ekleme — çift dönüş aynı nesneyi iki Get()'e verirdi.
            // Blob'da iki collider var, tek temas aynı adımda iki OnTriggerEnter üretebiliyor (2026-11-03).
            if (obj == null || !obj.gameObject.activeSelf) return;
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }

        private T CreateInstance()
        {
            T obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            return obj;
        }
    }

}
