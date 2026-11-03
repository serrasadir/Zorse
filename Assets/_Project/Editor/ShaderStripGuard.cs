using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlobSurvivor.EditorTools
{
    // 2026-11-03 (build 5, Serra: "telefonda gri her yer, sadece UI var"): URP'nin shader ayıklayıcısı
    // Lit'in ForwardLit geçişini ve UberPost'u tamamen silmişti ("After scriptable stripping: 0") —
    // cihazda hiçbir 3D obje çizilmiyordu, sadece gökyüzü + UI. Ayarlar doğruydu, sebep build anındaki
    // geçici bir durum. Bu bekçi tüm ayıklayıcılardan SONRA çalışır (callbackOrder en yüksek) ve
    // ForwardLit'ten hiç varyant kalmadıysa build'i hata ile durdurur — bozuk build TestFlight'a gitmesin.
    public class ShaderStripGuard : IPreprocessShaders, IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string ForwardPass = "ForwardLit";

        private static bool _sawLit;
        private static int _forwardLitVariants;

        public int callbackOrder => int.MaxValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            _sawLit = false;
            _forwardLitVariants = 0;
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader == null || shader.name != LitShader) return;
            _sawLit = true;
            if (snippet.passName == ForwardPass) _forwardLitVariants += data.Count;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Lit hiç işlenmediyse (ör. sahnede Lit materyali yok) karar verecek veri yok.
            if (!_sawLit) return;
            if (_forwardLitVariants > 0)
            {
                Debug.Log($"[ShaderStripGuard] OK: {LitShader}/{ForwardPass} {_forwardLitVariants} varyant");
                return;
            }

            throw new BuildFailedException(
                $"[ShaderStripGuard] {LitShader} shader'ının {ForwardPass} geçişi tamamen ayıklandı — " +
                "cihazda 3D hiçbir şey çizilmez. Unity'yi yeniden başlatıp build'i tekrar al.");
        }
    }
}
