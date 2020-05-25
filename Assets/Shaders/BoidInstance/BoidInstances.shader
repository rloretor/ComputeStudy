Shader "Instancing/BoidInstances"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"}
        LOD 100
        ZWrite On
Blend One Zero // Premultiplied transparency
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma  multi_compile_fwdbase
            #pragma multi_compile_local __ ISBILLBOARD


            #include "UnityCG.cginc"
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"
            
            #define E 0.0000001

            
            struct BoidData{
                float3 position;
                float scale;
                float3 velocity;
                float dummy;
            };

            StructuredBuffer<BoidData> _BoidsBuffer;
            float _SphereRadius;
            uint _Instances;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal:NORMAL;
                float2 uv : TEXCOORD0;
               
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color :COLOR0;
                float3 wPos : TEXCOORD1;
                float3 sphereWPos: TEXCOORD2;
                float3 rayD: TEXCOORD3;
                float3 N:TEXCOORD4;
                float2 uv : TEXCOORD0;
                
            };
            
            struct f2s
            {
                float4 color : SV_Target;
                float depth : SV_Depth;
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
            float3 hash3( float3 p ){
                float3 q = float3(  dot(p,float3(127.1,311.7,234.916)), 
                			        dot(p,float3(269.5,183.3,511.5234)), 
                			        dot(p,float3(419.2,371.9,8732.324998)) );
                return frac(sin(q)*43758.5453);
            }
            
            float3 pal( in float t, in float3 a, in float3 b, in float3 c, in float3 d )
            {
                return a + b*cos( 6.28318*(c*t+d) );
            }
            struct Ellipsoid
            {
                float3 cen;
                float3 rad;
            };
            

            //https://www.shadertoy.com/view/ll2GD3
            #define BOIDPALETTE(p) pal( p,float3(0.5,0.5,0.5),float3(0.5,0.5,0.5),float3(1.0,1.0,0.5),float3(0.8,0.90,0.30) )
            #define Rot(a)  float2x2(cos(a), sin(a),-sin(a), cos(a))
            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                BoidData boid = _BoidsBuffer[instanceID];
               
                #ifdef ISBILLBOARD
                 float3 localSpaceCameraPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1));            
                 float3 camVect = normalize(boid.position - localSpaceCameraPos);
                 float4x4 rot =lookAtMatrix( v.vertex  - localSpaceCameraPos,float3(0,1,0));
                 v.vertex.xyz*=(_SphereRadius*boid.scale+10);
                 #else         
                 float4x4 rot =lookAtMatrix( boid.velocity,float3(0,1,0));
                 //  v.vertex.xy = mul(Rot(lerp(-45,45,boid.dummy)*0.01745329252),v.vertex.xy);
                 o.N =  mul(rot,v.normal);
                 v.vertex.xyz*=(_SphereRadius*boid.scale);
                #endif
                v.vertex  = mul(rot,v.vertex);
                v.vertex.xyz += boid.position;
                
                o.wPos = v.vertex;
                o.sphereWPos = boid.position;
                o.rayD = (o.wPos -_WorldSpaceCameraPos.xyz);
                o.color = float4(boid.velocity,boid.scale.r);
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
            
           float ellipsoid( float3 ro, float3 rd,  Ellipsoid sph )
            {
                float3 oc = ro - sph.cen;
                
                float3 ocn = oc / sph.rad;
                float3 rdn = rd / sph.rad;
                
                float a = dot( rdn, rdn );
            	float b = dot( ocn, rdn );
            	float c = dot( ocn, ocn );
            	float h = b*b - a*(c-1.0);
            	if( h<0.0 ) return -1.0;
            	return (-b - sqrt( h ))/a;
            }
            
            float3 SafeNormalize(float3 v){
                float d = dot(v,v);
                return d<=E?0: v/sqrt(d);
            }
            f2s frag (v2f i) : SV_Target
            {
                f2s o;
                float2 uv =  i.uv *2 -1;
                float3 N = normalize(i.N);
                  #ifdef ISBILLBOARD
                    float3 D = normalize(i.rayD);
                    Ellipsoid e;
                    e.cen =i.sphereWPos;
                    e.rad = abs(SafeNormalize(i.color.xyz))*i.color.w;
                    float d =  sphere(_WorldSpaceCameraPos.xyz,D,i.sphereWPos,(_SphereRadius*i.color.w)/2);
                    //float d =  ellipsoid( _WorldSpaceCameraPos.xyz
                    //                    , D
                    //                    ,e);
                    clip(d);
                    float4 p = float4(_WorldSpaceCameraPos.xyz+  D * d,1);
                    N = normalize( p -i.sphereWPos);
                    p = mul(UNITY_MATRIX_VP,float4(p.xyz,1));
                    o.depth = p.z/p.w;
                #endif
                
                float3 L = _WorldSpaceLightPos0;
                o.color=   float4(N,1);//dot(N,L) ;
                return o;
            }
            ENDCG
        }
    }
}
