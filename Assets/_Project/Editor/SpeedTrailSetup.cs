using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlobSurvivor.EditorTools
{
    // Topik'in rüzgar izi (SpeedTrail) için saydam, köşe rengini kullanan materyal → Resources/FX/SpeedTrail.
    // Iz rengi/alfası TrailRenderer gradyanından gelir. Tekrar çalıştırmak güvenli.
    internal static class SpeedTrailSetup
    {
        private const string Folder = "Assets/_Project/Resources/FX/SpeedTrail";

        [MenuItem("Tools/Blob.io/Rüzgar İzini Kur")]
        private static void Setup()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/_Project/Resources/FX", "SpeedTrail");

            string path = $"{Folder}/Mat_SpeedTrail.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            Debug.Log("[SpeedTrailSetup] Rüzgar izi materyali kuruldu → Resources/FX/SpeedTrail.");
        }
    }
}
