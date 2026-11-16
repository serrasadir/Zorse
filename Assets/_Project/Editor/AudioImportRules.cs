using UnityEditor;
using UnityEngine;

namespace BlobSurvivor.EditorTools
{
    // Ses import kuralları (Resources/Audio altı). Ambience/ ve Music/: uzun döngüler, Vorbis + Streaming
    // (bellekte açılmaz, ~1 MB). Diğerleri kısa efekt: Vorbis + Decompress On Load (anında çalar).
    public class AudioImportRules : AssetPostprocessor
    {
        private const string Root = "Assets/_Project/Resources/Audio/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (AudioImporter)assetImporter;
            bool longLoop = assetPath.StartsWith(Root + "Ambience/") || assetPath.StartsWith(Root + "Music/");

            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = longLoop ? 0.5f : 0.7f;
            s.loadType = longLoop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.preloadAudioData = !longLoop;
            importer.defaultSampleSettings = s;
            importer.loadInBackground = longLoop;
        }
    }
}
