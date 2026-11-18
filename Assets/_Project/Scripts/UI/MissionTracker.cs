using TMPro;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Systems;

namespace BlobSurvivor.UI
{
    // Anlık görev HUD satırı (slot şeridinin altı) + "Sandığı kap" görevinde hedef oku.
    // Ok hedef ekrandaysa üstünde aşağı bakar, değilse ekran kenarında hedefi gösterir.
    // Metin sadece değer değişince yeniden yazılır.
    public class MissionTracker : MonoBehaviour
    {
        private const float LineTop = 430f, LineHeight = 56f, LineSize = 40f, LineMin = 30f;
        private const float ArrowSize = 96f, EdgeInset = 90f, BobHeight = 14f, BobHz = 2.5f;
        private const float AboveTarget = 110f; // ekranda: hedefin bu kadar px üstü
        private const float ResultShow = 1.5f;

        private static readonly Color LineColor = new Color(0.45f, 0.9f, 1f);
        private static readonly Color FailColor = new Color(1f, 0.55f, 0.5f);
        private static readonly Color DoneColor = new Color(0.55f, 1f, 0.55f);
        private static readonly Color ShadowColor = new Color(0.05f, 0.05f, 0.17f, 0.85f);

        private GameObject _hud;
        private RectTransform _layer;
        private Canvas _canvas;
        private TMP_Text _line, _lineShadow;
        private RectTransform _arrow;
        private Camera _camera;

        private int _shownProgress = -1, _shownSeconds = -1, _shownMeters = -1;
        private MissionType _shownType;
        private MissionResult _shownResult;
        private bool _shownActive;
        private bool _k; // arayüz kiti: konturlu yazı, ayrı gölge yok

        internal void Init(GameObject hud, RectTransform layer)
        {
            _hud = hud;
            _layer = layer;
            _canvas = layer.GetComponentInParent<Canvas>().rootCanvas;

            var safeGo = new GameObject("MissionSafeArea", typeof(RectTransform));
            safeGo.layer = layer.gameObject.layer;
            var safe = (RectTransform)safeGo.transform;
            safe.SetParent(layer, false);
            HudFeedbackLayer.Stretch(safe);
            safeGo.AddComponent<SafeAreaHandler>();

            _lineShadow = MakeLine("MissionLineShadow", safe, new Vector2(0f, -LineTop - 4f), ShadowColor);
            _line = MakeLine("MissionLine", safe, new Vector2(0f, -LineTop), LineColor);
            _k = UiKit.Ready;
            if (_k)
            {
                UiKit.Outlined(_line, LineColor);
                _lineShadow.gameObject.SetActive(false);
            }

            TMP_SpriteAsset glyphs = Resources.Load<TMP_SpriteAsset>("Sprite Assets/UI_Glyphs");
            var arrowGo = new GameObject("MissionArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            arrowGo.layer = layer.gameObject.layer;
            _arrow = (RectTransform)arrowGo.transform;
            _arrow.SetParent(layer, false); // tüm ekran: kenara kadar gidebilsin
            _arrow.anchorMin = _arrow.anchorMax = _arrow.pivot = new Vector2(0.5f, 0.5f);
            _arrow.sizeDelta = new Vector2(ArrowSize, ArrowSize);
            var arrowText = arrowGo.GetComponent<TMP_Text>();
            arrowText.spriteAsset = glyphs;
            arrowText.text = "<sprite name=\"yukari\" tint=1>";
            arrowText.fontSize = ArrowSize;
            arrowText.alignment = TextAlignmentOptions.Center;
            arrowText.color = LineColor;
            arrowText.raycastTarget = false;

            SetVisible(false, false);
        }

        private TMP_Text MakeLine(string name, RectTransform parent, Vector2 pos, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(1000f, LineHeight);
            var text = go.GetComponent<TMP_Text>();
            text.enableAutoSizing = true;
            text.fontSizeMax = LineSize;
            text.fontSizeMin = LineMin;
            text.fontSize = LineSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private void SetVisible(bool line, bool arrow)
        {
            if (_line.gameObject.activeSelf != line)
            {
                _line.gameObject.SetActive(line);
                _lineShadow.gameObject.SetActive(line && !_k);
            }
            if (_arrow.gameObject.activeSelf != arrow) _arrow.gameObject.SetActive(arrow);
        }

        private void LateUpdate()
        {
            MissionSystem m = MissionSystem.Instance;
            bool hudOn = _hud != null && _hud.activeInHierarchy;
            if (m == null || !hudOn) { SetVisible(false, false); return; }

            bool showResult = !m.Active && m.LastResult != MissionResult.None
                && Time.unscaledTime - m.LastResultTime < ResultShow;
            if (!m.Active && !showResult) { SetVisible(false, false); _shownActive = false; return; }

            UpdateLine(m, showResult);
            bool arrow = m.Active && m.Type == MissionType.GrabChest;
            SetVisible(true, arrow);
            if (arrow) UpdateArrow(m.TargetPoint);
        }

        private void UpdateLine(MissionSystem m, bool showResult)
        {
            if (showResult)
            {
                if (_shownActive || _shownResult != m.LastResult)
                {
                    bool done = m.LastResult == MissionResult.Completed;
                    SetText(done ? "Görev tamam!" : "Süre doldu", done ? DoneColor : FailColor);
                    _shownResult = m.LastResult;
                    _shownActive = false;
                }
                return;
            }

            int seconds = Mathf.Max(0, Mathf.CeilToInt(m.TimeLeft));
            int meters = m.Type == MissionType.GrabChest ? Mathf.RoundToInt(m.DistanceToTarget) : 0;
            if (_shownActive && _shownType == m.Type && _shownProgress == m.Progress
                && _shownSeconds == seconds && _shownMeters == meters) return;

            _shownActive = true;
            _shownResult = MissionResult.None;
            _shownType = m.Type;
            _shownProgress = m.Progress;
            _shownSeconds = seconds;
            _shownMeters = meters;

            string detail = m.Target > 0 ? $" {m.Progress}/{m.Target}"
                : m.Type == MissionType.GrabChest ? $" · {meters} m" : "";
            SetText($"GÖREV · {m.Label}{detail} · {seconds / 60}:{seconds % 60:00}", LineColor);
        }

        private void SetText(string text, Color color)
        {
            _line.text = text;
            _lineShadow.text = text;
            _line.color = color;
        }

        private void UpdateArrow(Vector3 world)
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Camera uiCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            Vector3 sp = _camera.WorldToScreenPoint(world);
            bool behind = sp.z < 0f;
            if (behind) sp = -sp; // kameranın arkası: yön ters döner
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            Vector2 p = new Vector2(sp.x, sp.y);
            float scale = _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            float inset = EdgeInset * scale;
            bool onScreen = !behind && p.x > inset && p.x < screen.x - inset && p.y > inset && p.y < screen.y - inset;

            float bob = Mathf.Sin(Time.unscaledTime * BobHz * Mathf.PI * 2f) * BobHeight * scale;
            float angle;
            if (onScreen)
            {
                p.y += AboveTarget * scale + bob;
                angle = 180f; // aşağı bakar
            }
            else
            {
                Vector2 center = screen * 0.5f;
                Vector2 dir = p - center;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
                Vector2 half = center - new Vector2(inset, inset);
                float t = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(dir.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
                p = center + dir * t - dir.normalized * bob;
                angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // ok glifi yukarı bakar
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_layer, p, uiCam, out Vector2 local))
                _arrow.anchoredPosition = local;
            _arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
