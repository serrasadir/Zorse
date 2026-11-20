using BlobSurvivor.Data;
using BlobSurvivor.Entities;
using BlobSurvivor.Entities.Enemies;
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
        private static readonly string[] LimbNames = { "LegL", "LegR", "ArmL", "ArmR", ShieldName };
        private const string ShieldName = "Shield";
        private const float ShieldYaw = 70f;  // indirilince kalkan omuzdan dışa açılır (yukarıdan net görünsün)
        private const float ShieldDroop = 25f; // ve dışa doğru sarkar

        private struct Entry
        {
            public string Prefab, Mesh;
            public bool ArmL, ArmR;
            public string Shield; // ayrı kalkan parçası (pivot omuzda), sersemleyince iner
        }

        private static readonly Entry[] Entries =
        {
            new Entry { Prefab = "Enemy_Police", Mesh = "H_Police", ArmL = true, ArmR = true },
            new Entry { Prefab = "Enemy_ElitePolice", Mesh = "H_Elite", ArmL = true, ArmR = true },
            // FBX'te x aynalı: Blender'daki sol (kalkan) kolu Unity'de ArmR tarafı; o kol kalkanla ayrı parça, coplu kol sallanır
            new Entry { Prefab = "Enemy_RiotPolice", Mesh = "H_RiotPolice", ArmL = true, ArmR = false, Shield = "H_RiotShield" },
            new Entry { Prefab = "Enemy_Guard", Mesh = "H_Guard", ArmL = true, ArmR = true },
            // Kale haritası (MedievalSkinSetup kopyaları)
            new Entry { Prefab = "Enemy_MedTownGuard", Mesh = "H_MedTownGuard", ArmL = true, ArmR = true },
            new Entry { Prefab = "Enemy_MedArcher", Mesh = "H_MedArcher", ArmL = true, ArmR = true },
            new Entry { Prefab = "Enemy_MedShieldKnight", Mesh = "H_MedShieldKnight", ArmL = true, ArmR = true },
            new Entry { Prefab = "Enemy_MedMilitia", Mesh = "H_MedMilitia", ArmL = true, ArmR = true },
        };

        [MenuItem("Tools/Blob.io/Polis Yürüyüş Parçalarını Kur")]
        private static void SetupAll() => Setup(null);

        // onlyPrefix: sadece bu önekli prefab'lar (kale kurulumu "Enemy_Med": şehir rig'leri yeniden üretilmesin).
        internal static void Setup(string onlyPrefix)
        {
            if (!AssetDatabase.IsValidFolder(RigFolder)) AssetDatabase.CreateFolder("Assets/_Project/Art/City", "EnemyRigs");
            var log = new System.Text.StringBuilder();
            foreach (Entry e in Entries)
                if (onlyPrefix == null || e.Prefab.StartsWith(onlyPrefix)) log.AppendLine(Apply(e, log));
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

                string shieldResult = string.IsNullOrEmpty(e.Shield) ? "" : AddShield(e, contents, model, renderer, source);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                return $"OK {e.Prefab}{shieldResult}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // Kalkan FBX'i gövdeyle aynı uzayda (Blender'da obje konumu = omuz). Mesh omuza göre Model-yerel uzaya
        // çevrilir, Model'in child'ı olarak omuza konur; indirilmiş poz geometriden: dışa açılış + sarkma.
        private static string AddShield(Entry e, GameObject contents, Transform model, MeshRenderer bodyRenderer, MeshFilter body)
        {
            MeshFilter src = CityKitPipeline.LoadSourceMesh(e.Shield);
            if (src == null) return $", kalkan ATLANDI: {e.Shield}.fbx yok";

            Matrix4x4 toModel = body.transform.worldToLocalMatrix * src.transform.localToWorldMatrix;
            Vector3 pivot = toModel.MultiplyPoint3x4(Vector3.zero);
            Mesh srcMesh = src.sharedMesh;
            Vector3[] pos = srcMesh.vertices;
            Vector3[] nrm = srcMesh.normals;
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < pos.Length; i++)
            {
                pos[i] = toModel.MultiplyPoint3x4(pos[i]) - pivot;
                centroid += pos[i];
                if (i < nrm.Length) nrm[i] = toModel.MultiplyVector(nrm[i]).normalized;
            }
            centroid /= Mathf.Max(1, pos.Length);

            string assetPath = $"{RigFolder}/{e.Shield}_Part.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            bool created = mesh == null;
            if (created) mesh = new Mesh();
            mesh.Clear();
            mesh.name = e.Shield + "_Part";
            mesh.vertices = pos;
            if (nrm.Length == pos.Length) mesh.normals = nrm; else mesh.RecalculateNormals();
            mesh.uv = srcMesh.uv;
            mesh.triangles = srcMesh.triangles;
            mesh.RecalculateBounds();
            if (created) AssetDatabase.CreateAsset(mesh, assetPath); else EditorUtility.SetDirty(mesh);

            var go = new GameObject(ShieldName) { layer = model.gameObject.layer };
            go.transform.SetParent(model, false);
            go.transform.localPosition = pivot;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = bodyRenderer.sharedMaterials;
            r.shadowCastingMode = bodyRenderer.shadowCastingMode; // büyük parça: gölgesi görünsün
            r.receiveShadows = bodyRenderer.receiveShadows;

            // Model-yerel yönler: yukarı, ileri (kalkan omzun önünde), dış yan (omuz gövdenin hangi yanında).
            Vector3 up = body.transform.InverseTransformDirection(Vector3.up).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(centroid, up).normalized;
            Vector3 side = Vector3.ProjectOnPlane(pivot, up).normalized;
            Quaternion yaw = Quaternion.AngleAxis(ShieldYaw, up);
            if (Vector3.Dot(yaw * forward, side) < 0f) yaw = Quaternion.AngleAxis(-ShieldYaw, up);
            Vector3 opened = yaw * forward;
            Quaternion droop = Quaternion.AngleAxis(ShieldDroop, opened);
            if (Vector3.Dot(droop * up, side) < 0f) droop = Quaternion.AngleAxis(-ShieldDroop, opened);

            ShieldLower lower = contents.GetComponent<ShieldLower>();
            if (lower == null) lower = contents.AddComponent<ShieldLower>();
            var so = new SerializedObject(lower);
            so.FindProperty("_shield").objectReferenceValue = go.transform;
            so.FindProperty("_lowered").quaternionValue = droop * yaw;
            so.ApplyModifiedPropertiesWithoutUndo();
            return ", kalkan omuzda";
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
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // gövdenin gölgesi yeter
            renderer.receiveShadows = source.receiveShadows;
            return go.transform;
        }
    }
}
