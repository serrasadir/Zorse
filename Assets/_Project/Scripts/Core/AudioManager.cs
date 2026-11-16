using UnityEngine;

namespace BlobSurvivor.Core
{
    // Ses: arka plan döngüsü (şimdilik yağmur; müzik gelince aynı kanal) + efekt kaynakları havuzu.
    // Kendiliğinden kurulur (sahne objesi yok). Seviyeler GameSettings'ten (Müzik / Efekt), değişince anında uygulanır.
    // Açılış videosu oynarken (StudioSplash) arka plan bekler, video bitince yavaşça açılır.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string AmbiencePath = "Audio/Ambience/RainLoop";
        private const float AmbienceLevel = 0.6f;   // tam müzik sesinde yağmurun seviyesi
        private const float FadeInTime = 2.5f;
        private const int SfxVoices = 8;

        private AudioSource _ambience;
        private AudioSource[] _sfx;
        private int _nextSfx;
        private float _fade;       // 0→1, açılış sonrası
        private int _holds;        // açılış ekranı gibi bekletenler

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<AudioManager>();
        }

        private void Awake()
        {
            _ambience = gameObject.AddComponent<AudioSource>();
            _ambience.clip = Resources.Load<AudioClip>(AmbiencePath);
            _ambience.loop = true;
            _ambience.playOnAwake = false;
            _ambience.spatialBlend = 0f;
            _ambience.priority = 0;

            _sfx = new AudioSource[SfxVoices];
            for (int i = 0; i < SfxVoices; i++)
            {
                AudioSource s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                _sfx[i] = s;
            }
            GameSettings.Changed += ApplyVolumes;
        }

        private void OnDestroy() => GameSettings.Changed -= ApplyVolumes;

        // Açılış ekranı başında Hold, bitince Release çağırır.
        public void Hold() => _holds++;
        public void Release() => _holds = Mathf.Max(0, _holds - 1);

        private void Update()
        {
            if (_holds > 0 || _ambience.clip == null || _fade >= 1f) return;
            if (!_ambience.isPlaying) _ambience.Play();
            _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime / FadeInTime);
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            _ambience.volume = AmbienceLevel * GameSettings.MusicVolume * _fade * _fade;
        }

        // Kısa efekt: sıradaki boş (yoksa en eski) kaynaktan çalar. Ses Efekt ayarıyla ölçeklenir.
        public void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            float v = volume * GameSettings.SfxVolume;
            if (clip == null || v <= 0.001f) return;
            AudioSource s = _sfx[_nextSfx];
            for (int i = 0; i < SfxVoices; i++)
            {
                AudioSource c = _sfx[(_nextSfx + i) % SfxVoices];
                if (!c.isPlaying) { s = c; _nextSfx = (_nextSfx + i) % SfxVoices; break; }
            }
            _nextSfx = (_nextSfx + 1) % SfxVoices;
            s.pitch = pitch;
            s.PlayOneShot(clip, v);
        }
    }
}
