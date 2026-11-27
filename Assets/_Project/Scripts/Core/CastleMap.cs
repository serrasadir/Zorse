using UnityEngine;

namespace BlobSurvivor.Core
{
    // Bölgeli harita (Crownreach; CrownreachMapBuilder kurar): bölge sınırlarını MapRegions'a, görünür silah temasını MapTheme'e verir;
    // küçük harita için harita sınırları ve önceden çekilmiş üstten görüntü (Resources/UI/CastleMinimap). Şehirde yok.
    public class CastleMap : MonoBehaviour
    {
        [SerializeField] private float[] _zLines = { -24f, 6f, 32f };
        [SerializeField] private float _side = 24f;
        [SerializeField] private float _half = 56f;
        [SerializeField] private Vector2 _minimapZ;
        [SerializeField] private Vector2 _keep = new Vector2(0f, 45f);
        [SerializeField] private string _minimap = "UI/CastleMinimap"; // Resources yolu (Crownreach kendi görüntüsünü verir)

        public static CastleMap Instance { get; private set; }
        public string MinimapResource => _minimap;

        public float Side => _side;
        public float Half => _half;
        public Vector3 Keep => new Vector3(_keep.x, 0f, _keep.y);

        // Küçük haritanın kapsadığı dünya dikdörtgeni (x0, z0, x1, z1): oyun alanı + yanlardan biraz pay.
        // Küçük harita adanın z aralığını kaplar (sıfırsa ±_half: ada ortada değilse üstte boş deniz kalıyordu).
        public Rect MinimapWorld => _minimapZ == Vector2.zero
            ? Rect.MinMaxRect(-_side - 2f, -_half, _side + 2f, _half)
            : Rect.MinMaxRect(-_side - 2f, _minimapZ.x, _side + 2f, _minimapZ.y);

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

        public void EditorSetMinimap(string resource, Vector2 z)
        {
            _minimap = resource;
            _minimapZ = z;
        }
#endif
    }
}
