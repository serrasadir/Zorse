using UnityEngine;

namespace BlobSurvivor.Core
{
    // Kale sahnesi (CastleSceneBuilder kurar): bölge sınırlarını MapRegions'a, görünür silah temasını MapTheme'e verir;
    // küçük harita için harita sınırları ve önceden çekilmiş üstten görüntü (Resources/UI/CastleMinimap). Şehirde yok.
    public class CastleMap : MonoBehaviour
    {
        [SerializeField] private float[] _zLines = { -24f, 6f, 32f };
        [SerializeField] private float _side = 24f;
        [SerializeField] private float _half = 56f;
        [SerializeField] private Vector2 _keep = new Vector2(0f, 45f);

        public static CastleMap Instance { get; private set; }

        public float Side => _side;
        public float Half => _half;
        public Vector3 Keep => new Vector3(_keep.x, 0f, _keep.y);

        // Küçük haritanın kapsadığı dünya dikdörtgeni (x0, z0, x1, z1): oyun alanı + yanlardan biraz pay.
        public Rect MinimapWorld => Rect.MinMaxRect(-_side - 2f, -_half, _side + 2f, _half);

        private void Awake()
        {
            Instance = this;
            MapRegions.Set((float[])_zLines.Clone(), _side);
            MapTheme.Set("Med", Resources.Load<Material>("FX/Weapons/Mat_MedWeapon")); // MedievalSkinSetup pişirir
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            MapRegions.Clear();
            MapTheme.Clear();
        }

#if UNITY_EDITOR
        public void EditorSetup(float[] zLines, float side, float half, Vector2 keep)
        {
            _zLines = zLines;
            _side = side;
            _half = half;
            _keep = keep;
        }
#endif
    }
}
