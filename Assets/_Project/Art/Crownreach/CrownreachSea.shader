// Crownreach denizi: kıyıya yakın hafif açık (sığ su), uzakta sayfanın deniz rengi. Köpük yok (basamaklı kıyıyı
// vurguluyordu). _ShoreTex (CrownreachMapBuilder.ShoreMask): R = kıyıya uzaklık (0..1 = 0..Reach birim, yumuşatılmış).
// Mobil: unlit, piksel başına tek doku okuması.
Shader "BlobSurvivor/CrownreachSea"
{
    Properties
    {
        _ShoreTex ("Kıyı mesafesi", 2D) = "white" {}
        _ShoreRect ("Kıyı alanı (x0, z0, 1/genişlik, 1/derinlik)", Vector) = (0, 0, 1, 1)
        _Deep ("Derin su", Color) = (0.29, 0.4, 0.396, 1)
        _Shallow ("Sığ su", Color) = (0.36, 0.5, 0.49, 1)
        _ShallowWidth ("Sığ su genişliği (0..1)", Range(0.05, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_ShoreTex); SAMPLER(sampler_ShoreTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _ShoreTex_ST;
                float4 _ShoreRect;
                half4 _Deep;
                half4 _Shallow;
                half _ShallowWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 world : TEXCOORD0; half fog : TEXCOORD1; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.world = p.positionWS.xz;
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                // Alanın dışı (sampler clamp) kenar pikselini okur: harita kenarı derin suya yazılır.
                half d = SAMPLE_TEXTURE2D(_ShoreTex, sampler_ShoreTex, (i.world - _ShoreRect.xy) * _ShoreRect.zw).r;
                half3 col = lerp(_Shallow.rgb, _Deep.rgb, smoothstep(0.0h, _ShallowWidth, d));
                return half4(MixFog(col, i.fog), 1.0h);
            }
            ENDHLSL
        }
    }
}
