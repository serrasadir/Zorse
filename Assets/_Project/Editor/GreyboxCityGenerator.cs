using UnityEditor;
using UnityEngine;

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
