using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlobSurvivor.EditorTools
{
    // Oyuncu mermilerine karaktere özel model + renkli iz (TrailRenderer). Modeller zorse-blender/build_bullets.py
    // (X_Bullet*.fbx, şehir paleti). Eski hali: 0.12'lik gri küre (Lit varsayılan), yolda görünmüyordu.
    // Tekrar çalıştırmak güvenli.
    internal static class BulletModelSetup
    {
        private const string PaletteMaterialPath = "Assets/_Project/Art/City/Mat_CityPalette.mat";
        private const string TrailMaterialPath = "Assets/_Project/Materials/Mat_BulletTrail.mat";
        private const float ModelScale = 0.85f;

        private struct Entry
        {
            public string Prefab, Mesh;
            public Color Trail;
            public float Spin;
        }

        private static readonly Entry[] Entries =
        {
            new Entry { Prefab = "Assets/_Project/Prefabs/Weapons/Cannon.prefab", Mesh = "X_BulletTopik", Trail = new Color(1f, 0.64f, 0.3f), Spin = 720f },
            new Entry { Prefab = "Assets/_Project/Prefabs/Weapons/MetalBall.prefab", Mesh = "X_BulletMiknato", Trail = new Color(0.55f, 0.82f, 1f), Spin = 540f },
            new Entry { Prefab = "Assets/_Project/Prefabs/Weapons/Pistol.prefab", Mesh = "X_BulletMermo", Trail = new Color(1f, 0.85f, 0.35f), Spin = 0f },
        };

        [MenuItem("Tools/Blob.io/Mermi Modellerini Uygula")]
        private static void Setup()
        {
            Material palette = AssetDatabase.LoadAssetAtPath<Material>(PaletteMaterialPath);
            if (palette == null)
            {
                Debug.LogError($"[BulletModelSetup] {PaletteMaterialPath} yok.");
                return;
            }
            Material trailMat = EnsureTrailMaterial();

            int done = 0;
            foreach (Entry e in Entries)
            {
                MeshFilter source = CityKitPipeline.LoadSourceMesh(e.Mesh);
                if (source == null) continue;

                GameObject root = PrefabUtility.LoadPrefabContents(e.Prefab);
                root.transform.localScale = Vector3.one;
                // Eski gri küre kökteydi.
                var oldRenderer = root.GetComponent<MeshRenderer>();
                if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);
                var oldFilter = root.GetComponent<MeshFilter>();
                if (oldFilter != null) Object.DestroyImmediate(oldFilter);
                var oldCollider = root.GetComponent<Collider>();
                if (oldCollider != null) Object.DestroyImmediate(oldCollider); // mermi spatial sorgu kullanır
                foreach (string old in new[] { "Model", "Spin" })
                {
                    for (Transform t = root.transform.Find(old); t != null; t = root.transform.Find(old))
                        Object.DestroyImmediate(t.gameObject);
                }

                var model = new GameObject("Model");
                model.transform.SetParent(root.transform, false);
                model.transform.localRotation = source.transform.localRotation;
                model.transform.localScale = source.transform.localScale * ModelScale;
                if (TipPointsBackward(source.sharedMesh, model.transform.localRotation))
                    model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.localRotation;
                model.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                var mr = model.AddComponent<MeshRenderer>();
                mr.sharedMaterial = palette;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = LightProbeUsage.Off;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

                // Dönme pivotu: model mesh'i FBX'in kendi dönüşünü taşır, spin ayrı boş objede.
                var spin = new GameObject("Spin").transform;
                spin.SetParent(root.transform, false);
                model.transform.SetParent(spin, false);

                var trail = root.GetComponent<TrailRenderer>();
                if (trail == null) trail = root.AddComponent<TrailRenderer>();
                trail.sharedMaterial = trailMat;
                trail.time = 0.14f;
                trail.minVertexDistance = 0.12f;
                trail.widthMultiplier = 1f;
                trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.17f), new Keyframe(1f, 0f));
                var grad = new Gradient();
                grad.SetKeys(
                    new[] { new GradientColorKey(Color.Lerp(e.Trail, Color.white, 0.35f), 0f), new GradientColorKey(e.Trail, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                trail.colorGradient = grad;
                trail.alignment = LineAlignment.View;
                trail.numCapVertices = 0;
                trail.numCornerVertices = 0;
                trail.shadowCastingMode = ShadowCastingMode.Off;
                trail.receiveShadows = false;
                trail.autodestruct = false;
                trail.emitting = true;

                var projectile = root.GetComponent<BlobSurvivor.Entities.Weapons.Projectile>();
                if (projectile != null)
                {
                    var so = new SerializedObject(projectile);
                    so.FindProperty("_spinModel").objectReferenceValue = spin;
                    so.FindProperty("_spinSpeed").floatValue = e.Spin;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, e.Prefab);
                PrefabUtility.UnloadPrefabContents(root);
                done++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[BulletModelSetup] Mermi modeli + iz kuruldu: {done}/{Entries.Length} prefab.");
        }

        // Sivri uç uçuş yönüne (+Z) bakmalı: dar uç (en uçtaki köşelerin eksene uzaklığı küçük) -Z'deyse true.
        private static bool TipPointsBackward(Mesh mesh, Quaternion rotation)
        {
            Vector3[] v = mesh.vertices;
            float minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < v.Length; i++)
            {
                float z = (rotation * v[i]).z;
                minZ = Mathf.Min(minZ, z);
                maxZ = Mathf.Max(maxZ, z);
            }
            float band = (maxZ - minZ) * 0.05f, rFront = 0f, rBack = 0f;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 p = rotation * v[i];
                float r = new Vector2(p.x, p.y).magnitude;
                if (p.z >= maxZ - band) rFront = Mathf.Max(rFront, r);
                if (p.z <= minZ + band) rBack = Mathf.Max(rBack, r);
            }
            return rFront > rBack * 1.2f;
        }

        // Opak, köşe rengini kullanan unlit: iz incelerek biter, saydamlık gerekmez (mobilde ucuz).
        private static Material EnsureTrailMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(TrailMaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(mat, TrailMaterialPath);
            }
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
