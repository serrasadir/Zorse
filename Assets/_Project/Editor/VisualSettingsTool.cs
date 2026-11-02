using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlobSurvivor.EditorTools
{
    // Blender'daki görünüme yaklaşmak için sahnenin "çıplak" URP ayarlarını tamamlar (2026-11-01, Bahar):
    // tonemapping + renk ayarı (Volume), FXAA, gradient ortam ışığı, güneş rengi/gölgesi.
    // Hepsi mobilde ucuz: tek uber post pass + FXAA; bloom/SSAO/MSAA bilinçli olarak yok.
    public static class VisualSettingsTool
    {
        private const string ProfilePath = "Assets/_Project/Settings/PP_City.asset";
        private const string VolumeName = "GlobalVolume";

        // Gotik palet (CLAUDE.md Sanat Yönü) ile uyumlu, şehir paletinin pastel tonlarını boğmayan değerler.
        private static readonly Color AmbientSky = new Color(0.78f, 0.80f, 0.95f);
        private static readonly Color AmbientEquator = new Color(0.62f, 0.57f, 0.66f);
        private static readonly Color AmbientGround = new Color(0.27f, 0.21f, 0.32f);
        private static readonly Color SunColor = new Color(1f, 0.94f, 0.84f);
        private const float SunIntensity = 1.25f;
        private const float ShadowStrength = 0.75f; // tam siyah gölge düz renkli low-poly'de kirli görünüyor

        [MenuItem("Tools/Blob.io/Görsel Ayarları Uygula")]
        private static void Apply()
        {
            bool camera = SetupCamera();
            bool volume = SetupVolume();
            SetupAmbient();
            bool sun = SetupSun();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log($"[VisualSettingsTool] Uygulandı — kamera: {camera}, volume: {volume}, ortam ışığı: true, güneş: {sun}. " +
                      "Sahneyi kaydet (Ctrl+S). Profil: " + ProfilePath);
        }

        private static bool SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[VisualSettingsTool] MainCamera tag'li kamera bulunamadı.");
                return false;
            }

            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            Undo.RecordObject(data, "Görsel Ayarlar");
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing; // MSAA'dan çok daha ucuz
            EditorUtility.SetDirty(data);
            return true;
        }

        private static bool SetupVolume()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            // Neutral: ACES pastel paleti fazla koyulaştırıp turuncuya çekiyor; Neutral renkleri korur.
            Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
            color.postExposure.Override(0.25f); // Neutral tonemapping görüntüyü biraz karartır
            color.contrast.Override(12f);
            color.saturation.Override(18f);

            Vignette vignette = GetOrAdd<Vignette>(profile);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);
            EditorUtility.SetDirty(profile);

            GameObject go = GameObject.Find(VolumeName);
            if (go == null)
            {
                go = new GameObject(VolumeName);
                Undo.RegisterCreatedObjectUndo(go, "Görsel Ayarlar");
            }

            Volume volume = go.GetComponent<Volume>();
            if (volume == null) volume = Undo.AddComponent<Volume>(go);
            Undo.RecordObject(volume, "Görsel Ayarlar");
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = profile;
            return true;
        }

        // VolumeProfile bileşenleri profile asset'inin alt asset'i olarak kaydedilmezse yeniden açılışta kaybolur.
        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T existing)) return existing;

            T component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        // Varsayılan skybox ortamı gölgedeki yüzleri griye boğuyor; üç renkli gradient low-poly şehirde
        // yönleri ayırt ettirir (çatı açık, cephe orta, alt yüzler koyu) ve bake gerektirmez.
        private static void SetupAmbient()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky;
            RenderSettings.ambientEquatorColor = AmbientEquator;
            RenderSettings.ambientGroundColor = AmbientGround;
            DynamicGI.UpdateEnvironment();
        }

        private static bool SetupSun()
        {
            Light sun = null;
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                sun = light;
                break;
            }

            if (sun == null)
            {
                Debug.LogWarning("[VisualSettingsTool] Directional Light bulunamadı.");
                return false;
            }

            Undo.RecordObject(sun, "Görsel Ayarlar");
            sun.color = SunColor;
            sun.intensity = SunIntensity;
            sun.shadows = LightShadows.Soft; // Mobile_RPAsset soft shadow'u desteklemiyor, orada sert kalır
            sun.shadowStrength = ShadowStrength;
            return true;
        }
    }
}
