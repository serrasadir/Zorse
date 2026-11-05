using UnityEditor;

namespace BlobSurvivor.EditorTools
{
    // Art/UI altındaki dokular arayüz görseli: Sprite, şeffaf, mipmap yok (portreler: build_blobs.py; Resources/UI: Zorse açılış logosu).
    internal class UITextureImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/_Project/Art/UI/") && !assetPath.StartsWith("Assets/_Project/Resources/UI/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = assetPath.Contains("/Resources/UI/") ? 1024 : 512; // logo büyük
        }
    }
}
