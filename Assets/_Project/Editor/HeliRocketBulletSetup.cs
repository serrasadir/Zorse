using UnityEditor;
using UnityEngine;
using BlobSurvivor.Data;
using BlobSurvivor.Entities.Enemies;

namespace BlobSurvivor.EditorTools
{
    // Final boss'un makineli serisi sarı top yerine küçük roket atsın: alan saldırısının roket şablonu
    // (Enemy_FinalBoss > RocketFX > Rocket: Body + Flame) küçültülüp ayrı bir mermi prefab'ına kopyalanır,
    // EnemyData_FinalBoss'un mermisi yapılır; değince patlar (alan hasarı). Elit polis EnemyBullet'ta kalır.
    // Tekrar çalıştırmak güvenli.
    internal static class HeliRocketBulletSetup
    {
        private const string BossPrefabPath = "Assets/_Project/Prefabs/Enemies/Enemy_FinalBoss.prefab";
        private const string BulletPath = "Assets/_Project/Prefabs/Enemies/HeliRocketBullet.prefab";
        private const string BossDataPath = "Assets/_Project/ScriptableObjects/Enemies/EnemyData_FinalBoss.asset";
        private const float BulletScale = 0.42f; // şablon ~1.5 boyunda → ~0.6
        private const int ProjectileLayer = 18;
        private const float SplashRadius = 1.2f; // alan hasarı: blob yüzeyi patlamaya bu kadar yakınsa vurur

        [MenuItem("Tools/Blob.io/Helikopter Mermi Roketini Kur")]
        private static void Run()
        {
            GameObject boss = PrefabUtility.LoadPrefabContents(BossPrefabPath);
            try
            {
                Transform template = boss.transform.Find("RocketFX/Rocket");
                if (template == null)
                {
                    Debug.LogError("[HeliRocketBulletSetup] RocketFX/Rocket yok: önce Final Boss Helikopterini Uygula.");
                    return;
                }

                var root = new GameObject("HeliRocketBullet") { layer = ProjectileLayer };
                try
                {
                    root.AddComponent<EnemyProjectile>();
                    var rocket = Object.Instantiate(template.gameObject, root.transform);
                    rocket.name = "Rocket";
                    rocket.SetActive(true);
                    rocket.transform.localPosition = Vector3.zero;
                    rocket.transform.localRotation = Quaternion.identity;
                    rocket.transform.localScale = template.localScale * BulletScale;
                    foreach (Collider c in rocket.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                    foreach (Transform t in rocket.GetComponentsInChildren<Transform>(true))
                    {
                        t.gameObject.layer = ProjectileLayer;
                        t.gameObject.SetActive(true);
                    }
                    foreach (Renderer r in rocket.GetComponentsInChildren<Renderer>(true))
                    {
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        r.receiveShadows = false;
                    }

                    var so = new SerializedObject(root.GetComponent<EnemyProjectile>());
                    so.FindProperty("_hitRadius").floatValue = 0.25f;
                    so.FindProperty("_splashRadius").floatValue = SplashRadius;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    var flicker = root.AddComponent<RocketFlameFlicker>();
                    var fso = new SerializedObject(flicker);
                    fso.FindProperty("_flame").objectReferenceValue = rocket.transform.Find("Flame");
                    fso.ApplyModifiedPropertiesWithoutUndo();

                    GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, BulletPath);
                    var data = AssetDatabase.LoadAssetAtPath<EnemyData>(BossDataPath);
                    var dso = new SerializedObject(data);
                    dso.FindProperty("_projectilePrefab").objectReferenceValue = saved.GetComponent<EnemyProjectile>();
                    dso.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(data);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[HeliRocketBulletSetup] {BulletPath} → EnemyData_FinalBoss mermisi.");
                }
                finally { Object.DestroyImmediate(root); }
            }
            finally { PrefabUtility.UnloadPrefabContents(boss); }
        }
    }
}
