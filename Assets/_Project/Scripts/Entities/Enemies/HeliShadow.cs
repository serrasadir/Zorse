using UnityEngine;

namespace BlobSurvivor.Entities.Enemies
{
    // 2026-11-04 (Serra: "gölge güzel korkutucu bir his verebilirdi"): helikopterin tam altında, yerde sahte gölge.
    // Tepeden bakan kamerada irtifayı okutur ve tehdidi gösterir: alçaldıkça küçülür, keskinleşir ve koyulaşır
    // ("geliyor"). İki katman: gövdenin yumuşak lekesi + gerçek ana rotorla aynı açıda dönen pervane gölgesi.
    // Kökün (yerde, NavMeshAgent) çocuğudur — modelin süzülmesi/geri tepmesi gölgeyi oynatmaz.
    // Helikopterin kendi gerçek gölgesi kapalı (güneş açısıyla kayık ikinci gölge kafa karıştırırdı).
    // Kurulum: CityKitPipeline.ApplyFinalBossHelicopter (doku/materyal/quad'ları o üretir).
    public class HeliShadow : MonoBehaviour
    {
        [SerializeField] private Renderer _body;
        [SerializeField] private Renderer _rotor;
        [SerializeField] private Transform _rotorSource; // HelicopterVisual'ın döndürdüğü ana rotor pivot'u

        [Header("İrtifaya göre (alçak → yüksek)")]
        [SerializeField] private float _lowAltitude = 5f;
        [SerializeField] private float _highAltitude = 8f;
        [SerializeField] private float _lowAlpha = 0.8f;
        [SerializeField] private float _highAlpha = 0.45f;
        [SerializeField] private float _lowScale = 1f;
        [SerializeField] private float _highScale = 1.3f;
        [SerializeField, Range(0f, 1f)] private float _rotorAlphaFactor = 0.7f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private FinalBossController _boss;
        private MaterialPropertyBlock _block;
        private Vector3 _bodyBaseScale;
        private Vector3 _rotorBaseScale;
        private float _lastT = -1f;

        private void Awake()
        {
            _boss = GetComponentInParent<FinalBossController>();
            _block = new MaterialPropertyBlock();
            if (_body != null) _bodyBaseScale = _body.transform.localScale;
            if (_rotor != null) _rotorBaseScale = _rotor.transform.localScale;
        }

        private void OnEnable() => _lastT = -1f;

        private void LateUpdate()
        {
            // Pervane gölgesi gerçek rotorla aynı açıda (LateUpdate: HelicopterVisual bu kare döndürdükten sonra).
            if (_rotor != null && _rotorSource != null)
                _rotor.transform.rotation = Quaternion.Euler(90f, _rotorSource.eulerAngles.y, 0f);

            float altitude = _boss != null ? _boss.CurrentAltitude : _highAltitude;
            float t = Mathf.InverseLerp(_lowAltitude, _highAltitude, altitude); // 0 = alçak, 1 = yüksek
            if (Mathf.Abs(t - _lastT) < 0.005f) return; // irtifa değişmiyorsa renk/ölçeğe dokunma
            _lastT = t;

            float alpha = Mathf.Lerp(_lowAlpha, _highAlpha, t);
            float scale = Mathf.Lerp(_lowScale, _highScale, t);

            if (_body != null)
            {
                _body.transform.localScale = _bodyBaseScale * scale;
                SetAlpha(_body, alpha);
            }
            if (_rotor != null)
            {
                _rotor.transform.localScale = _rotorBaseScale * scale;
                SetAlpha(_rotor, alpha * _rotorAlphaFactor);
            }
        }

        private void SetAlpha(Renderer renderer, float alpha)
        {
            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, new Color(0f, 0f, 0f, alpha));
            renderer.SetPropertyBlock(_block);
        }
    }
}
