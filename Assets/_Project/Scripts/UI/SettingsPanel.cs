using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BlobSurvivor.Core;

namespace BlobSurvivor.UI
{
    // Ayarlar: Müzik ve Efekt sesi (sürgü), Titreşim (aç/kapa). Lobiden (dişli) ve duraklatma menüsünden açılır.
    // Kök Canvas'ın üstünde tam ekran; düzen ilk açılışta kodla bir kez kurulur. Değerler GameSettings'te.
    public class SettingsPanel : MonoBehaviour
    {
        private static SettingsPanel s_instance;

        private static readonly Color Dim = new Color(0.02f, 0.02f, 0.08f, 0.75f);
        private static readonly Color CardColor = new Color(0.05f, 0.05f, 0.17f, 0.95f);
        private static readonly Color Track = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
        private static readonly Color Off = new Color(0.3f, 0.3f, 0.42f, 1f);
        private static readonly Color Ink = new Color(0.12f, 0.08f, 0.02f, 1f);
        private static readonly Color Subtle = new Color(1f, 1f, 1f, 0.8f);

        private Sprite _rounded, _circle;
        private Slider _music, _sfx;
        private Image _vibrationImage;
        private TMP_Text _vibrationText, _musicValue, _sfxValue;

        public static void Open(Component anyUnderCanvas, Sprite rounded)
        {
            if (s_instance == null)
            {
                Canvas canvas = anyUnderCanvas.GetComponentInParent<Canvas>(true);
                if (canvas == null) return;
                var go = new GameObject("SettingsPanel", typeof(RectTransform));
                go.layer = canvas.gameObject.layer;
                s_instance = go.AddComponent<SettingsPanel>();
                s_instance.Build((RectTransform)canvas.rootCanvas.transform, rounded);
            }
            s_instance.transform.SetAsLastSibling();
            s_instance.Refresh();
            s_instance.gameObject.SetActive(true);
        }

        private void Close()
        {
            GameSettings.Save();
            gameObject.SetActive(false);
        }

        private void Refresh()
        {
            _music.SetValueWithoutNotify(GameSettings.MusicVolume);
            _sfx.SetValueWithoutNotify(GameSettings.SfxVolume);
            _musicValue.text = Percent(GameSettings.MusicVolume);
            _sfxValue.text = Percent(GameSettings.SfxVolume);
            bool on = GameSettings.Vibration;
            _vibrationImage.color = on ? Gold : Off;
            _vibrationText.text = on ? "AÇIK" : "KAPALI";
            _vibrationText.color = on ? Ink : Color.white;
        }

        private static string Percent(float v) => $"%{Mathf.RoundToInt(v * 100f)}";

        // ---------------------------------------------------------------- düzen (bir kez)

        private void Build(RectTransform root, Sprite rounded)
        {
            _rounded = rounded;
            _circle = Resources.Load<Sprite>("UI/Shapes/UI_Circle");
            var rect = (RectTransform)transform;
            rect.SetParent(root, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            // Karartma: dokunmayı tutar, kartın dışına dokunmak kapatır. Kart kardeş (çocuğu olsa tıklama ona da çıkardı).
            RectTransform dim = Box("Dim", rect, Vector2.zero, Vector2.zero, Dim, null);
            dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one;
            Image dimImage = dim.GetComponent<Image>();
            dimImage.raycastTarget = true;
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.targetGraphic = dimImage;
            dimButton.onClick.AddListener(Close);

            RectTransform card = Box("Card", rect, Vector2.zero, new Vector2(900f, 860f), CardColor, _rounded);
            card.GetComponent<Image>().raycastTarget = true; // karta dokunmak kapatmasın

            Text("Title", card, new Vector2(0f, 340f), new Vector2(800f, 100f), 76f, TextAlignmentOptions.Center, Color.white).text = "AYARLAR";

            _music = SliderRow(card, 180f, "Müzik", out _musicValue, v => { GameSettings.MusicVolume = v; _musicValue.text = Percent(v); });
            _sfx = SliderRow(card, 20f, "Efekt", out _sfxValue, v => { GameSettings.SfxVolume = v; _sfxValue.text = Percent(v); });

            Text("VibrationLabel", card, new Vector2(-180f, -140f), new Vector2(400f, 70f), 46f, TextAlignmentOptions.Left, Color.white).text = "Titreşim";
            RectTransform pill = Box("VibrationToggle", card, new Vector2(240f, -140f), new Vector2(260f, 96f), Gold, _rounded);
            _vibrationImage = pill.GetComponent<Image>();
            _vibrationImage.raycastTarget = true;
            var toggle = pill.gameObject.AddComponent<Button>();
            toggle.targetGraphic = _vibrationImage;
            toggle.onClick.AddListener(() =>
            {
                GameSettings.Vibration = !GameSettings.Vibration;
                Refresh();
                Haptics.Impact(1); // açınca hissettir (kapalıyken çalmaz)
            });
            _vibrationText = Text("Label", pill, Vector2.zero, pill.sizeDelta, 40f, TextAlignmentOptions.Center, Ink);

            RectTransform close = Box("CloseButton", card, new Vector2(0f, -320f), new Vector2(560f, 120f), Gold, _rounded);
            Image closeImage = close.GetComponent<Image>();
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(Close);
            Text("Label", close, Vector2.zero, close.sizeDelta, 50f, TextAlignmentOptions.Center, Ink).text = "TAMAM";
        }

        private Slider SliderRow(RectTransform card, float y, string label, out TMP_Text value, UnityEngine.Events.UnityAction<float> onChange)
        {
            Text(label + "Label", card, new Vector2(-180f, y + 50f), new Vector2(400f, 60f), 46f, TextAlignmentOptions.Left, Color.white).text = label;
            value = Text(label + "Value", card, new Vector2(290f, y + 50f), new Vector2(160f, 60f), 40f, TextAlignmentOptions.Right, Subtle);

            // Dokunma alanı geniş (90), görünen ray ince (24).
            RectTransform area = NewRect(label + "Slider", card, new Vector2(0f, y - 20f), new Vector2(740f, 90f));
            var hit = area.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            RectTransform track = Box("Track", area, Vector2.zero, Vector2.zero, Track, _rounded);
            track.anchorMin = new Vector2(0f, 0.5f); track.anchorMax = new Vector2(1f, 0.5f);
            track.sizeDelta = new Vector2(0f, 24f);

            RectTransform fillArea = NewRect("FillArea", area, Vector2.zero, Vector2.zero);
            fillArea.anchorMin = new Vector2(0f, 0.5f); fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.sizeDelta = new Vector2(0f, 24f);
            RectTransform fill = Box("Fill", fillArea, Vector2.zero, Vector2.zero, Gold, _rounded);
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
            fill.sizeDelta = Vector2.zero;

            RectTransform handleArea = NewRect("HandleArea", area, Vector2.zero, Vector2.zero);
            handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(30f, 0f); handleArea.offsetMax = new Vector2(-30f, 0f);
            RectTransform handle = Box("Handle", handleArea, Vector2.zero, new Vector2(60f, 60f), Color.white, _circle != null ? _circle : _rounded);
            handle.anchorMin = handle.anchorMax = new Vector2(0f, 0.5f);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.preserveAspect = _circle != null;

            var slider = area.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f;
            slider.onValueChanged.AddListener(onChange);
            return slider;
        }

        private static RectTransform NewRect(string name, RectTransform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform Box(string name, RectTransform parent, Vector2 pos, Vector2 size, Color color, Sprite sprite)
        {
            RectTransform rect = NewRect(name, parent, pos, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null) { image.sprite = sprite; image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple; }
            return rect;
        }

        private static TMP_Text Text(string name, RectTransform parent, Vector2 pos, Vector2 size, float fontSize,
            TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = NewRect(name, parent, pos, size);
            var t = rect.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = fontSize;
            t.fontStyle = FontStyles.Bold;
            t.alignment = alignment;
            t.color = color;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }
    }
}
