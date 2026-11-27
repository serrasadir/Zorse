// Tek yüzeyde çimen / toprak / arnavut / kaldırım (Crownreach zemini). _Control (CrownreachMapBuilder.BuildGroundControl):
// R toprak, G arnavut, B kaldırım, A gölge; çimen = kalan. Geçişte doku parlaklığıyla karışır: çimen tutamları toprağa
// taşar, kenar dağınık. Mobil: ağırlığı sıfır olan doku okunmaz (bölge içinde kontrol + 1 doku), normal map yok,
// ışık Lambert + SH + gölge (URP Lit'ten ucuz).
Shader "BlobSurvivor/GroundSplat"
{
    Properties
    {
        _Control ("Karışım haritası", 2D) = "black" {}
        _CtrlRect ("Karışım alanı (x0, z0, 1/genişlik, 1/derinlik)", Vector) = (0, 0, 1, 1)
        _GrassTex ("Çimen", 2D) = "white" {}
        _DirtTex ("Toprak", 2D) = "white" {}
        _CobbleTex ("Arnavut kaldırımı (alfa = taş)", 2D) = "white" {}
        _FlagTex ("Kaldırım taşı", 2D) = "white" {}
        _GrassTile ("Çimen tekrarı (birim)", Float) = 4
        _DirtTile ("Toprak tekrarı (birim)", Float) = 7
        _StoneTile ("Kaldırım tekrarı (birim)", Float) = 4
        _CobbleTile ("Arnavut tekrarı (birim)", Float) = 2.5
        _DirtTint ("Toprak rengi", Color) = (1, 1, 1, 1)
        _DirtSaturation ("Toprak doygunluğu", Range(0, 1)) = 0.5
        _BlendDepth ("Geçiş yumuşaklığı", Range(0.02, 1)) = 0.1
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

        CBUFFER_START(UnityPerMaterial)
            float4 _Control_ST;
            float4 _CtrlRect;
            float4 _GrassTex_ST;
            float4 _DirtTex_ST;
            float4 _CobbleTex_ST;
            float4 _FlagTex_ST;
            float _GrassTile;
            float _DirtTile;
            float _StoneTile;
            float _CobbleTile;
            half4 _DirtTint;
            half _DirtSaturation;
            half _BlendDepth;
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
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
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
                half4 ctrl = saturate(SAMPLE_TEXTURE2D(_Control, sampler_Control, (world - _CtrlRect.xy) * _CtrlRect.zw));
                half wd = ctrl.r, wc = ctrl.g, wf = ctrl.b;
                half wg = saturate(1.0h - wd - wc - wf);

                // Dallanma içinde mip için türev dışarıda alınır (tek tekrarın türevi, diğerleri ölçeklenir).
                float2 uv = world;
                float2 dx = ddx(uv), dy = ddy(uv);
                const half off = -10.0h; // okunmayan dokunun yüksekliği: hiç kazanamaz
                half3 g = 0, d = 0, c = 0, f = 0;
                half hg = off, hd = off, hc = off, hf = off;
                if (wg > 0.001h)
                {
                    float k = 1.0 / _GrassTile;
                    g = SAMPLE_TEXTURE2D_GRAD(_GrassTex, sampler_GrassTex, uv * k, dx * k, dy * k).rgb;
                    hg = wg + 0.05h + Lum(g) * 0.45h;
                }
                if (wd > 0.001h || wc > 0.001h)
                {
                    float k = 1.0 / _DirtTile;
                    d = SAMPLE_TEXTURE2D_GRAD(_DirtTex, sampler_GrassTex, uv * k, dx * k, dy * k).rgb;
                    d = lerp(Lum(d).xxx, d, _DirtSaturation) * _DirtTint.rgb;
                    hd = wd + Lum(d) * 0.15h;
                }
                if (wc > 0.001h)
                {
                    // Arnavut: taşların arası grimsi koyu toprak (alfa = taş); düşük kontrast, yenebilirler okunsun
                    float k = 1.0 / _CobbleTile;
                    half4 c4 = SAMPLE_TEXTURE2D_GRAD(_CobbleTex, sampler_GrassTex, uv * k, dx * k, dy * k);
                    c = lerp(lerp(Lum(d).xxx, d, 0.4h) * 0.78h, c4.rgb, c4.a);
                    hc = wc + c4.a * 0.6h;
                }
                if (wf > 0.001h)
                {
                    float k = 1.0 / _StoneTile;
                    f = SAMPLE_TEXTURE2D_GRAD(_FlagTex, sampler_GrassTex, uv * k, dx * k, dy * k).rgb;
                    hf = wf + Lum(f) * 0.5h;
                }

                // Yükseklikle karışım: ağırlık + doku parlaklığı; en yüksekten _BlendDepth aşağısı görünür.
                half top = max(max(hg, hd), max(hc, hf)) - _BlendDepth;
                half bg = max(hg - top, 0.0h), bd = max(hd - top, 0.0h), bc = max(hc - top, 0.0h), bf = max(hf - top, 0.0h);
                half3 albedo = (g * bg + d * bd + c * bc + f * bf) / max(bg + bd + bc + bf, 1e-4h);

                half ao = ctrl.a;
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 direct = sun.color * (saturate(sun.direction.y) * sun.shadowAttenuation * sun.distanceAttenuation);
                half3 col = albedo * (direct + SampleSH(half3(0, 1, 0))) * ao;
                return half4(MixFog(max(col, 0.0h), i.fog), 1.0h);
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

        // PC renderer'ın SSAO'su için derinlik + normal (zemin düz: +Y)
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

            half4 DNFrag() : SV_Target { return half4(0.0h, 1.0h, 0.0h, 0.0h); }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Simple Lit"
}
