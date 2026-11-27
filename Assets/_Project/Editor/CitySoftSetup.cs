using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlobSurvivor.EditorTools
{
    // Tools → Blob.io → Şehri Soft Görünüme Geçir (Bahar, 2026-11-20: kaledeki görünümü şehirde de karşılaştırmak için).
    // Ön koşul: tools/city_soft/soften_city.py şehir FBX'lerine yuvarlatma + köşe karartması (köşe rengi) yazmış olmalı.
    // GameScene'i açar; şehir palet materyallerini köşe karartmasını okuyan shader'a (PaletteAO) geçirir, şehir prefab'larına
    // temas gölgesi ekler, kaledeki sıcak ışığı (kendi profil kopyasıyla) uygular, yaya/polis yürüyüş parçalarını yeniden böler
    // (köşe rengi parçalara taşınsın), sahneyi kaydeder. Tekrar çalıştırmak güvenli.
    public static class CitySoftSetup
    {
        private const string GameScenePath = "Assets/_Project/Scenes/GameScene.unity";
        private const string CityPrefabs = "Assets/_Project/Prefabs/City";
        private const string CityProfilePath = "Assets/_Project/Art/City/CityWarmVolumeProfile.asset";
        private static readonly string[] Materials =
        {
            "Assets/_Project/Art/City/Mat_CityPalette.mat",
            "Assets/_Project/Art/City/Kit2/Mat_CityPaletteV2.mat",
            "Assets/_Project/Art/City/Mat_RobotDog.mat",
        };

        // Sadece şehrin ışığını yeniden uygular (ApplyWarmLighting değişince), modellere dokunmaz.
        [MenuItem("Tools/Blob.io/Şehir Işığını Uygula")]
        public static void ApplyLightingBoth()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.isDirty) { Debug.LogError($"[CitySoft] {active.name} kaydedilmemiş: önce kaydet."); return; }
            string back = active.path;
            foreach (var (scenePath, profile) in new[] { (GameScenePath, CityProfilePath) }) // Crownreach kendi ışığında
            {
                Scene s = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                MapBuildTools.ApplyWarmLighting(profile);
                EditorSceneManager.MarkSceneDirty(s);
                EditorSceneManager.SaveScene(s);
            }
            if (!string.IsNullOrEmpty(back)) EditorSceneManager.OpenScene(back, OpenSceneMode.Single);
            Debug.Log("[CitySoft] Işık iki haritaya uygulandı.");
        }

        private static void RemoveContactShadow(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform t = root.transform.Find("ContactShadow");
                if (t == null) return;
                Object.DestroyImmediate(t.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [MenuItem("Tools/Blob.io/Şehri Soft Görünüme Geçir")]
        public static void Run()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path != GameScenePath)
            {
                if (active.isDirty) { Debug.LogError($"[CitySoft] {active.name} kaydedilmemiş: önce kaydet."); return; }
                EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            }

            Shader ao = Shader.Find("BlobSurvivor/PaletteAO");
            if (ao == null) { Debug.LogError("[CitySoft] BlobSurvivor/PaletteAO shader'ı yok."); return; }
            int mats = 0;
            foreach (string path in Materials)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null) continue;
                Texture tex = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                m.shader = ao;
                if (tex != null) m.SetTexture("_BaseMap", tex);
                m.SetColor("_BaseColor", Color.white);
                EditorUtility.SetDirty(m);
                mats++;
            }

            int shadows = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { CityPrefabs }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name.Contains("Med")) continue; // kale prefab'ları kale kurulumunda
                // Temas gölgesi sadece bina ve arabada (eşyalardaki yüzlerce saydam kare mobilde pahalıydı); eşyalardan kaldırılır.
                bool building = name.StartsWith("B_");
                bool car = path.Contains("/Cars/");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                if (!building && !car)
                {
                    if (prefab.transform.Find("ContactShadow") != null) RemoveContactShadow(path);
                    continue;
                }
                if (prefab.GetComponent<BoxCollider>() == null) continue;
                MapBuildTools.AddContactShadow(prefab);
                shadows++;
            }

            MapBuildTools.ApplyWarmLighting(CityProfilePath);
            EnemyWalkSetup.Setup(null);
            PedestrianRigSetup.Setup(null);

            AssetDatabase.SaveAssets();
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[CitySoft] {mats} materyal PaletteAO'ya, {shadows} prefab'a temas gölgesi, sıcak ışık; sahne kaydedildi.");
        }
    }
}
