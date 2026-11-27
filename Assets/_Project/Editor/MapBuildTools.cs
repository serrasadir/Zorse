using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Harita kurulumlarının ortak araçları: bölge kapısı tabelası (Crownreach), nötr gün ışığı (şehir), temas gölgesi
    // (şehir binaları), küçük harita çekimi (Crownreach).
    public static class MapBuildTools
    {
        // Kapı üstünde boy işareti (dünya yazısı, kameraya dönük; ZoneGate günceller).
        internal static void AddGateSign(GameObject gate, int zone)
        {
            var sign = new GameObject("GateSign");
            sign.transform.SetParent(gate.transform, false);
            float top = 0f;
            foreach (Renderer r in gate.GetComponentsInChildren<Renderer>()) top = Mathf.Max(top, r.bounds.max.y);
            sign.transform.position = new Vector3(gate.transform.position.x, top + 0.6f, gate.transform.position.z - 1.2f); // kapının önünde, üstünde
            sign.transform.localScale = Vector3.one / Mathf.Max(0.01f, gate.transform.lossyScale.x);
            var tmp = sign.AddComponent<TMPro.TextMeshPro>();
            tmp.text = "BOY";
            tmp.fontSize = 6f;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(12f, 3f);
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(30, 20, 10, 255);
            var zg = gate.AddComponent<BlobSurvivor.UI.ZoneGate>();
            zg.EditorSetup(zone, tmp);
        }

        // Şehir de aynı sıcak ışığı kullanır (CitySoftSetup), kendi profil kopyasıyla.
        // Nötr gün ışığı (Bahar, 2026-11-21: "sarı ışık filtresini kaldır"): güneş ve ortam beyaza yakın, beyaz dengesi 0.
        internal static void ApplyWarmLighting(string profilePath)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.94f, 0.96f, 1.0f);
            RenderSettings.ambientEquatorColor = new Color(0.78f, 0.79f, 0.80f);
            RenderSettings.ambientGroundColor = new Color(0.46f, 0.47f, 0.52f);
            DynamicGI.UpdateEnvironment();

            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            {
                if (light.type != LightType.Directional) continue;
                light.color = new Color(1f, 0.98f, 0.95f);
                light.intensity = 1.15f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.55f; // yumuşak, renkli zeminde kirli durmasın
                Vector3 e = light.transform.eulerAngles;
                light.transform.rotation = Quaternion.Euler(48f, e.y, 0f); // biraz alçak: gölgeler uzun ve okunur
                break;
            }

            GameObject go = GameObject.Find("GlobalVolume");
            var volume = go != null ? go.GetComponent<UnityEngine.Rendering.Volume>() : null;
            if (volume == null || volume.sharedProfile == null) return;
            string cityPath = AssetDatabase.GetAssetPath(volume.sharedProfile);
            if (cityPath != profilePath)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath) == null)
                    AssetDatabase.CopyAsset(cityPath, profilePath);
                volume.sharedProfile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath);
            }
            var profile = volume.sharedProfile;
            if (profile.TryGet(out UnityEngine.Rendering.Universal.ColorAdjustments color))
            {
                color.postExposure.Override(0.3f);
                color.contrast.Override(8f);
                color.saturation.Override(12f);
            }
            if (!profile.TryGet(out UnityEngine.Rendering.Universal.WhiteBalance white))
            {
                white = profile.Add<UnityEngine.Rendering.Universal.WhiteBalance>(true);
                AssetDatabase.AddObjectToAsset(white, profile);
            }
            white.temperature.Override(0f);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        // ---------- temas gölgesi ----------
        // Lightmap bake yerine (yenen binanın gölgesi yerde kalırdı): her bina/eşyanın altında yumuşak kenarlı koyu leke,
        // nesnenin child'ı olduğu için yenince o da kaybolur. Tek doku + tek saydam materyal, statik birleşir.
        private const string ShadowTexPath = "Assets/_Project/Art/FX/ContactShadow.png"; // Art/City dışında: oradaki ön işlemci alfayı siler
        private const string ShadowMatPath = CityKitPipeline.MedievalRoot + "/Mat_ContactShadow.mat";
        private const float ShadowY = 0.08f;         // zemin düzlemlerinin (en fazla ~0.06) üstünde
        private const float ShadowGrow = 1.12f; // koyu çekirdek ayak izi kadar, dışa doğru yumuşak sönüm (pay boyla orantılı)
        private static Material s_shadowMat;

        internal static void AddContactShadow(GameObject prefab)
        {
            EnsureShadowAssets();
            if (s_shadowMat == null) return;
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform old = root.transform.Find("ContactShadow");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                BoxCollider body = root.GetComponent<BoxCollider>();
                if (body == null) return;
                Bounds b = new Bounds(body.center, body.size);
                foreach (BoxCollider extra in root.GetComponents<BoxCollider>()) // fare delikli parça: iki kutu
                    b.Encapsulate(new Bounds(extra.center, extra.size));
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad); // Unity'nin dörtgeni (helikopter gölgesi gibi)
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.name = "ContactShadow";
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(b.center.x, ShadowY, b.center.z);
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // yere yatık, yüzü yukarı
                float pad = Mathf.Clamp(0.6f * Mathf.Min(b.size.x, b.size.z), 0.3f, 1.6f);
                go.transform.localScale = new Vector3(b.size.x * ShadowGrow + pad, b.size.z * ShadowGrow + pad, 1f);
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = s_shadowMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureShadowAssets()
        {
            if (s_shadowMat != null) return;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(ShadowTexPath) == null)
            {
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        // Yuvarlatılmış kare: kenara doğru yumuşak sönüm.
                        float u = Mathf.Abs((x + 0.5f) / n * 2f - 1f), v = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                        float d = Mathf.Pow(Mathf.Pow(u, 4f) + Mathf.Pow(v, 4f), 0.25f);
                        float a = 1f - Mathf.SmoothStep(0.55f, 1f, d);
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
                    }
                System.IO.File.WriteAllBytes(ShadowTexPath, tex.EncodeToPNG()); // eğri değişirse dosyayı sil, yeniden üretilir
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(ShadowTexPath);
                if (AssetImporter.GetAtPath(ShadowTexPath) is TextureImporter ti)
                {
                    ti.alphaIsTransparency = true;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.mipmapEnabled = false;
                    ti.filterMode = FilterMode.Bilinear; // Art/City ön işlemcisi Point yapar: gradyan basamaklanmasın
                    ti.SaveAndReimport();
                }
            }
            s_shadowMat = AssetDatabase.LoadAssetAtPath<Material>(ShadowMatPath);
            if (s_shadowMat == null)
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlit == null) return;
                s_shadowMat = new Material(unlit) { name = "Mat_ContactShadow" };
                s_shadowMat.SetFloat("_Surface", 1f);          // Transparent
                s_shadowMat.SetFloat("_Blend", 0f);            // Alpha
                s_shadowMat.SetFloat("_ZWrite", 0f);
                s_shadowMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                s_shadowMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                s_shadowMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                s_shadowMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 50;
                s_shadowMat.SetOverrideTag("RenderType", "Transparent");
                AssetDatabase.CreateAsset(s_shadowMat, ShadowMatPath);
            }
            // Saydamlık her seferinde baştan (URP materyal arayüzünün yaptığı kurulum; kodla açınca kendiliğinden olmuyor).
            s_shadowMat.SetFloat("_Surface", 1f);
            s_shadowMat.SetFloat("_Blend", 0f);
            s_shadowMat.SetFloat("_ZWrite", 0f);
            s_shadowMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            s_shadowMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            s_shadowMat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            s_shadowMat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            s_shadowMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            s_shadowMat.SetOverrideTag("RenderType", "Transparent");
            s_shadowMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 50;
            s_shadowMat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ShadowTexPath));
            s_shadowMat.SetColor("_BaseColor", new Color(0.1f, 0.08f, 0.14f, 0.6f)); // mor-siyah: sıcak sahnede yumuşak
            EditorUtility.SetDirty(s_shadowMat);

        }

        internal static void RenderMinimap(Rect world, string path)
        {
            const int width = 256;
            int height = Mathf.RoundToInt(width * world.height / world.width);
            var camGo = new GameObject("MinimapCam");
            var rt = new RenderTexture(width, height, 24);
            try
            {
                Camera cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = world.height / 2f;
                cam.aspect = world.width / world.height;
                cam.transform.position = new Vector3(world.center.x, 80f, world.center.y);
                cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.2f, 0.35f, 0.2f);
                cam.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
                cam.nearClipPlane = 1f;
                cam.farClipPlane = 200f;
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                RenderTexture.active = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camGo);
            }
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }
    }
}
