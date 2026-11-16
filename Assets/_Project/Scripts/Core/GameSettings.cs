using System;
using UnityEngine;

namespace BlobSurvivor.Core
{
    // Oyuncu ayarları (Ayarlar ekranı): müzik ve efekt sesi (0-1), titreşim. Cihazda PlayerPrefs'te; değişince Changed.
    public static class GameSettings
    {
        private const string MusicKey = "settings_music", SfxKey = "settings_sfx", VibrationKey = "settings_vibration";

        public static event Action Changed;

        private static bool _loaded;
        private static float _music = 1f, _sfx = 1f;
        private static bool _vibration = true;

        public static float MusicVolume { get { Load(); return _music; } set => Set(ref _music, Mathf.Clamp01(value), MusicKey); }
        public static float SfxVolume { get { Load(); return _sfx; } set => Set(ref _sfx, Mathf.Clamp01(value), SfxKey); }

        public static bool Vibration
        {
            get { Load(); return _vibration; }
            set
            {
                Load();
                if (_vibration == value) return;
                _vibration = value;
                PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _music = PlayerPrefs.GetFloat(MusicKey, 1f);
            _sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
            _vibration = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
        }

        // Sürgü sürüklenirken her karede gelir: kayıt diske ayarlar ekranı kapanınca (Save) yazılır.
        private static void Set(ref float field, float value, string key)
        {
            Load();
            if (Mathf.Approximately(field, value)) return;
            field = value;
            PlayerPrefs.SetFloat(key, value);
            Changed?.Invoke();
        }

        public static void Save() => PlayerPrefs.Save();
    }
}
