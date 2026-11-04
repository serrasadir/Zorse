using System.Collections.Generic;
using System.Linq;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Pedestrians;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // 2026-11-04: yaya çeşitlendirmesinin Editor adımları tek menüde — elle FBX sürüklemek yerine.
    // (1) Üç PedestrianData asset'inin (Man/Woman/Big) Variant Meshes listesini doldurur, (2) açık sahnedeki
    // PedestrianSpawner'ın havuzuna Big tipini ekler. Çocuk tipi 2026-11-04'te kaldırıldı (Serra). Tekrar çalıştırmak güvenli (listeler baştan yazılır).
    // Modeller: zorse-blender/build_people.py → Art/City/People (H_Man/H_Woman Art/City'de).
    public static class PedestrianVariantSetup
    {
        private const string DataFolder = "Assets/_Project/ScriptableObjects/Pedestrians";
        private const string CityArt = "Assets/_Project/Art/City";
        private const string PeopleArt = "Assets/_Project/Art/City/People";

        private static readonly Dictionary<string, string[]> Variants = new Dictionary<string, string[]>
        {
            ["PedestrianData_Man"] = new[] { "H_Man", "H_ManCasual", "H_ManCasual2", "H_Business", "H_Chef", "H_Doctor",
                                             "H_TouristM", "H_JoggerM", "H_Teen", "H_SimitSeller", "H_OldMan" },
            ["PedestrianData_Woman"] = new[] { "H_Woman", "H_WomanCasual", "H_WomanSkirt", "H_Businesswoman", "H_DoctorW",
                                               "H_TouristW", "H_JoggerW", "H_OldLady" },
            ["PedestrianData_Big"] = new[] { "H_Worker", "H_Courier", "H_Mover" },
        };

        [MenuItem("Tools/Blob.io/Yaya Varyantlarını Bağla")]
        private static void Run() => Debug.Log("[PedestrianVariantSetup]\n" + Apply());

        private static string Apply()
        {
            var log = new System.Text.StringBuilder();
            var allData = new List<PedestrianData>();

            foreach (KeyValuePair<string, string[]> entry in Variants)
            {
                string path = $"{DataFolder}/{entry.Key}.asset";
                PedestrianData data = AssetDatabase.LoadAssetAtPath<PedestrianData>(path);
                if (data == null)
                {
                    log.AppendLine($"ATLANDI {path}: bulunamadı");
                    continue;
                }
                allData.Add(data);

                List<Mesh> meshes = new List<Mesh>();
                foreach (string name in entry.Value)
                {
                    Mesh mesh = LoadMesh(name);
                    if (mesh != null) meshes.Add(mesh);
                    else log.AppendLine($"  EKSİK {name}.fbx (mesh yok)");
                }

                SerializedObject so = new SerializedObject(data);
                SerializedProperty list = so.FindProperty("_variantMeshes");
                list.arraySize = meshes.Count;
                for (int i = 0; i < meshes.Count; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = meshes[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                log.AppendLine($"OK {entry.Key}: {meshes.Count}/{entry.Value.Length} varyant");
            }
            AssetDatabase.SaveAssets();

            PedestrianSpawner spawner = Object.FindAnyObjectByType<PedestrianSpawner>();
            if (spawner == null)
            {
                log.AppendLine("ATLANDI PedestrianSpawner: açık sahnede yok (GameScene'i açıp tekrar çalıştır)");
                return log.ToString();
            }

            SerializedObject spawnerSo = new SerializedObject(spawner);
            SerializedProperty pool = spawnerSo.FindProperty("_pedestrianPool");
            List<Object> current = Enumerable.Range(0, pool.arraySize)
                .Select(i => pool.GetArrayElementAtIndex(i).objectReferenceValue)
                .Where(o => o != null).ToList();
            foreach (PedestrianData data in allData)
                if (!current.Contains(data)) current.Add(data);

            Undo.RecordObject(spawner, "Yaya havuzu");
            pool.arraySize = current.Count;
            for (int i = 0; i < current.Count; i++)
                pool.GetArrayElementAtIndex(i).objectReferenceValue = current[i];
            spawnerSo.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            log.AppendLine($"OK PedestrianSpawner havuzu: {string.Join(", ", current.Select(o => o.name))}. Sahneyi kaydet (Cmd+S).");
            return log.ToString();
        }

        private static Mesh LoadMesh(string name)
        {
            foreach (string folder in new[] { PeopleArt, CityArt })
            {
                string path = $"{folder}/{name}.fbx";
                Mesh mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();
                if (mesh != null) return mesh;
            }
            return null;
        }
    }
}
