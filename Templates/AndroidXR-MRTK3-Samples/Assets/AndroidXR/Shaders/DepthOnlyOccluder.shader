// 奥行きだけを書き込み、色は描かない（ColorMask 0）。
// 空間認識のメッシュをこれで描くと、現実の物（床・壁・机など）の後ろにある仮想物が隠れる。
// 背景のアルファ 0 はそのまま残るので、パススルーの映像は見えたまま。
//
// Writes depth only (no color). Render the spatial (scene) mesh with this so that virtual objects
// behind real-world surfaces are hidden. Works with any material on the virtual objects.
Shader "Android XR/Depth Only Occluder"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            // 仮想物より先に描いて、奥行きを書いておく
            "Queue" = "Geometry-1"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            return output;
        }

        half4 Frag(Varyings input) : SV_Target
        {
            return 0;
        }
        ENDHLSL

        Pass
        {
            Name "DepthOnlyOccluder"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
