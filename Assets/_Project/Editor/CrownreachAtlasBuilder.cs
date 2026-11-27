using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Crownreach;

namespace BlobSurvivor.EditorTools
{
    public static class CrownreachAtlasBuilder
    {
        const string Art = "Assets/_Project/Art/Crownreach/";
        const string ScenePath = "Assets/_Project/Scenes/CrownreachAtlas.unity";
        static readonly string[] Models = {"01_Farmland","02_WalledTown","03_Garrison","04_RoyalCastle"};

        [MenuItem("Tools/Blob.io/Crownreach Atlas/Open Scene")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
            else Create();
        }

        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new InvalidOperationException("CrownreachAtlas already exists.");
            AssetDatabase.Refresh();
            string texturePath = Art+"AtlasPalette.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Art+"AtlasPalette.mat");
            if (!mat)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
                mat.SetFloat("_Smoothness",0);
                AssetDatabase.CreateAsset(mat,Art+"AtlasPalette.mat");
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var regions = new Transform[4];
            int meshCount = 0;
            for (int i=0;i<Models.Length;i++)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(Art+Models[i]+".fbx");
                if (!model) throw new InvalidOperationException("Model import failed: "+Models[i]);
                var group = (GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.UnpackPrefabInstance(group,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                regions[i] = group.transform;
                var renderers = group.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length<10) throw new InvalidOperationException("District geometry missing.");
                foreach (var r in renderers) r.sharedMaterial = mat;
                meshCount += renderers.Length;
            }
            var sun = new GameObject("Afternoon sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1,.94f,.82f);
            sun.intensity = 1.65f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .65f;
            sun.transform.rotation = Quaternion.Euler(49,-32,0);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.78f,.84f,.87f);
            RenderSettings.ambientEquatorColor = new Color(.63f,.67f,.60f);
            RenderSettings.ambientGroundColor = new Color(.42f,.39f,.32f);
            var camera = new GameObject("Atlas Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.255f,.353f,.357f);
            camera.farClipPlane = 400;
            var rig = camera.gameObject.AddComponent<CrownreachCamera>();
            rig.districts = regions;
            rig.Frame(0);
            if (!EditorSceneManager.SaveScene(scene,ScenePath)) throw new IOException("Scene save failed");
            AssetDatabase.SaveAssets();
            Debug.Log("CROWNREACH_SCENE_OK meshes="+meshCount+" orthoSize="+camera.orthographicSize+" path="+ScenePath);
        }
    }
}
