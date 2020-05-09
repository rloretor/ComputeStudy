Shader "Instancing/BoidInstances"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 4.5


            #include "UnityCG.cginc"
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"
            
            struct BoidData{
                float3 position;
                float scale;
                float3 velocity;
                float dummy;
            };
            
            StructuredBuffer<BoidData> _BoidsBuffer;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 normal:NORMAL;
               
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color :COLOR0;
            };

float4x4 lookAtMatrix (float3 forward, float3 up){
        float3 z = normalize(forward);
        float3 x = normalize(cross(z,up));
        float3 y = cross (z,x);
        
        return float4x4(
                x.x,y.x,z.x,0,
                x.y,y.y,z.y,0,
                x.z,y.z,z.z,0,
                0,0,0,1
        );
}

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                BoidData boid = _BoidsBuffer[instanceID];
                float4x4 rot =lookAtMatrix( boid.velocity,float3(0,1,0));
                v.vertex.xyz*=boid.scale;
                v.vertex.w =1;
                v.vertex  = mul(rot,v.vertex);
                v.vertex.xyz += boid.position;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv =v.uv;
                o.color = boid.dummy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
