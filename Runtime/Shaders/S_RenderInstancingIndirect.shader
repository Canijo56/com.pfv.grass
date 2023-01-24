Shader "S_RenderInstancedIndirect_Buffer"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            // Render State
            Cull Off
            Blend One Zero
            ZTest Always
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma target 5.0
            #pragma exclude_renderers gles gles3 glcore

            #pragma instancing_options renderinglayer
            // #pragma enable_d3d11_debug_symbols 
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #include "UnityCG.cginc"


            
            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            #include "./IndirectStructs.hlsl"

            float4 _Color;
            uniform float4x4 _ObjectToWorld;

            void setup(){
                unity_ObjectToWorld = _ObjectToWorld;
            }

            // ConsumeStructuredBuffer<GrassBladeInstanceData> _GrassInstanceData;
            StructuredBuffer<GrassBladeInstanceData> _GrassInstanceData;
            // StructuredBuffer<DrawIndirectArgs> _DrawIndirectArgs;


            v2f vert(appdata_base v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                

                // uint maxInstances = _DrawIndirectArgs[0].instanceCount;
                // if (instanceID >= maxInstances)
                // {
                    // o.pos = mul(UNITY_MATRIX_VP, mul(_ObjectToWorld, v.vertex));
                    // return o;
                // }
                
                // GrassBladeInstanceData instance = _GrassInstanceData.Consume();
                GrassBladeInstanceData instance = _GrassInstanceData[instanceID];
                float4 wpos = mul (_ObjectToWorld, v.vertex);
                
                // float4 wpos = mul (_ObjectToWorld, instance.positionWS);
                // wpos.xyz += instance.positionWS + float3(0.1,0.0,0.0) * instanceID;
                wpos.xyz += instance.positionWS;
                
                // wpos.w = 1.0;
                // o.pos = mul(UNITY_MATRIX_VP, wpos);
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // return i.color;
                // return fixed4(0.0,1.0,0.0,1.0);
                return _Color;
            }
            ENDCG
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
            
            // Render State
            Cull Back
            ZTest LEqual
            ZWrite On
            ColorMask 0
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma enable_d3d11_debug_symbols 
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #include "UnityCG.cginc"
            
            #include "./IndirectStructs.hlsl"
            struct v2f { 
                float4 pos : SV_POSITION;
            };
            uniform float4x4 _ObjectToWorld;
            StructuredBuffer<GrassBladeInstanceData> _GrassInstanceData;

            v2f vert(appdata_base v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                GrassBladeInstanceData instance = _GrassInstanceData[instanceID];
                // float4 wpos = mul(mul (_ObjectToWorld, instance.positionWS), v.vertex);

                float4 wpos = mul (_ObjectToWorld, v.vertex);
                wpos.xyz += instance.positionWS;
                wpos.w = 1.0;
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return 0;
            }
            ENDCG
        }
        // shadow caster rendering pass, implemented manually
        // using macros from UnityCG.cginc
        // Pass
        // {
            //     Tags {"LightMode"="ShadowCaster"}

            //     Cull Off
            //     ZTest LEqual
            //     ZWrite On
            //     ColorMask 0
            
            //     CGPROGRAM
            //     #pragma vertex vert
            //     #pragma fragment frag
            //     #pragma multi_compile_shadowcaster
            //     #pragma multi_compile_instancing
            //     #pragma instancing_options procedural:setup
            //     #include "UnityCG.cginc"


            //     #include "./IndirectStructs.hlsl"

            //     struct v2f { 
                //         V2F_SHADOW_CASTER;
            //     };
            //     uniform float4x4 _ObjectToWorld;
            //     StructuredBuffer<GrassBladeInstanceData> _GrassInstanceData;

            //     v2f vert(appdata_base v, uint instanceID : SV_InstanceID)
            //     {
                //         v2f o;
                //         // float4 wpos = mul(_ObjectToWorld, v.vertex + float4(instanceID, cmdID, 0, 0));
                //         v.vertex = mul(mul (_ObjectToWorld,_GrassInstanceData[instanceID].positionWS), v.vertex);
                //         // o.pos = wpos;
                //         TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                //         return o;
            //     }

            //     float4 frag(v2f i) : SV_Target
            //     {
                //         SHADOW_CASTER_FRAGMENT(i)
            //     }
            //     ENDCG
        // }
    }
}