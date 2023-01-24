Shader "S_RenderIndirect_Buffer"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            uniform float4x4 _ObjectToWorld;
            StructuredBuffer<float4x4> _positionsBuffer;

            // #define MAX_SIZE_POSITIONS 1023
            // float4x4  _PositionsArray[MAX_SIZE_POSITIONS];

            v2f vert(appdata_base v, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                v2f o;
                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);
                float4 wpos = mul(mul (_ObjectToWorld,_positionsBuffer[instanceID]), v.vertex);
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                // o.color = float4(cmdID & 1 ? 0.0f : 1.0f, cmdID & 1 ? 1.0f : 0.0f, instanceID / float(GetIndirectInstanceCount()), 0.0f);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // return i.color;
                return i.pos;
            }
            ENDCG
        }
        // shadow caster rendering pass, implemented manually
        // using macros from UnityCG.cginc
        Pass
        {
            Tags {"LightMode"="ShadowCaster"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct v2f { 
                V2F_SHADOW_CASTER;
            };
            uniform float4x4 _ObjectToWorld;
            StructuredBuffer<float4x4> _positionsBuffer;

            v2f vert(appdata_base v, uint svInstanceID : SV_InstanceID)
            {
                
                InitIndirectDrawArgs(0);
                v2f o;
                uint cmdID = GetCommandID(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);
                // float4 wpos = mul(_ObjectToWorld, v.vertex + float4(instanceID, cmdID, 0, 0));
                v.vertex = mul(mul (_ObjectToWorld,_positionsBuffer[instanceID]), v.vertex);
                // o.pos = wpos;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}