using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    // Çevik kuvvet: sersemleyince kalkan (kolla birlikte, omuzdan) yana açılıp sarkar, toparlanınca kalkar.
    // Kalkan parçası ve indirilmiş pozu Tools → Blob.io → Polis Yürüyüş Parçalarını Kur yazar.
    // Hasar tarafı EnemyBase.IsShieldDown (FrontShieldFactor) — görsel aynı durumu okur.
    public class ShieldLower : MonoBehaviour
    {
        [SerializeField] private Transform _shield;
        [SerializeField] private Quaternion _lowered = Quaternion.identity; // Model-yerel
        [SerializeField] private float _lowerSpeed = 7f; // 1/sn: inişi hızlı (vuruş anı belli olsun)
        [SerializeField] private float _raiseSpeed = 2.5f;

        private EnemyBase _enemy;
        private Renderer _renderer;
        private float _amount;

        private void Awake()
        {
            _enemy = GetComponent<EnemyBase>();
            if (_shield != null) _renderer = _shield.GetComponent<Renderer>();
        }

        private void OnEnable()
        {
            _amount = 0f;
            if (_shield != null) _shield.localRotation = Quaternion.identity;
        }

        private void LateUpdate()
        {
            if (_shield == null || _enemy == null) return;
            bool down = _enemy.IsShieldDown;
            float target = down ? 1f : 0f;
            if (Mathf.Approximately(_amount, target)) return;
            _amount = Mathf.MoveTowards(_amount, target, (down ? _lowerSpeed : _raiseSpeed) * Time.deltaTime);
            if (_renderer != null && !_renderer.isVisible) return;
            float eased = _amount * _amount * (3f - 2f * _amount);
            _shield.localRotation = Quaternion.Slerp(Quaternion.identity, _lowered, eased);
        }
    }
}
