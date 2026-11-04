using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    // Final boss (polis helikopteri) için sadece görsel davranış — oynanışa dokunmaz.
    // Enemy_FinalBoss altındaki "Model" child'ına takılır. Collider/NavMesh kökte kalır,
    // bu script yalnızca Model'in yerel pozisyon/rotasyonunu ve rotorları oynatır.
    // - Rotorlar transform.Rotate ile döner (Animator yok, perf ~0).
    // - Havada süzülür (sinüs bob), hareket yönüne doğru burnunu hafif eğer.
    // - Yükseklik FinalBossController.CurrentAltitude'tan gelir (vurdukça kademe kademe alçalır; oynanış
    //   collider'ı da aynı irtifada). Son kademede (IsEdible) rotor yavaşlar: "hasarlı" işareti.
    //   2026-11-04: eskiden yenebilir fazda yere iniyordu — Serra'nın kararıyla oyuncu helikoptere uzanacak
    //   kadar büyüyüp yutuyor, yere inmiyor. Boss yoksa (önizleme) eski hover/iniş davranışı kalır.
    // - Model > Heli hiyerarşisinde Heli'ye takılır: EnemyBase'in atış geri tepmesi Model'i oynatır,
    //   bu script kendi transform'unu — ikisi birbirini ezmez.
    public class HelicopterVisual : MonoBehaviour
    {
        [Header("Rotors")]
        [SerializeField] private Transform _mainRotor;
        [SerializeField] private Transform _tailRotor;
        [SerializeField] private float _mainRotorSpeed = 1080f;   // derece/sn
        [SerializeField] private float _tailRotorSpeed = 1800f;

        [Header("Hover")]
        [SerializeField] private float _hoverHeight = 1.6f;
        [SerializeField] private float _bobAmplitude = 0.15f;
        [SerializeField] private float _bobFrequency = 1.3f;
        [SerializeField] private float _maxTilt = 12f;            // derece, tam hızda
        [SerializeField] private float _speedForMaxTilt = 4f;

        [Header("Edible Phase")]
        [SerializeField] private float _landedHeight = 0.15f;
        [SerializeField] [Range(0f, 1f)] private float _landedRotorFactor = 0.6f; // son kademede rotor hızı
        [SerializeField] private float _descendSpeed = 1.2f;      // birim/sn

        private EnemyBase _enemy;
        private FinalBossController _boss;
        private Transform _root;
        private Vector3 _basePos;
        private Quaternion _baseRot;
        private float _height;
        private float _bobPhase;
        private Vector3 _tilt; // x = pitch, z = roll (yerel)

        private void Awake()
        {
            _enemy = GetComponentInParent<EnemyBase>();
            _boss = GetComponentInParent<FinalBossController>();
            _root = _enemy != null ? _enemy.transform : transform.parent;
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
        }

        private void OnEnable()
        {
            // Pool'dan tekrar çıkınca havadan başlasın.
            _height = _boss != null ? _boss.CurrentAltitude : _hoverHeight;
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
            _tilt = Vector3.zero;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool edible = _boss != null && _boss.IsEdible;

            if (_boss != null)
            {
                _height = _boss.CurrentAltitude; // irtifa geçişini boss zaten yumuşatıyor
            }
            else
            {
                float targetHeight = edible ? _landedHeight : _hoverHeight;
                _height = Mathf.MoveTowards(_height, targetHeight, _descendSpeed * dt);
            }

            _bobPhase += _bobFrequency * Mathf.PI * 2f * dt;
            float bob = Mathf.Sin(_bobPhase) * _bobAmplitude;

            // Hareket yönüne eğilme: dünya hızını kök uzayına çevir.
            Vector3 targetTilt = Vector3.zero;
            if (_enemy != null && _root != null)
            {
                Vector3 local = _root.InverseTransformDirection(_enemy.CurrentVelocity);
                float k = _maxTilt / _speedForMaxTilt;
                targetTilt.x = Mathf.Clamp(local.z * k, -_maxTilt, _maxTilt);
                targetTilt.z = Mathf.Clamp(-local.x * k, -_maxTilt, _maxTilt);
            }
            _tilt = Vector3.Lerp(_tilt, targetTilt, 4f * dt);

            transform.localPosition = _basePos + new Vector3(0f, _height + bob, 0f);
            transform.localRotation = _baseRot * Quaternion.Euler(_tilt.x, 0f, _tilt.z);

            float rotorFactor = edible ? _landedRotorFactor : 1f;
            if (_mainRotor != null) _mainRotor.Rotate(0f, _mainRotorSpeed * rotorFactor * dt, 0f, Space.Self);
            if (_tailRotor != null) _tailRotor.Rotate(_tailRotorSpeed * rotorFactor * dt, 0f, 0f, Space.Self);
        }
    }
}
