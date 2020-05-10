Shader "Instancing/BoidInstances"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue "= "Transparent" }
        LOD 100
        ZWrite On
Blend One Zero // Premultiplied transparency
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            
            #pragma multi_compile_local __ ISBILLBOARD


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
                float3 wPos : TEXCOORD1;
                float3 sphereWPos: TEXCOORD2;
                float3 rayD: TEXCOORD3;
                
            };

            float4x4 lookAtMatrix (float3 forward, float3 up){
                float3 z = normalize(forward);
                float3 x = normalize(cross(z,up));
                float3 y = normalize(cross (z,x));
        
                return float4x4(
                        x.x,y.x,z.x,0,
                        x.y,y.y,z.y,0,
                        x.z,y.z,z.z,0,
                        0,0,0,1
                );
            }   
            
            float _SphereRadius;
            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                BoidData boid = _BoidsBuffer[instanceID];
                #ifdef ISBILLBOARD
                float3 localSpaceCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1));            
                float3 camVect = normalize(boid.position - localSpaceCameraPos);
                float4x4 rot =lookAtMatrix( v.vertex  - localSpaceCameraPos,float3(0,1,0));
                #else
                float4x4 rot =lookAtMatrix( boid.velocity,float3(0,1,0));
                #endif
                v.vertex.xyz;
                v.vertex.xyz*=5;
                v.vertex  = mul(rot,v.vertex);
                v.vertex.xyz += boid.position;
                
                o.wPos = v.vertex;
                o.sphereWPos = boid.position;
                o.rayD = (o.wPos -_WorldSpaceCameraPos.xyz);
                o.color = boid.scale;
                o.vertex = mul(UNITY_MATRIX_VP,v.vertex);
                o.uv =v.uv;
                return o;
            }
           float sphere(float3 ray, float3 dir, float3 center, float radius)
            {
                float3 rc = ray-center;
                float c = dot(rc, rc) - (radius*radius);
                float b = dot(dir, rc);
                float d = b*b - c;
                float t = -b - sqrt(abs(d));
                float st = step(0.0, min(t,d));
                return lerp(-1.0, t, st);
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv =  i.uv *2 -1;
                float3 D = normalize(i.rayD);
                float d =  sphere(_WorldSpaceCameraPos.xyz,D,i.sphereWPos,_SphereRadius*i.color);
               
                if(d<0){
                discard;
                }
                float3 p = _WorldSpaceCameraPos.xyz+  D * d;
                float3 N = normalize( p -i.sphereWPos  );
                float f= smoothstep(0,1,1- length(uv));
                float3 L = _WorldSpaceLightPos0;
                
                return float4(N,1);
            }
            ENDCG
        }
    }
}
