using UnityEngine;
using UnityEngine.UI;

namespace BlobSurvivor.UI
{
    // Sadece uygulama ilk açılırken (RuntimeInitializeOnLoad, tek sahne: bir kez); Ana Menü/restart'ta gelmez.
    // Soğuk açılışta Zorse Studio logosu (chameleon-jump'taki gibi): siyah ekranda logo belirir, en az ~2.2 sn
    // kalır, söner; arkada giriş ekranı/şehir bu sırada hazırlanır. Kendi Canvas'ı en üstte (TitleScreen'i örter).
    // Logo: Resources/UI/zorse-logo (Sprite). Sahne kurulumu yok; sahne yüklenince kendini kurar, bitince yok olur.
    public class StudioSplash : MonoBehaviour
    {
        private const float FadeIn = 0.4f, Hold = 1.3f, FadeOut = 0.5f;
        private CanvasGroup _group, _all;
        private float _start = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            Sprite logo = Resources.Load<Sprite>("UI/zorse-logo");
            if (logo == null) return;

            var go = new GameObject("StudioSplash", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;

            var black = new GameObject("Black", typeof(RectTransform), typeof(Image));
            var br = (RectTransform)black.transform;
            br.SetParent(go.transform, false);
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            black.GetComponent<Image>().color = Color.black; // açılıştan itibaren tıklamayı da tutar

            var logoGo = new GameObject("Logo", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var lr = (RectTransform)logoGo.transform;
            lr.SetParent(go.transform, false);
            lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0.5f, 0.5f);
            float width = 1080f * 0.66f;
            lr.sizeDelta = new Vector2(width, width * logo.rect.height / logo.rect.width);
            var img = logoGo.GetComponent<Image>();
            img.sprite = logo;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var splash = go.AddComponent<StudioSplash>();
            splash._group = logoGo.GetComponent<CanvasGroup>();
            splash._group.alpha = 0f;
        }

        private void Update()
        {
            // İlk karede saymaya başla (sahne yükleme süresi logoyu yemesin). Ölçeksiz: timeScale'den bağımsız.
            if (_start < 0f) _start = Time.unscaledTime;
            float t = Time.unscaledTime - _start;
            if (t < FadeIn) { _group.alpha = t / FadeIn; return; }
            if (t < FadeIn + Hold) { _group.alpha = 1f; return; }
            float k = (t - FadeIn - Hold) / FadeOut;
            if (k >= 1f) { Destroy(gameObject); return; } // tek seferlik: açılış bitti
            // Logo ve siyah birlikte söner: alttan giriş ekranı belirir.
            _all.alpha = 1f - k;
        }

        private void Awake() => _all = gameObject.AddComponent<CanvasGroup>();
    }
}
