using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BlobSurvivor.Entities.Vehicles;

namespace BlobSurvivor.EditorTools
{
    // Perf testi (#44/#50) için geçici greybox şehir — gerçek sanat (Hüma/Bahar ile konuşulacak,
    // bkz. CLAUDE.md) gelene kadar NavMesh/steering'in bina benzeri engellerle nasıl davrandığını
    // test etmeye yarar. Menüden çalıştırılır, sahneye kalıcı GameObject'ler ekler; NavMesh'i
    // SONRA elle bake etmek gerekir (bu proje Unity.AI.Navigation paketini kullanıyor — "Ground"
    // GameObject'inin üzerindeki NavMeshSurface component'ini seçip Inspector'daki "Bake"
    // butonuna basmak yeterli, bu script kendisi bake etmiyor).
    public static class GreyboxCityGenerator
    {
        private const string RootName = "GreyboxCity";
        private const int GridSize = 6;          // 6x6 blok
        private const float BlockSpacing = 12f;   // blok merkezleri arası mesafe
        private const float BuildingChance = 0.6f; // bazı bloklar boş meydan/sokak kalsın
        private const float StreetWidth = 6f;      // blok içi boşluk (sokak payı)
        private const float BlobKeepClearRadius = 8f; // Blob (0,1,0)'da spawn olur, oraya bina konmasın

        [MenuItem("Tools/Blob.io/Greybox Şehir Oluştur")]
        private static void Generate()
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog("Greybox Şehir",
                    "Sahnede zaten bir 'GreyboxCity' var. Silinip yeniden mi oluşturulsun?",
                    "Evet, yeniden oluştur", "İptal");
                if (!replace) return;
                Undo.DestroyObjectImmediate(existing);
            }

            int environmentLayer = LayerMask.NameToLayer("Environment");
            if (environmentLayer < 0)
            {
                Debug.LogError("[GreyboxCityGenerator] 'Environment' layer bulunamadı (CLAUDE.md'de layer 16 olarak tanımlı) — Project Settings > Tags and Layers'ı kontrol et.");
                return;
            }

            // Art/Buildings'te asset varsa küp yerine onlar kullanılır; yoksa eski greybox davranışı.
            List<GameObject> buildingPrefabs = BuildingAssetPipeline.BuildPrefabs();

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Greybox Şehir Oluştur");

            float offset = (GridSize - 1) * BlockSpacing * 0.5f;
            int buildingCount = 0;

            for (int x = 0; x < GridSize; x++)
            {
                for (int z = 0; z < GridSize; z++)
                {
                    Vector3 blockCenter = new Vector3(x * BlockSpacing - offset, 0f, z * BlockSpacing - offset);

                    if (blockCenter.magnitude < BlobKeepClearRadius) continue;
                    if (Random.value > BuildingChance) continue;

                    if (buildingPrefabs.Count > 0)
                    {
                        GameObject prefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Count)];
                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                        instance.name = $"Building_{x}_{z}";
                        instance.transform.position = blockCenter; // döndürme yok: ön cephe kameraya bakmalı
                        Undo.RegisterCreatedObjectUndo(instance, "Greybox Şehir Oluştur");
                        buildingCount++;
                        continue;
                    }

                    float width = Random.Range(4f, BlockSpacing - StreetWidth);
                    float depth = Random.Range(4f, BlockSpacing - StreetWidth);
                    float height = Random.Range(3f, 12f);

                    GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    building.name = $"Building_{x}_{z}";
                    building.transform.SetParent(root.transform);
                    building.transform.position = blockCenter + new Vector3(0f, height * 0.5f, 0f);
                    building.transform.localScale = new Vector3(width, height, depth);
                    building.layer = environmentLayer;

                    GameObjectUtility.SetStaticEditorFlags(building, StaticEditorFlags.NavigationStatic);
                    Undo.RegisterCreatedObjectUndo(building, "Greybox Şehir Oluştur");
                    buildingCount++;
                }
            }

            Selection.activeGameObject = root;
            Debug.Log($"[GreyboxCityGenerator] {buildingCount} bina oluşturuldu. Şimdi Hierarchy'de 'Ground' GameObject'ini seç, Inspector'daki NavMeshSurface component'inde 'Bake' butonuna bas — yoksa yeni binalar NavMesh'e yansımaz.");
        }

        // 2026-09-05 (araba yolu testi): Serra'nın "hiç düzgün edemedim" dediği CarSpawner sorununun
        // kök sebebi bulundu — sahnedeki iki eski waypoint (Road_Waypoints/Waypoint_0 ve "Waypoint_0 (1)")
        // AYNI world pozisyonundaydı (muhtemelen Ctrl+D kopyası hiç taşınmamış), yani CarController'ın
        // waypoint döngüsünde gidecek ikinci bir nokta hiç yoktu. Bu araç, binaların ARASINDAKİ sokak
        // koridorlarını (Generate()'in aynı ızgara sabitleriyle hesaplanır, bina yarı-genişliği
        // (BlockSpacing-StreetWidth)/2 olduğu için sokak çizgisi her zaman güvenli mesafede kalır)
        // kullanarak 4 köşeli gerçek bir döngü yolu kurar ve CarSpawner._waypoints'e otomatik bağlar.
        internal const string CarRoadRootName = "CarRoad_Waypoints";
        private const string LegacyCarRoadRootName = "Road_Waypoints";
        private const float RoadWidth = 3f;
        private const float RoadThickness = 0.05f;

        [MenuItem("Tools/Blob.io/Araba Yolu Oluştur")]
        private static void GenerateCarRoad()
        {
            GameObject legacy = GameObject.Find(LegacyCarRoadRootName);
            if (legacy != null)
                Undo.DestroyObjectImmediate(legacy);

            GameObject existing = GameObject.Find(CarRoadRootName);
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog("Araba Yolu",
                    "Sahnede zaten bir 'CarRoad_Waypoints' var. Silinip yeniden mi oluşturulsun?",
                    "Evet, yeniden oluştur", "İptal");
                if (!replace) return;
                Undo.DestroyObjectImmediate(existing);
            }

            float offset = (GridSize - 1) * BlockSpacing * 0.5f;
            float z1 = StreetLineCoord(1, offset);
            float z2 = StreetLineCoord(GridSize - 2, offset);
            float x1 = StreetLineCoord(1, offset);
            float x2 = StreetLineCoord(GridSize - 2, offset);

            Vector3[] corners =
            {
                new Vector3(x1, 0f, z1),
                new Vector3(x2, 0f, z1),
                new Vector3(x2, 0f, z2),
                new Vector3(x1, 0f, z2),
            };

            GameObject root = new GameObject(CarRoadRootName);
            Undo.RegisterCreatedObjectUndo(root, "Araba Yolu Oluştur");

            Transform[] waypoints = new Transform[corners.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                GameObject wp = new GameObject($"Waypoint_{i}");
                Undo.RegisterCreatedObjectUndo(wp, "Araba Yolu Oluştur");
                wp.transform.SetParent(root.transform);
                wp.transform.position = corners[i];
                waypoints[i] = wp.transform;
            }

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 a = corners[i];
                Vector3 b = corners[(i + 1) % corners.Length];
                Vector3 mid = (a + b) * 0.5f + new Vector3(0f, RoadThickness * 0.5f, 0f);
                float length = Vector3.Distance(a, b);
                bool horizontal = Mathf.Abs(a.x - b.x) > Mathf.Abs(a.z - b.z);

                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(segment, "Araba Yolu Oluştur");
                segment.name = $"RoadSegment_{i}";
                segment.transform.SetParent(root.transform);
                segment.transform.position = mid;
                segment.transform.localScale = horizontal
                    ? new Vector3(length, RoadThickness, RoadWidth)
                    : new Vector3(RoadWidth, RoadThickness, length);

                Object.DestroyImmediate(segment.GetComponent<Collider>()); // sadece görsel — çarpışma/navmesh engeli olmasın
            }

            CarSpawner spawner = Object.FindAnyObjectByType<CarSpawner>();
            if (spawner != null)
            {
                SerializedObject so = new SerializedObject(spawner);
                SerializedProperty waypointsProp = so.FindProperty("_waypoints");
                waypointsProp.arraySize = waypoints.Length;
                for (int i = 0; i < waypoints.Length; i++)
                    waypointsProp.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(spawner);
                Debug.Log($"[GreyboxCityGenerator] Araba yolu oluşturuldu, CarSpawner._waypoints otomatik bağlandı ({waypoints.Length} köşe). NavMesh bake gerekmiyor — arabalar waypoint tabanlı, NavMesh kullanmıyor.");
            }
            else
            {
                Debug.LogWarning("[GreyboxCityGenerator] Sahnede CarSpawner bulunamadı — waypoint'ler oluşturuldu ama hiçbir spawner'a bağlanamadı, Inspector'dan elle sürüklemen gerekecek.");
            }

            Selection.activeGameObject = root;
        }

        private static float StreetLineCoord(int gapIndex, float offset) => gapIndex * BlockSpacing - offset + BlockSpacing * 0.5f;

        [MenuItem("Tools/Blob.io/Greybox Şehiri Sil")]
        private static void Remove()
        {
            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                Debug.Log("[GreyboxCityGenerator] Sahnede 'GreyboxCity' yok, silinecek bir şey bulunamadı.");
                return;
            }

            Undo.DestroyObjectImmediate(root);
            Debug.Log("[GreyboxCityGenerator] Greybox şehir silindi. 'Ground' üzerindeki NavMeshSurface'i tekrar Bake etmeyi unutma.");
        }
    }
}
