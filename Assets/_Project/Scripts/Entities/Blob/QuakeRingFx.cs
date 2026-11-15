using UnityEngine;

namespace BlobSurvivor.Entities.Blob
{
    // Yer Sarsıntısı görseli: yerden fırlayan kırık asfalt halkası (Blender: build_skill_fx.py → Resources/FX/Quake/FX_Quake,
    // yarıçap 1, şehir paleti). Halka sarsıntı yarıçapına ölçeklenir, yerden fırlar, kısa durur, geri batar.
    // Animasyon koddan (ASSET_RULES: modelde animasyon yok). Tek obje, her sarsıntıda yeniden oynar.
    public class QuakeRingFx : MonoBehaviour
    {
        private const string MeshPath = "FX/Quake/FX_Quake";
        private const string MaterialPath = "FX/Shield/Mat_ShieldPalette"; // CityPalette dokusu, opak
        private const float RiseTime = 0.08f, SettleTime = 0.08f, HoldTime = 0.3f, SinkTime = 0.3f;
        private const float GroundY = 0.15f;

        private MeshRenderer _renderer;
        private float _time = -1f;
        private float _radius;
        private float _height = 1f;

        public static QuakeRingFx Create()
        {
            Mesh mesh = Resources.Load<Mesh>(MeshPath);
            Material material = Resources.Load<Material>(MaterialPath);
            if (mesh == null || material == null) return null;
            var go = new GameObject("QuakeRing");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var fx = go.AddComponent<QuakeRingFx>();
            fx._renderer = go.AddComponent<MeshRenderer>();
            fx._renderer.sharedMaterial = material;
            fx._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fx._renderer.enabled = false;
            return fx;
        }

        // height: büyük sarsıntıda (Deprem) parçalar daha yüksek fırlar.
        public void Play(Vector3 center, float radius, float height = 1f)
        {
            transform.position = new Vector3(center.x, GroundY, center.z);
            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f); // her seferinde farklı dursun
            _radius = radius;
            _height = height;
            _time = 0f;
            _renderer.enabled = true;
            Apply();
        }

        private void LateUpdate()
        {
            if (_time < 0f) return;
            _time += Time.deltaTime;
            if (_time > RiseTime + SettleTime + HoldTime + SinkTime)
            {
                _time = -1f;
                _renderer.enabled = false;
                return;
            }
            Apply();
        }

        private void Apply()
        {
            float t = _time;
            float y, spread;
            if (t < RiseTime) { float k = t / RiseTime; y = 1.25f * k; spread = Mathf.Lerp(0.8f, 1f, k); }
            else if ((t -= RiseTime) < SettleTime) { y = Mathf.Lerp(1.25f, 1f, t / SettleTime); spread = 1f; }
            else if ((t -= SettleTime) < HoldTime) { y = 1f; spread = 1f; }
            else { t -= HoldTime; float k = t / SinkTime; y = 1f - k * k; spread = 1f + 0.05f * k; }
            float r = _radius * spread;
            transform.localScale = new Vector3(r, Mathf.Max(0.001f, y * _height * Mathf.Sqrt(_radius)), r);
        }
    }
}
