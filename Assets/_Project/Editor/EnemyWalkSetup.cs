using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Polis prefab'larına yürüyüş: modeli gövde + bacak + kol mesh'ine böler (PedestrianRigSetup.Split, aynı kural),
    // parçaları Model'in child'ı olarak ekler, köke LimbWalk takar. Parçalar prefab'da olduğu için vurulma kızarması,
    // ölüm efekti ve hamle pozu onları da kapsar. Model kaynağı hep FBX (Art/City/H_*.fbx): tekrar çalıştırmak
    // güvenli. Şehir kurulumu (ApplyEnemyModel) Model'i yeniden takarsa bu menü tekrar çalıştırılır.
    internal static class EnemyWalkSetup
    {
        private const string EnemyFolder = "Assets/_Project/Prefabs/Enemies";
        private const string RigFolder = "Assets/_Project/Art/City/EnemyRigs";
        private static readonly string[] LimbNames = { "LegL", "LegR", "ArmL", "ArmR" };

        private struct Entry
        {
            public string Prefab, Mesh;
            public bool ArmL, ArmR;
        }

        private static readonly Entry[] Entries =
        {
            new Entry { Prefab = "Enemy_Police", Mesh = "H_Police", ArmL = true, ArmR = true },
            new Entry { Prefab = "Enemy_ElitePolice", Mesh = "H_Elite", ArmL = true, ArmR = true },
            new Entry { Prefab = "Enemy_RiotPolice", Mesh = "H_RiotPolice", ArmL = false, ArmR = true }, // sol kol kalkanı tutar
        };

        [MenuItem("Tools/Blob.io/Polis Yürüyüş Parçalarını Kur")]
        private static void Setup()
        {
            if (!AssetDatabase.IsValidFolder(RigFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art/City", "EnemyRigs");
            var log = new System.Text.StringBuilder();
            foreach (Entry e in Entries) log.AppendLine(Apply(e, log));
            AssetDatabase.SaveAssets();
            Debug.Log($"[EnemyWalkSetup]\n{log}");
        }

        private static string Apply(Entry e, System.Text.StringBuilder log)
        {
            string path = $"{EnemyFolder}/{e.Prefab}.prefab";
            MeshFilter source = CityKitPipeline.LoadSourceMesh(e.Mesh);
            if (source == null) return $"ATLANDI {e.Prefab}: {e.Mesh}.fbx yok";

            PedestrianRig rig = PedestrianRigSetup.Split(source.sharedMesh, source.transform, RigFolder, e.Mesh, e.ArmL, e.ArmR, log);
            if (rig == null) return $"ATLANDI {e.Prefab}: bölünemedi";

            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform model = contents.transform.Find("Model");
                MeshFilter filter = model != null ? model.GetComponent<MeshFilter>() : null;
                MeshRenderer renderer = model != null ? model.GetComponent<MeshRenderer>() : null;
                if (filter == null || renderer == null) return $"ATLANDI {e.Prefab}: Model yok";

                foreach (string limbName in LimbNames)
                {
                    Transform old = model.Find(limbName);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                }
                filter.sharedMesh = rig.Body;
                Transform legL = AddLimb(model, renderer, "LegL", rig.LegL, rig.HipL);
                Transform legR = AddLimb(model, renderer, "LegR", rig.LegR, rig.HipR);
                Transform armL = AddLimb(model, renderer, "ArmL", rig.ArmL, rig.ShoulderL);
                Transform armR = AddLimb(model, renderer, "ArmR", rig.ArmR, rig.ShoulderR);

                LimbWalk walk = contents.GetComponent<LimbWalk>();
                if (walk == null) walk = contents.AddComponent<LimbWalk>();
                var so = new SerializedObject(walk);
                so.FindProperty("_legL").objectReferenceValue = legL;
                so.FindProperty("_legR").objectReferenceValue = legR;
                so.FindProperty("_armL").objectReferenceValue = armL;
                so.FindProperty("_armR").objectReferenceValue = armR;
                so.FindProperty("_axis").vector3Value = rig.SwingAxis;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                return $"OK {e.Prefab}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static Transform AddLimb(Transform model, MeshRenderer source, string name, Mesh mesh, Vector3 joint)
        {
            if (mesh == null) return null;
            var go = new GameObject(name) { layer = model.gameObject.layer };
            go.transform.SetParent(model, false);
            go.transform.localPosition = joint;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = source.sharedMaterials;
            renderer.shadowCastingMode = source.shadowCastingMode;
            renderer.receiveShadows = source.receiveShadows;
            return go.transform;
        }
    }
}
