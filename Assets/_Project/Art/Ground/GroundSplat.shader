// Kale zemini (Bahar, 2026-11-21: "kusursuz daire, keskin geçiş kötü"): tek yüzeyde çimen / toprak / arnavut / kaldırım.
// _Control (tools/medieval/ground_splat.py): R toprak, G arnavut, B kaldırım, A gölge; çimen = kalan. Geçişte yükseklikle
// karışır (doku parlaklığı = yükseklik): taşın tepesi çimenin üstünde kalır, derzlerden çimen/toprak sızar.
// Mobil: 4 renk + 2 normal + 1 kontrol örneği. Işık PaletteAO ile aynı (güneş + gölge + ortam + sis).
Shader "BlobSurvivor/GroundSplat"
{
    Properties
    {
        _Control ("Karışım haritası", 2D) = "black" {}
        _CtrlRect ("Karışım alanı (x0, z0, 1/genişlik, 1/derinlik)", Vector) = (0, 0, 1, 1)
        _GrassTex ("Çimen", 2D) = "white" {}
        _DirtTex ("Toprak", 2D) = "white" {}
        _CobbleTex ("Arnavut kaldırımı", 2D) = "white" {}
        _FlagTex ("Kaldırım taşı", 2D) = "white" {}
        [Normal] _CobbleN ("Arnavut normal", 2D) = "bump" {}
        [Normal] _FlagN ("Kaldırım normal", 2D) = "bump" {}
        _DirtTint ("Toprak rengi", Color) = (1, 1, 1, 1)
        _Tile ("Doku tekrarı (birim)", Float) = 4
        _BlendDepth ("Geçiş yumuşaklığı", Range(0.02, 1)) = 0.1
        _BumpScale ("Normal gücü", Range(0, 2)) = 1
        _Stone3D ("Taşlar 3D (zeminde taş yerine gölgeli çimen)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_Control); SAMPLER(sampler_Control);
        TEXTURE2D(_GrassTex); SAMPLER(sampler_GrassTex);
        TEXTURE2D(_DirtTex);
        TEXTURE2D(_CobbleTex);
        TEXTURE2D(_FlagTex);
        TEXTURE2D(_CobbleN); SAMPLER(sampler_CobbleN);
        TEXTURE2D(_FlagN);

        CBUFFER_START(UnityPerMaterial)
            float4 _Control_ST;
            float4 _CtrlRect;
            float4 _GrassTex_ST;
            float4 _DirtTex_ST;
            float4 _CobbleTex_ST;
            float4 _FlagTex_ST;
            float4 _CobbleN_ST;
            float4 _FlagN_ST;
            half4 _DirtTint;
            float _Tile;
            half _BlendDepth;
            half _BumpScale;
            half _Stone3D;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fog : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half Lum(half3 c) { return dot(c, half3(0.3h, 0.59h, 0.11h)); }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 world = i.positionWS.xz;
                float2 uvCtrl = (world - _CtrlRect.xy) * _CtrlRect.zw;
                half4 ctrl = saturate(SAMPLE_TEXTURE2D(_Control, sampler_Control, uvCtrl));
                float2 uv = world / _Tile;

                half3 g = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, uv).rgb;
                half3 d = SAMPLE_TEXTURE2D(_DirtTex, sampler_GrassTex, uv).rgb * _DirtTint.rgb;
                // Taşlı zemin: dokunun alfası = taş (Bahar: taş dokusunun kendi çimeni haritanın çimeniyle uyuşmuyordu).
                // Taşların arası haritanın çimeni; taşlar tek tek çimenin üstüne konmuş gibi.
                half4 c4 = SAMPLE_TEXTURE2D(_CobbleTex, sampler_GrassTex, uv);
                half stoneA = c4.a * (1.0h - _Stone3D);
                half3 c = lerp(g * lerp(1.0h, 0.82h, _Stone3D), c4.rgb, stoneA);   // 3D taşların altı: hafif gölgeli çimen
                half3 f = SAMPLE_TEXTURE2D(_FlagTex, sampler_GrassTex, uv).rgb;

                // Yükseklikle karışım: ağırlık + doku yüksekliği; en yüksekten _BlendDepth aşağısı görünür.
                half wd = ctrl.r, wc = ctrl.g, wf = ctrl.b;
                half wg = saturate(1.0h - wd - wc - wf);
                // Çimen tutamları (parlak) toprağın içine taşar, toprak çimen diplerinden görünür: yumuşak, dağınık kenar
                half hg = wg + 0.05h + Lum(g) * 0.45h;
                half hd = wd + Lum(d) * 0.15h;
                half hc = wc + stoneA * 0.6h;
                half hf = wf + Lum(f) * 0.5h;
                half top = max(max(hg, hd), max(hc, hf)) - _BlendDepth;
                half bg = max(hg - top, 0.0h), bd = max(hd - top, 0.0h), bc = max(hc - top, 0.0h), bf = max(hf - top, 0.0h);
                half inv = 1.0h / max(bg + bd + bc + bf, 1e-4h);
                bg *= inv; bd *= inv; bc *= inv; bf *= inv;
                half3 albedo = g * bg + d * bd + c * bc + f * bf;

                half3 n = half3(0, 1, 0);
                if (bc + bf > 0.01h)
                {
                    half3 nc = UnpackNormalScale(SAMPLE_TEXTURE2D(_CobbleN, sampler_CobbleN, uv), _BumpScale * stoneA);
                    half3 nf = UnpackNormalScale(SAMPLE_TEXTURE2D(_FlagN, sampler_CobbleN, uv), _BumpScale);
                    half3 nts = half3(SafeNormalize(float3(half3(0, 0, 1) * (bg + bd) + nc * bc + nf * bf)));
                    // Düz zemin: teğet = +X, bitangent = +Z, normal = +Y
                    n = half3(SafeNormalize(float3(nts.x, nts.z, nts.y)));
                }

                half ao = ctrl.a;
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(n, sun.direction));
                half3 direct = sun.color * (ndl * sun.shadowAttenuation * sun.distanceAttenuation);
                half3 ambient = SampleSH(n);
                half3 col = albedo * (direct + ambient * ao) * lerp(1.0h, ao, 0.5h);
                col = max(col, 0.0h);
                return half4(MixFog(col, i.fog), 1.0h);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 DepthVert(DepthAttributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half DepthFrag() : SV_Target { return 0; }
            ENDHLSL
        }
        // SSAO vb. için derinlik + normal (zemin düz: +Y)
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex DNVert
            #pragma fragment DNFrag
            #pragma multi_compile_instancing

            struct DNAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 DNVert(DNAttributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half4 DNFrag() : SV_Target
            {
                return half4(0.0h, 1.0h, 0.0h, 0.0h);
            }
            ENDHLSL
        }

    }

    FallBack "Universal Render Pipeline/Simple Lit"
}
