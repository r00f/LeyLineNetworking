Shader "HDRP/PostFX/HighlightPassDrawRenderers"
{
    Properties
    {
        _MaxDistance("Max Distance", float) = 15
        _LerpMaxDistance("Lerp Max Distance", float) = 20
    }
 
    HLSLINCLUDE
    

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    #pragma multi_compile _ DOTS_INSTANCING_ON
 
    // #pragma enable_d3d11_debug_symbols
 
    //enable GPU instancing support
    #pragma multi_compile_instancing
    #pragma instancing_options renderinglayer
 
    ENDHLSL
 
    SubShader
    {
        Pass
        {
            Name "FirstPass"
            Tags { "LightMode" = "FirstPass" }
 
            Blend Off
            ZWrite On
            ZTest LEqual
 
            Cull Back
 
            HLSLPROGRAM

 
            // Toggle the alpha test
            #define _ALPHATEST_ON
 
            // Toggle transparency
            // #define _SURFACE_TYPE_TRANSPARENT
 
            // Toggle fog on transparent
            #define _ENABLE_FOG_ON_TRANSPARENT
       
            // List all the attributes needed in your shader (will be passed to the vertex shader)
            // you can see the complete list of these attributes in VaryingMesh.hlsl
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
 
            // List all the varyings needed in your fragment shader
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TANGENT_TO_WORLD
            #define VARYINGS_NEED_POSITION_WS
       
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderers.hlsl"

            CBUFFER_START(UnityPerMaterial)

            float _MaxDistance;
            float _LerpMaxDistance;

            CBUFFER_END
       
            float invLerp(float A, float B, float T)
            {
                return (T - A) / (B - A);
            }
 
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                float dist = length(fragInputs.positionRWS);
       
                if (dist > _LerpMaxDistance) {
                    discard;
                }
           
                float percentage = invLerp(_MaxDistance, _LerpMaxDistance, dist);
                percentage = clamp (percentage, 0, 1);
           
                // Write back the data to the output structures
                ZERO_INITIALIZE(BuiltinData, builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = lerp(1, 0, percentage);
                builtinData.emissiveColor = float3(0, 0, 0);
                surfaceData.color = float3(1,1,1);
            }
 
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"
 
            #pragma vertex Vert
            #pragma fragment Frag
 
            ENDHLSL
        }
    }
}
