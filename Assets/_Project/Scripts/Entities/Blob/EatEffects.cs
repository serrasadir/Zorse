using UnityEngine;
using BlobSurvivor.Core;

namespace BlobSurvivor.Entities.Blob
{
    // Medium ve üstü lokmalarda geri bildirim: kırıntı patlaması (obje kaybolduğu an onu örter), tier'a göre
    // güçlenen titreşim, kamera sarsıntısı ve görünümün "yutkunması". BlobConsumption ekler ve çağırır.
    public class EatEffects : MonoBehaviour
    {
        private const int CrumbCount = 6; // sabit havuz; hepsi meşgulse en eskisi yeniden kullanılır (Emit eklenir)

        //                                      Medium  Large  Giant  Colossal
        private static readonly float[] Shake = { 0f,    0.05f, 0.08f, 0.12f };
        private static readonly float[] Gulp  = { 0.08f, 0.12f, 0.16f, 0.2f };
        private static readonly int[] Haptic  = { 0,     1,     1,     2 };

        private EatCrumbs[] _crumbs;
        private int _next;
        private CameraController _camera;
        private BlobVisual _visual;

        private void Awake()
        {
            _camera = FindAnyObjectByType<CameraController>();
            _visual = GetComponent<BlobVisual>();

            EatCrumbs prefab = Resources.Load<EatCrumbs>("FX/EatCrumbs");
            if (prefab == null) return;
            // Blob'un child'ı değil: kök ölçeği büyüdükçe parçacıklar da büyümesin.
            Transform container = new GameObject("EatCrumbs").transform;
            _crumbs = new EatCrumbs[CrumbCount];
            for (int i = 0; i < CrumbCount; i++)
                _crumbs[i] = Instantiate(prefab, container); // tek seferlik havuz kurulumu
        }

        private void OnEnable() => GameEvents.OnCharacterSelected += HandleRunStart;
        private void OnDisable() => GameEvents.OnCharacterSelected -= HandleRunStart;

        private void HandleRunStart(Data.CharacterData _)
        {
            if (_crumbs == null) return;
            foreach (EatCrumbs c in _crumbs) c.Stop();
        }

        // Medium altı lokmalar sadece hafif titreşim alır.
        public void Play(BlobTier objectTier, Bounds bounds, Color a, Color b)
        {
            int i = (int)objectTier - (int)BlobTier.Medium;
            if (i < 0)
            {
                Haptics.LightTap();
                return;
            }
            i = Mathf.Min(i, Shake.Length - 1);

            Haptics.Impact(Haptic[i]);
            if (Shake[i] > 0f && _camera != null) _camera.Shake(Shake[i], 0.22f);
            if (_visual != null) _visual.Gulp(Gulp[i]);

            if (_crumbs == null) return;
            Vector3 s = bounds.size;
            float size = Mathf.Pow(Mathf.Max(1e-3f, s.x * s.y * s.z), 1f / 3f);
            _crumbs[_next].Play(bounds, a, b, transform.position, size);
            _next = (_next + 1) % _crumbs.Length;
        }
    }
}
