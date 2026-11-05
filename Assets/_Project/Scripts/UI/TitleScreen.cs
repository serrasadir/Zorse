using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Blob;

namespace BlobSurvivor.UI
{
    // Giriş ekranı: oyun açılınca ve "Ana Menü"de. Arkada gerçek şehir canlı akar, kamera Topik'e yaklaşır; Topik
    // göz kırpar (BlobTierMeshes) ve sağa sola bakınır (BlobVisual.MenuIdle). Üstte logo, altta nefes alan OYNA →
    // lobi (karakter seçimi). Sahne kurulumu yok: sahne yüklenince kendini kök Canvas'a kurar.
    public class TitleScreen : MonoBehaviour
    {
        private static TitleScreen s_instance;

        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color Shadow = new Color(0.05f, 0.05f, 0.17f, 0.85f);
        private static readonly Color Crimson = new Color(0.545f, 0f, 0f, 1f);
        private const float TitleZoom = 0.35f;

        private RectTransform _root;
        private RectTransform _playButton;
        private LobbyPanel _lobby;
        private CameraController _camera;
        private bool _entering;
        private bool _shown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            LobbyPanel lobby = FindAnyObjectByType<LobbyPanel>(FindObjectsInactive.Include);
            Canvas canvas = lobby != null ? lobby.GetComponentInParent<Canvas>(true) : null;
            if (canvas == null) return;
            var go = new GameObject("TitleScreen", typeof(RectTransform));
            go.layer = canvas.gameObject.layer;
            go.transform.SetParent(canvas.rootCanvas.transform, false);
            s_instance = go.AddComponent<TitleScreen>();
            s_instance._lobby = lobby;
        }

        // Pause menüsü / oyun sonu "Ana Menü".
        public static void ShowTitle()
        {
            if (s_instance != null) s_instance.Show();
        }

        private void OnEnable() => GameEvents.OnCharacterSelected += HandleRunStarted;
        private void OnDisable() => GameEvents.OnCharacterSelected -= HandleRunStarted;

        private void Start()
        {
            _camera = FindAnyObjectByType<CameraController>();
            Build();
            StartCoroutine(ShowAfterLobbyStart()); // LobbyPanel.Start paneli açar; bir kare sonra üstüne geç
        }

        private IEnumerator ShowAfterLobbyStart()
        {
            yield return null;
            Show();
        }

        private void Show()
        {
            _entering = true;
            GameManager.Instance?.EnterTitle(); // run'ı sıfırlar (OnCharacterSelected → aşağıda yok sayılır)
            _entering = false;

            _lobby?.HideForTitle();
            _root.gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _shown = true;
            SetMenuCamera(true);
        }

        private void OnPlay()
        {
            _root.gameObject.SetActive(false);
            _shown = false;
            _lobby?.Show(); // karakter seçimi; kamera Topik'e yakın kalır
        }

        // Lobi'de karakter seçildi → oyun başladı.
        private void HandleRunStarted(CharacterData _)
        {
            if (_entering) return;
            _root.gameObject.SetActive(false);
            _shown = false;
            SetMenuCamera(false);
        }

        private void SetMenuCamera(bool on)
        {
            if (_camera != null) _camera.CinematicZoom = on ? TitleZoom : 1f;
            GameObject blob = GameObject.FindWithTag("Blob");
            BlobVisual visual = blob != null ? blob.GetComponent<BlobVisual>() : null;
            if (visual != null) visual.MenuIdle = on;
        }

        private void Update()
        {
            if (!_shown || _playButton == null) return;
            float t = Time.unscaledTime;
            _playButton.localScale = Vector3.one * (1f + Mathf.Sin(t * 3.2f) * 0.035f); // nefes alan OYNA
        }

        // ---------------------------------------------------------------- düzen (bir kez)

        private void Build()
        {
            // Bileşenin objesi hep açık kalır (coroutine/Update); açılıp kapanan içerik ayrı child.
            var self = (RectTransform)transform;
            self.anchorMin = Vector2.zero;
            self.anchorMax = Vector2.one;
            self.offsetMin = self.offsetMax = Vector2.zero;
            _root = NewRect("Content", self, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Yazılar güvenli alanda (çentik/alt şerit dışında).
            RectTransform safe = NewRect("Safe", _root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Rect area = Screen.safeArea;
            Vector2 min = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            Vector2 max = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            safe.anchorMin = min;
            safe.anchorMax = max;

            ShadowedLabel("LogoTop", safe, new Vector2(0.5f, 1f), new Vector2(0f, -250f), "LEFT NO", 120f, Color.white);
            ShadowedLabel("LogoBottom", safe, new Vector2(0.5f, 1f), new Vector2(0f, -410f), "CRUMBS", 200f, Gold);
            ShadowedLabel("Tagline", safe, new Vector2(0.5f, 1f), new Vector2(0f, -545f), "Ye, büyü... polisi bile ye!", 46f,
                new Color(1f, 1f, 1f, 0.92f));

            Sprite rounded = FindRoundedSprite();
            _playButton = NewRect("PlayButton", safe, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            _playButton.pivot = new Vector2(0.5f, 0.5f);
            _playButton.anchoredPosition = new Vector2(0f, 300f);
            _playButton.sizeDelta = new Vector2(640f, 180f);
            var image = _playButton.gameObject.AddComponent<Image>();
            image.color = Crimson;
            if (rounded != null) { image.sprite = rounded; image.type = Image.Type.Sliced; }
            var button = _playButton.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OnPlay);
            TMP_Text label = Label("Label", _playButton, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 180f), "OYNA", 96f, Color.white);
            label.raycastTarget = false;

            _root.gameObject.SetActive(false);
        }

        private static Sprite FindRoundedSprite()
        {
            foreach (Image img in FindObjectsByType<Image>(FindObjectsInactive.Include))
                if (img.sprite != null && img.sprite.name == "UISprite") return img.sprite;
            return null;
        }

        private static RectTransform NewRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        private static TMP_Text Label(string name, RectTransform parent, Vector2 anchor, Vector2 pos, Vector2 size,
            string text, float fontSize, Color color)
        {
            RectTransform rect = NewRect(name, parent, anchor, anchor, Vector2.zero, Vector2.zero);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var t = rect.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        // Şehir üstünde okunsun diye koyu gölge kopyası (malzeme kopyası/outline yok, sıfır maliyet).
        private static void ShadowedLabel(string name, RectTransform parent, Vector2 anchor, Vector2 pos, string text, float size, Color color)
        {
            var box = new Vector2(1000f, size * 1.3f);
            Label(name + "Shadow", parent, anchor, pos + new Vector2(0f, -size * 0.06f), box, text, size, Shadow);
            Label(name, parent, anchor, pos, box, text, size, color);
        }
    }
}
