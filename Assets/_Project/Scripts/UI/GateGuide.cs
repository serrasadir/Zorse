using BlobSurvivor.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlobSurvivor.UI
{
    // Kale haritasında yön: sol altta küçük harita
    // (önceden çekilmiş üstten görüntü Resources/UI/CastleMinimap + blob, kapılar, burç işaretleri). Şehirde (CastleMap yok)
    // hiçbir şey göstermez. HudFeedbackLayer kurar.
    public class GateGuide : MonoBehaviour
    {
        private const float MapWidth = 190f, MapMargin = 26f, MarkerSize = 16f, BlobMarkerSize = 22f;
        private const float MapInterval = 0.1f;
        private static readonly Color GateLocked = new Color(1f, 0.85f, 0.4f);
        private static readonly Color GateReady = new Color(0.45f, 1f, 0.4f);
        private static readonly Color KeepColor = new Color(0.95f, 0.3f, 0.25f);

        private RectTransform _layer, _map, _blobMarker, _keepMarker;
        private Image[] _gateMarkers;
        private Canvas _canvas;
        private bool _built, _castle;
        private float _mapTimer;

        internal void Init(GameObject hud, RectTransform layer)
        {
            _layer = layer;
            _canvas = layer.GetComponentInParent<Canvas>().rootCanvas;
        }

        private void Build()
        {
            _built = true;
            _castle = CastleMap.Instance != null;
            if (!_castle) return;

            Texture2D tex = Resources.Load<Texture2D>("UI/CastleMinimap"); // CastleSceneBuilder çeker
            if (tex == null) return;
            Rect world = CastleMap.Instance.MinimapWorld;
            float height = MapWidth * world.height / world.width;

            var safeGo = new GameObject("GuideSafeArea", typeof(RectTransform));
            safeGo.layer = _layer.gameObject.layer;
            var safe = (RectTransform)safeGo.transform;
            safe.SetParent(_layer, false);
            HudFeedbackLayer.Stretch(safe);
            safeGo.AddComponent<SafeAreaHandler>();

            var frame = NewRect("Minimap", safe, new Vector2(MapWidth + 10f, height + 10f));
            frame.anchorMin = frame.anchorMax = frame.pivot = Vector2.zero;
            frame.anchoredPosition = new Vector2(MapMargin, MapMargin);
            var bg = frame.gameObject.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.1f, 0.18f, 0.75f);
            bg.raycastTarget = false;

            _map = NewRect("Map", frame, new Vector2(MapWidth, height));
            var raw = _map.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.raycastTarget = false;
            raw.color = new Color(1f, 1f, 1f, 0.92f);

            _gateMarkers = new Image[ZoneGate.All.Count];
            for (int i = 0; i < _gateMarkers.Length; i++)
                _gateMarkers[i] = Marker("Gate" + i, MarkerSize, GateLocked);
            _keepMarker = Marker("Keep", MarkerSize * 1.2f, KeepColor).rectTransform;
            _blobMarker = Marker("Blob", BlobMarkerSize, Color.white).rectTransform;
            _keepMarker.anchoredPosition = WorldToMap(CastleMap.Instance.Keep);
        }

        private RectTransform NewRect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = _layer.gameObject.layer;
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            r.sizeDelta = size;
            return r;
        }

        private Image Marker(string name, float size, Color color)
        {
            RectTransform r = NewRect(name, _map, new Vector2(size, size));
            r.localRotation = Quaternion.Euler(0f, 0f, 45f); // elmas
            var img = r.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var outline = r.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(2f, -2f);
            return img;
        }

        private Vector2 WorldToMap(Vector3 p)
        {
            Rect w = CastleMap.Instance.MinimapWorld;
            float u = Mathf.InverseLerp(w.xMin, w.xMax, p.x) - 0.5f;
            float v = Mathf.InverseLerp(w.yMin, w.yMax, p.z) - 0.5f;
            return new Vector2(u * _map.sizeDelta.x, v * _map.sizeDelta.y);
        }

        private void Update()
        {
            if (!_built) Build();
            if (!_castle || CastleMap.Instance == null) return;
            Vector3 blob = MapRegions.BlobPosition;
            ZoneGate next = ZoneGate.NextFor(blob);

            if (_map == null) return;
            _mapTimer -= Time.unscaledDeltaTime;
            if (_mapTimer > 0f) return;
            _mapTimer = MapInterval;
            _blobMarker.anchoredPosition = WorldToMap(blob);
            for (int i = 0; i < _gateMarkers.Length && i < ZoneGate.All.Count; i++)
            {
                ZoneGate g = ZoneGate.All[i];
                if (g == null) continue;
                _gateMarkers[i].rectTransform.anchoredPosition = WorldToMap(g.Position);
                _gateMarkers[i].color = g.Ready ? GateReady : GateLocked;
                float pulse = g == next ? 1f + Mathf.PingPong(Time.unscaledTime * 2f, 0.4f) : 1f; // sıradaki kapı
                _gateMarkers[i].rectTransform.localScale = Vector3.one * pulse;
            }
        }
    }
}
