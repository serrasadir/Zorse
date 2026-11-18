using System.Collections.Generic;
using BlobSurvivor.Data;
using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Yaya modellerini gövde + 2 bacak + 2 kol mesh'ine böler ve eklem noktalarını PedestrianData._rigs'e yazar
    // (PedestrianRig; yürürken bacak/kol sallanır). Modeller küplerden: bacak = kalça yüksekliğinin altındaki
    // üçgenler (sol/sağ), kol = gövde genişliğinin dışındaki üçgenler. Kalça = ortadan geçen (x=0'ı kesen) en alt
    // parçanın tabanı. Bacak gövde genişliği içinde (yandaki çanta/baston değil), kol omuz hizasının altında
    // (şapka siperi/tepsi değil). Kolu ayrılamayan (kutu taşıyan, geniş gövdeli) model sadece bacakla yürür.
    // Bütün PedestrianData varyantları işlenir. Mesh'ler mesh-yerel uzayda kalır. Tekrar çalıştırmak güvenli.
    internal static class PedestrianRigSetup
    {
        private const string PeopleFolder = "Assets/_Project/Art/City/People";
        private const string RigFolder = PeopleFolder + "/Rigs";
        private const string DataFolder = "Assets/_Project/ScriptableObjects/Pedestrians";

        private const float CrossX = 0.05f;      // x=0'ı kesen parça (gövde/baş)
        private const float DepthLimit = 0.22f;  // gövde hizası (ön/arka); öndeki kalkan, sırt çantası değil
        private const float ArmEpsilon = 0.011f;
        private const float ShoulderDrop = 0.04f;
        private const float ArmTopFraction = 0.6f; // kol, kalça → tepe aralığının bu oranının altında biter
        private const int MinArmTriangles = 8;    // daha azı: kol ayrılamadı (sadece bacak)

        [MenuItem("Tools/Blob.io/Yaya Yürüyüş Parçalarını Kur")]
        private static void Setup()
        {
            if (!AssetDatabase.IsValidFolder(RigFolder)) AssetDatabase.CreateFolder(PeopleFolder, "Rigs");

            var rigs = new List<PedestrianRig>();
            var log = new System.Text.StringBuilder();
            var done = new HashSet<Mesh>();
            foreach (string guid in AssetDatabase.FindAssets("t:PedestrianData", new[] { DataFolder }))
            {
                var data = AssetDatabase.LoadAssetAtPath<PedestrianData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null || data.VariantMeshes == null) continue;
                foreach (Mesh variant in data.VariantMeshes)
                {
                    if (variant == null || !done.Add(variant)) continue;
                    PedestrianRig rig = Build(variant, log);
                    if (rig != null) rigs.Add(rig);
                }
            }

            int assigned = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:PedestrianData", new[] { DataFolder }))
            {
                var data = AssetDatabase.LoadAssetAtPath<PedestrianData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null) continue;
                var mine = new List<PedestrianRig>();
                foreach (PedestrianRig rig in rigs)
                    if (System.Array.IndexOf(data.VariantMeshes ?? new Mesh[0], rig.Source) >= 0) mine.Add(rig);
                var so = new SerializedObject(data);
                SerializedProperty list = so.FindProperty("_rigs");
                list.ClearArray();
                for (int i = 0; i < mine.Count; i++)
                {
                    list.InsertArrayElementAtIndex(i);
                    WriteRig(list.GetArrayElementAtIndex(i), mine[i]);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(data);
                assigned += mine.Count;
                log.AppendLine($"{data.name}: {mine.Count} yürüyen görünüm");
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[PedestrianRigSetup] {rigs.Count} model bölündü, {assigned} veriye yazıldı.\n{log}");
        }

        private static void WriteRig(SerializedProperty p, PedestrianRig rig)
        {
            p.FindPropertyRelative("Source").objectReferenceValue = rig.Source;
            p.FindPropertyRelative("Body").objectReferenceValue = rig.Body;
            p.FindPropertyRelative("LegL").objectReferenceValue = rig.LegL;
            p.FindPropertyRelative("LegR").objectReferenceValue = rig.LegR;
            p.FindPropertyRelative("ArmL").objectReferenceValue = rig.ArmL;
            p.FindPropertyRelative("ArmR").objectReferenceValue = rig.ArmR;
            p.FindPropertyRelative("HipL").vector3Value = rig.HipL;
            p.FindPropertyRelative("HipR").vector3Value = rig.HipR;
            p.FindPropertyRelative("ShoulderL").vector3Value = rig.ShoulderL;
            p.FindPropertyRelative("ShoulderR").vector3Value = rig.ShoulderR;
            p.FindPropertyRelative("SwingAxis").vector3Value = rig.SwingAxis;
        }

        private enum Part { Body, LegL, LegR, ArmL, ArmR }

        private static PedestrianRig Build(Mesh mesh, System.Text.StringBuilder log)
        {
            string name = mesh.name;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(mesh));
            MeshFilter filter = null;
            if (model != null)
                foreach (MeshFilter f in model.GetComponentsInChildren<MeshFilter>(true))
                    if (f.sharedMesh == mesh) { filter = f; break; }
            if (filter == null) { log.AppendLine($"{name}: model yok"); return null; }
            return Split(mesh, filter.transform, RigFolder, name, true, true, log);
        }

        // Ortak bölme (yaya ve polis, EnemyWalkSetup): source = FBX'teki mesh'in transform'u (dönüş/ölçek).
        // armL/armR false: o kol gövdede kalır (kalkan tutan kol gibi). Mesh'ler folder'a yazılır.
        internal static PedestrianRig Split(Mesh mesh, Transform source, string folder, string name, bool armLAllowed,
            bool armRAllowed, System.Text.StringBuilder log)
        {
            // Mesh-yerel → dünya (Unity, y yukarı): sınıflandırma dünya birimleriyle yapılır.
            Matrix4x4 toWorld = Matrix4x4.TRS(Vector3.zero, source.rotation, source.lossyScale);
            Matrix4x4 toLocal = toWorld.inverse;
            Vector3[] local = mesh.vertices;
            var world = new Vector3[local.Length];
            for (int i = 0; i < local.Length; i++) world[i] = toWorld.MultiplyPoint3x4(local[i]);
            int[] tris = mesh.triangles;
            int triCount = tris.Length / 3;

            float hip = float.MaxValue;
            for (int t = 0; t < triCount; t++)
            {
                Bounds b = TriBounds(world, tris, t);
                if (b.min.x < -CrossX && b.max.x > CrossX && Mathf.Abs(b.center.z) <= DepthLimit) hip = Mathf.Min(hip, b.min.y);
            }
            float half = 0f;
            for (int t = 0; t < triCount; t++)
            {
                Bounds b = TriBounds(world, tris, t);
                if (b.min.x < -CrossX && b.max.x > CrossX && b.min.y < hip + 0.35f && b.max.y > hip + 0.05f)
                    half = Mathf.Max(half, Mathf.Max(-b.min.x, b.max.x));
            }
            if (hip == float.MaxValue || half <= 0f) { log.AppendLine($"{name}: kalça bulunamadı"); return null; }
            float top = float.MinValue;
            foreach (Vector3 p in world) top = Mathf.Max(top, p.y);
            float armTop = hip + (top - hip) * ArmTopFraction;

            var parts = new Part[triCount];
            for (int t = 0; t < triCount; t++)
            {
                Bounds b = TriBounds(world, tris, t);
                bool armBand = b.min.y > hip * 0.5f && b.max.y <= armTop;
                if (b.max.y <= hip + 0.001f && Mathf.Abs(b.center.x) <= half && Mathf.Abs(b.center.z) <= DepthLimit) parts[t] = b.center.x < 0f ? Part.LegL : Part.LegR;
                else if (armBand && b.min.x >= half - ArmEpsilon) parts[t] = Part.ArmR;
                else if (armBand && b.max.x <= -half + ArmEpsilon) parts[t] = Part.ArmL;
                else parts[t] = Part.Body;
            }

            // Kol ayrılamadıysa (az üçgen ya da tek taraf) kollar gövdede kalır: sadece bacak yürür.
            int armL = 0, armR = 0;
            foreach (Part part in parts) { if (part == Part.ArmL) armL++; else if (part == Part.ArmR) armR++; }
            // Sadece izin verilen kollar aranır (kalkanlı: sol kol kalkanla ayrı parça, gövdede yok).
            bool arms = (!armLAllowed || armL >= MinArmTriangles) && (!armRAllowed || armR >= MinArmTriangles)
                && (armLAllowed || armRAllowed);
            bool keepL = arms && armLAllowed, keepR = arms && armRAllowed;
            for (int t = 0; t < triCount; t++)
                if ((parts[t] == Part.ArmL && !keepL) || (parts[t] == Part.ArmR && !keepR)) parts[t] = Part.Body;

            // Eklemler (dünya): bacak = parçanın ortası, kalça yüksekliğinde; kol = ortası, üst ucun biraz altında.
            Vector3 hipL = Joint(world, tris, parts, Part.LegL, hip, false);
            Vector3 hipR = Joint(world, tris, parts, Part.LegR, hip, false);
            Vector3 shoulderL = Joint(world, tris, parts, Part.ArmL, 0f, true);
            Vector3 shoulderR = Joint(world, tris, parts, Part.ArmR, 0f, true);

            var rig = new PedestrianRig
            {
                Source = mesh,
                HipL = toLocal.MultiplyPoint3x4(hipL),
                HipR = toLocal.MultiplyPoint3x4(hipR),
                ShoulderL = toLocal.MultiplyPoint3x4(shoulderL),
                ShoulderR = toLocal.MultiplyPoint3x4(shoulderR),
                SwingAxis = toLocal.MultiplyVector(Vector3.right).normalized,
            };
            rig.Body = Extract(mesh, parts, Part.Body, Vector3.zero, folder, $"{name}_Body");
            rig.LegL = Extract(mesh, parts, Part.LegL, rig.HipL, folder, $"{name}_LegL");
            rig.LegR = Extract(mesh, parts, Part.LegR, rig.HipR, folder, $"{name}_LegR");
            rig.ArmL = keepL ? Extract(mesh, parts, Part.ArmL, rig.ShoulderL, folder, $"{name}_ArmL") : null;
            rig.ArmR = keepR ? Extract(mesh, parts, Part.ArmR, rig.ShoulderR, folder, $"{name}_ArmR") : null;
            if (rig.LegL == null || rig.LegR == null)
            {
                log.AppendLine($"{name}: bacak ayrılamadı, atlandı");
                return null;
            }
            log.AppendLine($"{name}: kalça {hip:0.00}, yarı genişlik {half:0.00}, " + (keepL && keepR ? "bacak + kol" : keepL || keepR ? "bacak + tek kol" : "sadece bacak"));
            return rig;
        }

        private static Bounds TriBounds(Vector3[] v, int[] tris, int t)
        {
            var b = new Bounds(v[tris[t * 3]], Vector3.zero);
            b.Encapsulate(v[tris[t * 3 + 1]]);
            b.Encapsulate(v[tris[t * 3 + 2]]);
            return b;
        }

        private static Vector3 Joint(Vector3[] v, int[] tris, Part[] parts, Part part, float hip, bool shoulder)
        {
            Vector3 sum = Vector3.zero;
            int n = 0;
            float top = float.MinValue;
            for (int t = 0; t < parts.Length; t++)
            {
                if (parts[t] != part) continue;
                for (int k = 0; k < 3; k++)
                {
                    Vector3 p = v[tris[t * 3 + k]];
                    sum += p;
                    n++;
                    top = Mathf.Max(top, p.y);
                }
            }
            if (n == 0) return Vector3.zero;
            Vector3 c = sum / n;
            c.y = shoulder ? top - ShoulderDrop : hip;
            return c;
        }

        // Parçanın üçgenlerini yeni mesh'e kopyalar; köşeler eklem (mesh-yerel) kadar kaydırılır.
        private static Mesh Extract(Mesh source, Part[] parts, Part part, Vector3 pivot, string folder, string assetName)
        {
            Vector3[] pos = source.vertices;
            Vector3[] nrm = source.normals;
            Vector2[] uv = source.uv;
            int[] tris = source.triangles;
            var map = new Dictionary<int, int>();
            var outPos = new List<Vector3>();
            var outNrm = new List<Vector3>();
            var outUv = new List<Vector2>();
            var outTris = new List<int>();
            for (int t = 0; t < parts.Length; t++)
            {
                if (parts[t] != part) continue;
                for (int k = 0; k < 3; k++)
                {
                    int i = tris[t * 3 + k];
                    if (!map.TryGetValue(i, out int j))
                    {
                        j = outPos.Count;
                        map[i] = j;
                        outPos.Add(pos[i] - pivot);
                        if (nrm.Length > i) outNrm.Add(nrm[i]);
                        if (uv.Length > i) outUv.Add(uv[i]);
                    }
                    outTris.Add(j);
                }
            }
            if (outTris.Count == 0) return null;

            string path = $"{folder}/{assetName}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool created = mesh == null;
            if (created) mesh = new Mesh();
            mesh.Clear();
            mesh.name = assetName;
            mesh.SetVertices(outPos);
            if (outNrm.Count == outPos.Count) mesh.SetNormals(outNrm);
            if (outUv.Count == outPos.Count) mesh.SetUVs(0, outUv);
            mesh.SetTriangles(outTris, 0);
            mesh.RecalculateBounds();
            if (outNrm.Count != outPos.Count) mesh.RecalculateNormals();
            if (created) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }
    }
}
