using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BlobSurvivor.UI
{
    // Sadece uygulama ilk açılırken (RuntimeInitializeOnLoad, tek sahne: bir kez); Ana Menü/restart'ta gelmez.
    // Soğuk açılışta Zorse Studio girişi: Resources/UI/zorse-intro (3 sn'lik dikey video, sesli) oynar, biter, söner;
    // arkada giriş ekranı/şehir bu sırada hazırlanır. Kendi Canvas'ı en üstte (TitleScreen'i örter).
    // Video yoksa ya da açılamazsa eski hâl: Resources/UI/zorse-logo (Sprite) siyahta belirip söner.
    // Sahne kurulumu yok; sahne yüklenince kendini kurar, bitince yok olur.
    public class StudioSplash : MonoBehaviour
    {
        private const float FadeIn = 0.4f, Hold = 1.3f, FadeOut = 0.5f;
        private const float PrepareTimeout = 3f;   // video bu sürede hazırlanmazsa atlanır
        private const float MaxVideoTime = 6f;     // bitiş olayı gelmezse güvenlik
        // Videonun zemin rengi: ekran videodan uzunsa kenarlar aynı renkte kalsın.
        private static readonly Color VideoBackground = new Color32(24, 25, 24, 255);

        private CanvasGroup _group, _all;
        private float _start = -1f;

        private VideoPlayer _video;
        private RenderTexture _texture;
        private bool _videoMode, _videoStarted, _videoDone;
        private float _fadeStart = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            VideoClip clip = Resources.Load<VideoClip>("UI/zorse-intro");
            Sprite logo = clip == null ? Resources.Load<Sprite>("UI/zorse-logo") : null;
            if (clip == null && logo == null) return;

            var go = new GameObject("StudioSplash", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;

            var back = new GameObject("Back", typeof(RectTransform), typeof(Image));
            var br = (RectTransform)back.transform;
            br.SetParent(go.transform, false);
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero;
            back.GetComponent<Image>().color = clip != null ? VideoBackground : Color.black; // açılıştan itibaren tıklamayı da tutar

            var splash = go.AddComponent<StudioSplash>();
            if (clip != null) splash.SetupVideo(clip);
            else splash.SetupLogo(logo);
        }

        private void Awake() => _all = gameObject.AddComponent<CanvasGroup>();

        private void SetupLogo(Sprite logo)
        {
            var logoGo = new GameObject("Logo", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            var lr = (RectTransform)logoGo.transform;
            lr.SetParent(transform, false);
            lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(0.5f, 0.5f);
            float width = 1080f * 0.66f;
            lr.sizeDelta = new Vector2(width, width * logo.rect.height / logo.rect.width);
            var img = logoGo.GetComponent<Image>();
            img.sprite = logo;
            img.preserveAspect = true;
            img.raycastTarget = false;
            _group = logoGo.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
        }

        // Video tam genişlikte, oranı korunur (üst/alt boşluk zemin rengiyle dolar). RenderTexture video bitince bırakılır.
        private void SetupVideo(VideoClip clip)
        {
            _videoMode = true;
            int w = (int)clip.width, h = (int)clip.height;
            _texture = new RenderTexture(w, h, 0) { name = "StudioSplashVideo" };

            var imgGo = new GameObject("Video", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            var ir = (RectTransform)imgGo.transform;
            ir.SetParent(transform, false);
            ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one; ir.offsetMin = ir.offsetMax = Vector2.zero;
            var raw = imgGo.GetComponent<RawImage>();
            raw.texture = _texture;
            raw.raycastTarget = false;
            raw.color = new Color(1f, 1f, 1f, 0f); // ilk kare gelene kadar gizli (boş doku görünmesin)
            var fitter = imgGo.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = h > 0 ? w / (float)h : 9f / 16f;

            _video = gameObject.AddComponent<VideoPlayer>();
            _video.playOnAwake = false;
            _video.isLooping = false;
            _video.source = VideoSource.VideoClip;
            _video.clip = clip;
            _video.renderMode = VideoRenderMode.RenderTexture;
            _video.targetTexture = _texture;
            _video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            _video.audioOutputMode = VideoAudioOutputMode.Direct;
            _video.skipOnDrop = true;
            _video.prepareCompleted += p => { p.Play(); };
            _video.started += _ => { _videoStarted = true; raw.color = Color.white; };
            _video.loopPointReached += _ => _videoDone = true;
            _video.errorReceived += (_, msg) => { Debug.LogWarning($"[StudioSplash] Video oynatılamadı: {msg}"); _videoDone = true; };
            _video.Prepare();
        }

        private void Update()
        {
            // İlk karede saymaya başla (sahne yükleme süresi girişi yemesin). Ölçeksiz: timeScale'den bağımsız.
            if (_start < 0f) _start = Time.unscaledTime;
            float t = Time.unscaledTime - _start;

            if (_videoMode)
            {
                bool timedOut = (!_videoStarted && t > PrepareTimeout) || t > MaxVideoTime;
                if (_fadeStart < 0f && (_videoDone || timedOut)) _fadeStart = Time.unscaledTime;
                if (_fadeStart >= 0f) FadeAll((Time.unscaledTime - _fadeStart) / FadeOut);
                return;
            }

            if (t < FadeIn) { _group.alpha = t / FadeIn; return; }
            if (t < FadeIn + Hold) { _group.alpha = 1f; return; }
            FadeAll((t - FadeIn - Hold) / FadeOut);
        }

        // Giriş ve zemin birlikte söner: alttan giriş ekranı belirir. Tek seferlik: bitince yok olur.
        private void FadeAll(float k)
        {
            if (k >= 1f) { Destroy(gameObject); return; }
            _all.alpha = 1f - k;
        }

        private void OnDestroy()
        {
            if (_video != null) _video.Stop();
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }
        }
    }
}
