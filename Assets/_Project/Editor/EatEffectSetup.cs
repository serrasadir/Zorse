using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Entities.Vehicles;

namespace BlobSurvivor.EditorTools
{
    // Yeme efekti kurulumu: kırıntı materyali + Resources/FX/EatCrumbs prefab'ı, ve her yenebilirin kırıntı
    // renkleri (modelin palet renklerinden alana göre en çok kullanılan iki renk). GameScene açıkken; sonra Cmd+S.
    internal static class EatEffectSetup
    {
        private const string FxFolder = "Assets/_Project/Resources/FX";
        private const string MaterialPath = FxFolder + "/Mat_EatCrumbs.mat";
        private const string PrefabPath = FxFolder + "/EatCrumbs.prefab";
        private const string ResultPath = "Temp/eat_effect_setup.txt";

        [MenuItem("Tools/Blob.io/Yeme Efektini Kur")]
        private static void Run()
        {
            var log = new StringBuilder();
            BuildPrefab(BuildMaterial());
            log.AppendLine("Prefab: " + PrefabPath);
            AssignColors(log);
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Temp");
            File.WriteAllText(ResultPath, log.ToString());
            Debug.Log($"[EatEffectSetup] Bitti, sahneyi kaydet (Cmd+S). Ayrıntı: {Path.GetFullPath(ResultPath)}");
        }

        private static Material BuildMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Simple Lit"));
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void BuildPrefab(Material mat)
        {
            var go = new GameObject("EatCrumbs");
            try
            {
                var ps = go.AddComponent<ParticleSystem>();
                go.AddComponent<EatCrumbs>();

                var main = ps.main;
                main.duration = 1f;
                main.loop = false;
                main.playOnAwake = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.75f);
                main.startRotation3D = true;
                main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.gravityModifier = 0.8f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.scalingMode = ParticleSystemScalingMode.Shape;
                main.maxParticles = 150;

                var emission = ps.emission;
                emission.rateOverTime = 0f; // EatCrumbs.Play Emit ile basar

                var shape = ps.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.randomDirectionAmount = 0.6f;

                var force = ps.forceOverLifetime;
                force.enabled = true;
                force.space = ParticleSystemSimulationSpace.World;

                var size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 1f), new Keyframe(0.6f, 0.85f), new Keyframe(1f, 0f)));

                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.separateAxes = true;
                rot.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
                rot.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
                rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.alignment = ParticleSystemRenderSpace.Local;

                PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static void AssignColors(StringBuilder log)
        {
            log.AppendLine("\n# Kırıntı renkleri");
            var done = new HashSet<ConsumableData>();
            foreach (var c in Resources.FindObjectsOfTypeAll<ConsumableBase>())
            {
                if (EditorUtility.IsPersistent(c) || !c.gameObject.scene.IsValid()) continue;
                var data = new SerializedObject(c).FindProperty("_data")?.objectReferenceValue as ConsumableData;
                if (data == null || data is PedestrianData || !done.Add(data)) continue;
                if (!TryCrumbColors(c.transform, out Color a, out Color b)) continue;
                SetColors(new SerializedObject(data), a, b);
                EditorUtility.SetDirty(data);
                log.AppendLine($"{data.name}\t#{ColorUtility.ToHtmlStringRGB(a)} #{ColorUtility.ToHtmlStringRGB(b)}");
            }

            var spawner = Object.FindAnyObjectByType<CarSpawner>(FindObjectsInactive.Include);
            if (spawner == null) return;
            var prefabs = new SerializedObject(spawner).FindProperty("_carPrefabs");
            for (int i = 0; i < prefabs.arraySize; i++)
            {
                var car = prefabs.GetArrayElementAtIndex(i).objectReferenceValue as CarController;
                if (car == null || !TryCrumbColors(car.transform, out Color a, out Color b)) continue;
                SetColors(new SerializedObject(car), a, b);
                EditorUtility.SetDirty(car);
                log.AppendLine($"{car.name}\t#{ColorUtility.ToHtmlStringRGB(a)} #{ColorUtility.ToHtmlStringRGB(b)}");
            }
        }

        internal static void SetColors(SerializedObject so, Color a, Color b)
        {
            so.FindProperty("_crumbColorA").colorValue = a;
            so.FindProperty("_crumbColorB").colorValue = b;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static readonly Dictionary<Texture, Texture2D> s_readable = new Dictionary<Texture, Texture2D>();

        // Modelin üçgenlerini UV'lerinden palet rengine bağlar; alana göre en çok kullanılan iki rengi döner.
        internal static bool TryCrumbColors(Transform root, out Color a, out Color b)
        {
            a = b = Color.grey;
            var area = new Dictionary<Color32, float>();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<MeshRenderer>();
                Mesh mesh = mf.sharedMesh;
                if (mesh == null || r == null || r.sharedMaterial == null) continue; // editörde Read/Write kapalı mesh de okunur
                Texture2D tex = Readable(r.sharedMaterial.mainTexture);
                if (tex == null) continue;
                Vector3[] v = mesh.vertices;
                Vector2[] uv = mesh.uv;
                int[] t = mesh.triangles;
                if (uv.Length != v.Length) continue;
                for (int i = 0; i < t.Length; i += 3)
                {
                    float ar = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]).magnitude;
                    Vector2 c = (uv[t[i]] + uv[t[i + 1]] + uv[t[i + 2]]) / 3f;
                    Color32 px = tex.GetPixel(Mathf.Clamp((int)(Mathf.Repeat(c.x, 1f) * tex.width), 0, tex.width - 1),
                                              Mathf.Clamp((int)(Mathf.Repeat(c.y, 1f) * tex.height), 0, tex.height - 1));
                    area[px] = (area.TryGetValue(px, out float s) ? s : 0f) + ar;
                }
            }
            if (area.Count == 0) return false;
            var top = area.OrderByDescending(kv => kv.Value).Take(2).Select(kv => (Color)kv.Key).ToArray();
            a = top[0];
            b = top.Length > 1 ? top[1] : top[0] * 0.85f;
            a.a = b.a = 1f;
            return true;
        }

        // Palet dokuları sıkıştırılmış/okunamaz olabilir; kaynak PNG'den bir kopya okunur.
        private static Texture2D Readable(Texture texture)
        {
            if (texture == null) return null;
            if (s_readable.TryGetValue(texture, out Texture2D cached) && cached != null) return cached;
            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            var copy = new Texture2D(2, 2);
            if (!copy.LoadImage(File.ReadAllBytes(path))) return null;
            s_readable[texture] = copy;
            return copy;
        }
    }
}
