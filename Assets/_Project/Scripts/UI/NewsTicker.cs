using TMPro;
using UnityEngine;
using BlobSurvivor.Core;
using BlobSurvivor.Data;

namespace BlobSurvivor.UI
{
    // 2026-11-03 (Serra, Plague Inc. haber bandı): üstte ince "HABER" bandı. İlk haberle belirir, run boyunca ekranda
    // kalır; başlık 30 saniyede bir yumuşak geçişle değişir. Blob büyüdükçe blob'la ilgili haber oranı artar (oranlar
    // NewsHeadlines asset'inde). Blob yeni bir tier'a geçince birkaç saniye içinde o tier'dan bir haber öne alınır.
    // Bandı aşan uzun başlık döngüyle kayar (bekle → kay → sonda bekle → başa dön).
    // Maliyet: başlık başına bir TMP metin güncellemesi; kayma/geçiş sadece pozisyon ve renk alfası.
    public class NewsTicker : MonoBehaviour
    {
        [SerializeField] private NewsHeadlines _headlines;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private TextMeshProUGUI _text;

        [Header("Zamanlama (oyun saniyesi — duraklatma ve kart seçiminde durur)")]
        [SerializeField] private float _firstNewsDelay = 8f;
        [SerializeField] private float _newsInterval = 30f;
        [SerializeField] private float _tierNewsDelay = 2.5f;

        [Header("Görünüm")]
        [SerializeField] private float _fadeDuration = 0.35f;
        [SerializeField] private float _scrollStartDelay = 1.5f;
        [SerializeField] private float _scrollSpeed = 110f;
        [SerializeField] private float _scrollEndHold = 2f;

        private bool _running;
        private bool _barVisible;
        private float _nextNewsTimer;
        private float _scrollTime;
        private float _scrollDistance;
        // Başlık geçişi: önce eski metin söner, sonra yenisi belirir. -1 = geçiş yok.
        private float _swapTime = -1f;
        private string _pendingHeadline;
        private BlobTier _currentTier = BlobTier.Tiny;
        private BlobTier _lastAnnouncedTier = BlobTier.Tiny;
        private bool _tierNewsPending;

        // Aynı başlık, listedeki diğerleri gösterilmeden tekrar gelmesin.
        private readonly ShuffleBag _worldBag = new ShuffleBag();
        private readonly ShuffleBag _blobBag = new ShuffleBag();

        private void Awake() => SetBarAlpha(0f);

        private void OnEnable()
        {
            GameEvents.OnCharacterSelected += HandleRunStarted;
            GameEvents.OnBlobTierChanged += HandleTierChanged;
            GameEvents.OnGameOver += HandleRunEnded;
            GameEvents.OnRunComplete += HandleRunCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnCharacterSelected -= HandleRunStarted;
            GameEvents.OnBlobTierChanged -= HandleTierChanged;
            GameEvents.OnGameOver -= HandleRunEnded;
            GameEvents.OnRunComplete -= HandleRunCompleted;
        }

        private void HandleRunStarted(CharacterData _)
        {
            _running = true;
            _nextNewsTimer = _firstNewsDelay;
            _currentTier = BlobTier.Tiny;
            _lastAnnouncedTier = BlobTier.Tiny;
            _tierNewsPending = false;
            Hide();
        }

        private void HandleRunEnded()
        {
            _running = false;
            Hide();
        }

        private void HandleRunCompleted(RunEndReason _) => HandleRunEnded();

        private void HandleTierChanged(BlobTier tier)
        {
            _currentTier = tier;
            if (!_running || tier <= _lastAnnouncedTier) return;

            _lastAnnouncedTier = tier;
            _tierNewsPending = true;
            _nextNewsTimer = Mathf.Min(_nextNewsTimer, _tierNewsDelay);
        }

        private void Update()
        {
            if (!_running || _headlines == null) return;

            float dt = Time.deltaTime;
            if (_barVisible)
            {
                if (_group != null && _group.alpha < 1f) SetBarAlpha(_group.alpha + dt / _fadeDuration);
                UpdateSwap(dt);
                UpdateScroll(dt);
            }

            _nextNewsTimer -= dt;
            if (_nextNewsTimer > 0f || _swapTime >= 0f) return;

            _nextNewsTimer = _newsInterval;
            string headline = PickHeadline();
            if (string.IsNullOrEmpty(headline)) return;

            if (!_barVisible)
            {
                ApplyHeadline(headline);
                _barVisible = true; // bant bir kere belirir, run sonuna kadar kalır
            }
            else
            {
                _pendingHeadline = headline;
                _swapTime = 0f;
            }
        }

        private string PickHeadline()
        {
            bool forcedBlob = _tierNewsPending;
            _tierNewsPending = false;

            if (forcedBlob || Random.value < _headlines.BlobNewsChance(_currentTier))
            {
                string[] blobNews = _headlines.BlobNewsFor(_currentTier);
                if (blobNews != null) return _blobBag.Next(blobNews); // tier değişince torba yeni listeye geçer
            }
            return _worldBag.Next(_headlines.WorldNews);
        }

        private void UpdateSwap(float dt)
        {
            if (_swapTime < 0f) return;

            _swapTime += dt;
            if (_pendingHeadline != null)
            {
                _text.alpha = 1f - _swapTime / _fadeDuration;
                if (_swapTime < _fadeDuration) return;
                ApplyHeadline(_pendingHeadline);
                _pendingHeadline = null;
                _swapTime = 0f;
            }

            _text.alpha = _swapTime / _fadeDuration;
            if (_swapTime >= _fadeDuration)
            {
                _text.alpha = 1f;
                _swapTime = -1f;
            }
        }

        private void ApplyHeadline(string headline)
        {
            _text.text = headline;
            float textWidth = _text.preferredWidth;
            RectTransform textRect = _text.rectTransform;
            textRect.sizeDelta = new Vector2(textWidth, textRect.sizeDelta.y);
            textRect.anchoredPosition = new Vector2(0f, textRect.anchoredPosition.y);
            _scrollDistance = Mathf.Max(0f, textWidth - _viewport.rect.width);
            _scrollTime = 0f;
        }

        private void UpdateScroll(float dt)
        {
            if (_scrollDistance <= 0f || _swapTime >= 0f) return;

            _scrollTime += dt;
            float cycle = _scrollStartDelay + _scrollDistance / _scrollSpeed + _scrollEndHold;
            float t = _scrollTime % cycle;
            float x = Mathf.Clamp((t - _scrollStartDelay) * _scrollSpeed, 0f, _scrollDistance);
            RectTransform textRect = _text.rectTransform;
            textRect.anchoredPosition = new Vector2(-x, textRect.anchoredPosition.y);
        }

        private void Hide()
        {
            _barVisible = false;
            _swapTime = -1f;
            _pendingHeadline = null;
            if (_text != null) _text.alpha = 1f;
            SetBarAlpha(0f);
        }

        private void SetBarAlpha(float alpha)
        {
            if (_group != null) _group.alpha = Mathf.Clamp01(alpha);
        }

        // Liste karıştırılıp sırayla dağıtılır; bitince yeniden karışır. Liste değişirse baştan kurulur.
        private class ShuffleBag
        {
            private string[] _source;
            private int[] _order;
            private int _index;

            public string Next(string[] source)
            {
                if (source == null || source.Length == 0) return null;

                if (_source != source)
                {
                    _source = source;
                    _order = new int[source.Length];
                    for (int i = 0; i < _order.Length; i++) _order[i] = i;
                    _index = _order.Length; // ilk çağrıda karıştır
                }

                if (_index >= _order.Length)
                {
                    for (int i = _order.Length - 1; i > 0; i--)
                    {
                        int j = Random.Range(0, i + 1);
                        (_order[i], _order[j]) = (_order[j], _order[i]);
                    }
                    _index = 0;
                }
                return _source[_order[_index++]];
            }
        }
    }
}
