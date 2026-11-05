using UnityEngine;

namespace BlobSurvivor.Core
{
    // Şehir dışındaki suyun dokusunu yavaşça kaydırır (Şehir Kıyısını Kur ekler). Paylaşılan materyale
    // dokunmaz: sadece bu renderer'a MaterialPropertyBlock ile _BaseMap_ST yazar. Kare başına tek vektör.
    [RequireComponent(typeof(Renderer))]
    public class WaterScroll : MonoBehaviour
    {
        private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");

        [SerializeField] private Vector2 _speed = new Vector2(0.012f, 0.007f); // doku/sn
        [SerializeField] private float _wobble = 0.01f;                        // hafif ileri-geri salınım

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private Vector4 _st = new Vector4(1f, 1f, 0f, 0f);

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
            Material m = _renderer.sharedMaterial;
            if (m != null && m.HasProperty(BaseMapST)) _st = m.GetVector(BaseMapST);
        }

        private void Update()
        {
            float t = Time.time;
            Vector4 st = _st;
            st.z = Mathf.Repeat(_st.z + _speed.x * t + Mathf.Sin(t * 0.7f) * _wobble, 1f);
            st.w = Mathf.Repeat(_st.w + _speed.y * t, 1f);
            _block.SetVector(BaseMapST, st);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
