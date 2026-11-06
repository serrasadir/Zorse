using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using BlobSurvivor.Core;
using BlobSurvivor.Entities.Blob;
using BlobSurvivor.Input;

// 2026-11-03: Play Mode'da (duraklatılmışken de) blob'un durumunu ve çevresindeki collider'ları dosyaya döker.
// Claude, Temp/blob_probe.request dosyası oluşturarak tetikler, sonucu Temp/blob_probe.txt'den okur.
// Elle: Tools → Blob.io → Blob Çevresini Raporla.
[InitializeOnLoad]
internal static class BlobProbe
{
    private const string RequestPath = "Temp/blob_probe.request";
    private const string ResultPath = "Temp/blob_probe.txt";
    // NavMesh denetimi: binaların içine/çatısına düşen yürünebilir alan var mı? (düzenleme modunda da çalışır)
    private const string NavRequestPath = "Temp/navmesh_probe.request";
    private const string NavResultPath = "Temp/navmesh_probe.txt";
    private const float ProbeReach = 4f;     // gövde yüzeyinden bu kadar dışarısı taranır
    private const float SweepDistance = 3f;  // 4 yönde gövde boyutunda süpürme mesafesi

    private static double s_nextPoll;

    static BlobProbe() => EditorApplication.update += Poll;

    [MenuItem("Tools/Blob.io/Blob Çevresini Raporla")]
    private static void WriteFromMenu()
    {
        Write();
        Debug.Log($"[BlobProbe] Rapor yazıldı: {Path.GetFullPath(ResultPath)}");
    }

    private static void Poll()
    {
        if (EditorApplication.timeSinceStartup < s_nextPoll) return;
        s_nextPoll = EditorApplication.timeSinceStartup + 0.5;
        if (File.Exists(NavRequestPath))
        {
            File.Delete(NavRequestPath);
            WriteNavMeshReport();
        }
        if (!File.Exists(RequestPath)) return;

        File.Delete(RequestPath);
        Write();
    }

    private static void WriteNavMeshReport()
    {
        Physics.SyncTransforms();
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        int envMask = LayerMask.GetMask("Environment");
        int total = tri.indices.Length / 3, insideGround = 0, raised = 0;
        float areaTotal = 0f, areaInside = 0f, areaRaised = 0f;
        var samples = new List<string>();

        for (int i = 0; i < tri.indices.Length; i += 3)
        {
            Vector3 a = tri.vertices[tri.indices[i]], b = tri.vertices[tri.indices[i + 1]], c = tri.vertices[tri.indices[i + 2]];
            Vector3 centroid = (a + b + c) / 3f;
            float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            areaTotal += area;

            if (centroid.y > 0.6f)
            {
                raised++; areaRaised += area;
                if (samples.Count < 8) samples.Add($"yüksek y={centroid.y:0.##} xz=({centroid.x:0.#}, {centroid.z:0.#})");
                continue;
            }
            // Zemin seviyesinde ama bir binanın engel kutusunun içinde mi?
            if (Physics.CheckSphere(new Vector3(centroid.x, 0.5f, centroid.z), 0.05f, envMask, QueryTriggerInteraction.Ignore))
            {
                insideGround++; areaInside += area;
                if (samples.Count < 16) samples.Add($"bina içi xz=({centroid.x:0.#}, {centroid.z:0.#})");
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"NavMesh üçgen={total} alan={areaTotal:0}");
        if (tri.vertices.Length > 0)
        {
            Bounds nb = new Bounds(tri.vertices[0], Vector3.zero);
            foreach (Vector3 v in tri.vertices) nb.Encapsulate(v);
            sb.AppendLine($"sınırlar x=[{nb.min.x:0.#}, {nb.max.x:0.#}] z=[{nb.min.z:0.#}, {nb.max.z:0.#}]");
        }
        foreach (string agent in new[] { "Humanoid", "Tank" })
        {
            int id = -1;
            for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
                if (NavMesh.GetSettingsNameFromID(NavMesh.GetSettingsByIndex(i).agentTypeID) == agent) id = NavMesh.GetSettingsByIndex(i).agentTypeID;
            var filter = new NavMeshQueryFilter { agentTypeID = id, areaMask = NavMesh.AllAreas };
            int hit = 0;
            foreach (var probe in new[] { new Vector3(0f, 0f, 7.5f), new Vector3(82f, 0f, 82f), new Vector3(-82f, 0f, 7.5f), new Vector3(7.5f, 0f, -75f) })
                if (NavMesh.SamplePosition(probe, out NavMeshHit h, 1.5f, filter)) hit++;
            sb.AppendLine($"{agent} (id {id}): 4 sokak noktasından {hit} tanesi NavMesh'te");
        }
        sb.AppendLine($"zemin seviyesi, bina kutusu içinde: üçgen={insideGround} alan={areaInside:0}");
        sb.AppendLine($"0.6 üstünde (çatı/teras vb.): üçgen={raised} alan={areaRaised:0}");
        foreach (string s in samples) sb.AppendLine(s);
        File.WriteAllText(NavResultPath, sb.ToString());
    }

    private static void Write()
    {
        var sb = new StringBuilder();
        CultureInfo inv = CultureInfo.InvariantCulture;
        sb.AppendLine($"isPlaying={EditorApplication.isPlaying} isPaused={EditorApplication.isPaused} state={(GameManager.Instance != null ? GameManager.Instance.CurrentState.ToString() : "-")}");

        GameObject blob = GameObject.FindWithTag("Blob");
        if (blob == null)
        {
            sb.AppendLine("Blob bulunamadı.");
            File.WriteAllText(ResultPath, sb.ToString());
            return;
        }

        Transform t = blob.transform;
        BlobGrowth growth = blob.GetComponent<BlobGrowth>();
        Rigidbody rb = blob.GetComponent<Rigidbody>();
        sb.AppendLine($"blob pos={V(t.position)} scale={V(t.localScale)} layer={LayerMask.LayerToName(blob.layer)}");
        if (growth != null) sb.AppendLine($"tier={growth.CurrentTier} mass={growth.CurrentMass.ToString("0.#", inv)} level={growth.CurrentLevel}");
        if (rb != null) sb.AppendLine($"rb velocity={V(rb.linearVelocity)} kinematic={rb.isKinematic} constraints={rb.constraints}");
        if (InputManager.Instance != null) sb.AppendLine($"input={InputManager.Instance.MoveDirection}");

        SphereCollider solid = null;
        foreach (SphereCollider c in blob.GetComponents<SphereCollider>())
        {
            sb.AppendLine($"blob collider trigger={c.isTrigger} radius={c.radius} worldRadius={(c.radius * t.lossyScale.x).ToString("0.###", inv)} enabled={c.enabled}");
            if (!c.isTrigger) solid = c;
        }

        float bodyRadius = solid != null ? solid.radius * t.lossyScale.x : 0.5f;
        Vector3 center = solid != null ? solid.bounds.center : t.position;

        // Vakum efekti (VacuumComponent'in oluşturduğu zemin kasırgası)
        GameObject fx = GameObject.Find("VacuumRange");
        sb.AppendLine(fx == null ? "VacuumRange yok" : $"VacuumRange active={fx.activeInHierarchy} pos={V(fx.transform.position)} blobBottom={(center.y - bodyRadius).ToString("0.###", inv)}");
        if (fx != null)
            foreach (Renderer r in fx.GetComponentsInChildren<Renderer>(true))
                sb.AppendLine($"  {r.name} enabled={r.enabled} visible={r.isVisible} mat={(r.sharedMaterial != null ? r.sharedMaterial.name + " shader=" + r.sharedMaterial.shader.name + " queue=" + r.sharedMaterial.renderQueue : "YOK")} scale={V(r.transform.lossyScale)} bounds={V(r.bounds.size)}");
        if (Physics.Raycast(center, Vector3.down, out RaycastHit ground, 10f, ~0, QueryTriggerInteraction.Ignore))
            sb.AppendLine($"zemin altında: {HierarchyPath(ground.collider.transform)} y={ground.point.y.ToString("0.###", inv)}");

        // 4 yönde gövde büyüklüğünde süpürme: blob'u o yönde ilk ne durdurur?
        sb.AppendLine();
        sb.AppendLine($"--- Süpürme (gövde yarıçapı {bodyRadius.ToString("0.###", inv)}, {SweepDistance} birim) ---");
        Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
        string[] names = { "sağ(+X)", "sol(-X)", "yukarı(+Z)", "aşağı(-Z)" };
        for (int d = 0; d < dirs.Length; d++)
        {
            RaycastHit[] hits = Physics.SphereCastAll(center, bodyRadius * 0.98f, dirs[d], SweepDistance, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            string first = "engel yok";
            foreach (RaycastHit h in hits)
            {
                if (h.collider.transform.IsChildOf(t)) continue;
                if (!BlocksBlob(blob, solid, h.collider)) continue;
                first = $"{HierarchyPath(h.collider.transform)} [{LayerMask.LayerToName(h.collider.gameObject.layer)}] mesafe={h.distance.ToString("0.###", inv)}";
                break;
            }
            sb.AppendLine($"{names[d]}: {first}");
        }

        // Çevredeki tüm collider'lar, gövde yüzeyine uzaklığa göre sıralı.
        sb.AppendLine();
        sb.AppendLine($"--- Çevre (gövde + {ProbeReach}) ---");
        Collider[] around = Physics.OverlapSphere(center, bodyRadius + ProbeReach, ~0, QueryTriggerInteraction.Collide);
        var rows = new List<(float gap, string line)>();
        foreach (Collider c in around)
        {
            if (c.transform.IsChildOf(t)) continue;
            Vector3 closest = c is MeshCollider mc && !mc.convex ? c.bounds.ClosestPoint(center) : c.ClosestPoint(center);
            float gap = Vector3.Distance(new Vector3(closest.x, center.y, closest.z), center) - bodyRadius;
            bool blocks = BlocksBlob(blob, solid, c);
            rows.Add((gap, $"gap={gap.ToString("0.###", inv),7} {(blocks ? "ENGEL " : "geçer ")} {HierarchyPath(c.transform)} [{LayerMask.LayerToName(c.gameObject.layer)}] {c.GetType().Name} trigger={c.isTrigger} bounds c={V(c.bounds.center)} size={V(c.bounds.size)}"));
        }
        rows.Sort((a, b) => a.gap.CompareTo(b.gap));
        foreach (var r in rows) sb.AppendLine(r.line);

        File.WriteAllText(ResultPath, sb.ToString());
    }

    // Blob'un katı collider'ını fiziksel olarak durdurur mu? (trigger değil, katman matrisi açık, IgnoreCollision yok)
    private static bool BlocksBlob(GameObject blob, Collider solid, Collider other)
    {
        if (other.isTrigger || !other.enabled) return false;
        if (Physics.GetIgnoreLayerCollision(blob.layer, other.gameObject.layer)) return false;
        if (solid != null && Physics.GetIgnoreCollision(solid, other)) return false;
        return true;
    }

    private static string V(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:0.##}, {1:0.##}, {2:0.##})", v.x, v.y, v.z);

    private static string HierarchyPath(Transform tr)
    {
        string p = tr.name;
        for (Transform parent = tr.parent; parent != null; parent = parent.parent) p = parent.name + "/" + p;
        return p;
    }
}
