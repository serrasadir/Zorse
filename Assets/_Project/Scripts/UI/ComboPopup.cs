using TMPro;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.UI
{
    // Kombo yazısı: kısa aralıkla (ChainWindow) gelen öldürmeler zincir sayılır (silah ya da yutma).
    // 3'ten itibaren blob'un üstünde "ÜÇLÜ KOMBO!" … her yeni öldürmede yazı zıplar, sayı büyüdükçe renk ısınır.
    // Zincir bitince kısa tutulup söner. Metin sadece sayı değişince yazılır.
    public class ComboPopup : MonoBehaviour
    {
        private const float ChainWindow = 0.75f; // bu kadar sn içinde yeni öldürme gelmezse zincir biter
        private const int MinCombo = 3;
        private const float Pop = 0.16f, HoldAfter = 0.55f, FadeTime = 0.35f;
        private const float BaseSize = 60f, SubSize = 30f;

        private static readonly string[] Names =
            { "", "", "", "ÜÇLÜ", "DÖRTLÜ", "BEŞLİ", "ALTILI", "YEDİLİ", "SEKİZLİ", "DOKUZLU" };
        private static readonly Color ShadowColor = new Color(0.05f, 0.05f, 0.17f, 0.85f);
        private static readonly Vector2 ShadowOffset = new Vector2(0f, -5f);

        private GameObject _hud;
        private RectTransform _root;
        private TMP_Text _title, _shadow, _sub;
        private int _count, _shown;
        private float _lastKill = -10f;
        private float _popTime = -1f, _endTime = -1f;
        private float _punch = 1.3f;

        internal void Init(GameObject hud, RectTransform layer)
        {
            _hud = hud;
            var safeGo = new GameObject("ComboSafeArea", typeof(RectTransform));
            safeGo.layer = layer.gameObject.layer;
            var safe = (RectTransform)safeGo.transform;
            safe.SetParent(layer, false);
            HudFeedbackLayer.Stretch(safe);
            safeGo.AddComponent<SafeAreaHandler>();

            var go = new GameObject("ComboPopup", typeof(RectTransform));
            go.layer = safeGo.layer;
            _root = (RectTransform)go.transform;
            _root.SetParent(safe, false);
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.6f); // blob'un biraz üstü, olay bandının altı
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.sizeDelta = new Vector2(800f, 120f);
            _root.localRotation = Quaternion.Euler(0f, 0f, 5f);

            _shadow = MakeText("Shadow", ShadowOffset, BaseSize, FontStyles.Bold | FontStyles.Italic, ShadowColor);
            _title = MakeText("Title", Vector2.zero, BaseSize, FontStyles.Bold | FontStyles.Italic, Color.white);
            _sub = MakeText("Sub", new Vector2(0f, -52f), SubSize, FontStyles.Bold, Color.white);
            if (UiKit.Ready)
            {
                // Kit: kalın konturlu Baloo; renkler ShowCount'ta yazılır. Ayrı gölge yazısı gerekmez.
                UiKit.Outlined(_title, Color.white, true);
                _title.fontStyle = FontStyles.Italic;
                UiKit.Outlined(_sub, Color.white);
                _shadow.gameObject.SetActive(false);
            }
            go.SetActive(false);
        }

        private TMP_Text MakeText(string name, Vector2 pos, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = _root.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(_root, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(800f, size * 1.4f);
            var text = go.GetComponent<TMP_Text>();
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += OnEnemyKilled;
            GameEvents.OnCharacterSelected += OnRunStarted;
            GameEvents.OnGameOver += Hide;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= OnEnemyKilled;
            GameEvents.OnCharacterSelected -= OnRunStarted;
            GameEvents.OnGameOver -= Hide;
        }

        private void OnRunStarted(CharacterData _) => Hide();

        private void OnEnemyKilled(EnemyData _)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            float now = Time.time;
            if (now - _lastKill <= ChainWindow) _count++;
            else { _count = 1; _shown = 0; } // yeni zincir
            _lastKill = now;
        }

        private void Hide()
        {
            _count = 0;
            _shown = 0;
            _popTime = -1f;
            _endTime = -1f;
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_root == null) return;
            GameManager gm = GameManager.Instance;
            if ((_hud != null && !_hud.activeInHierarchy) || (gm != null && gm.CurrentState == GameState.Menu))
            {
                if (_root.gameObject.activeSelf) Hide();
                return;
            }
            if (gm != null && gm.CurrentState != GameState.Playing) return; // kart ekranı/duraklatmada donar

            // Aynı karede gelen öldürmeler (bir sopa savuruşu) tek zıplama olsun diye sayı Update'te gösterilir.
            if (_count >= MinCombo && _count != _shown) ShowCount(_count);

            if (_popTime < 0f) return;
            float now = Time.time;
            if (_endTime < 0f && now - _lastKill > ChainWindow) { _endTime = now; _count = 0; }
            float t = now - _popTime;
            float scale = t < Pop ? Mathf.Lerp(_punch, 1f, t / Pop) : 1f;
            float alpha = 1f;
            if (_endTime >= 0f)
            {
                float e = now - _endTime - HoldAfter;
                if (e >= FadeTime) { Hide(); return; }
                if (e > 0f) alpha = 1f - e / FadeTime;
            }
            float grow = 1f + Mathf.Clamp((_shown - MinCombo) * 0.03f, 0f, 0.35f); // büyük kombo daha iri
            _root.localScale = new Vector3(scale * grow, scale * grow, 1f);
            _title.alpha = alpha;
            _shadow.alpha = alpha * ShadowColor.a;
            _sub.alpha = alpha;
        }

        private void ShowCount(int n)
        {
            int prev = _shown;
            _shown = n;
            string title = n < Names.Length ? Names[n] + " KOMBO!" : "x" + n + " KOMBO!";
            _title.text = title;
            _shadow.text = title;
            Color c = ColorFor(n);
            _title.color = c;
            string sub = n >= 50 ? "EFSANE!" : n >= 25 ? "DURDURULAMAZ!" : n >= 10 ? "MUHTEŞEM!" : "";
            _sub.gameObject.SetActive(sub.Length > 0);
            if (sub.Length > 0) { _sub.text = sub; _sub.color = c; }
            _punch = n - prev >= 2 ? 1.6f : 1.35f; // tek savuruşta çok öldürme daha sert zıplar
            _popTime = Time.time;
            _endTime = -1f;
            _root.gameObject.SetActive(true);

            // Titreşim sadece eşiklerde (her öldürmede değil).
            if (prev < 10 && n >= 10) Haptics.Impact(2);
            else if (prev < 5 && n >= 5) Haptics.Impact(1);
            else if (prev < MinCombo) Haptics.Impact(0);
        }

        // Sarı → turuncu → kırmızı → mor: sayı büyüdükçe ısınır.
        private static Color ColorFor(int n)
        {
            if (n >= 25) return new Color(0.85f, 0.45f, 1f);
            if (n >= 10) return new Color(1f, 0.35f, 0.35f);
            if (n >= 5) return new Color(1f, 0.6f, 0.2f);
            return new Color(1f, 0.9f, 0.3f);
        }
    }
}
