Shader "ScreenSpace/Bilateral"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        

       SIGMA("SIGMA",Float) = 10
       BSIGMA("BSIGMA",Float) = 0.1
       AUX("AUX",Float) = 0.1
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            float4 _MainTex_ST;

            uniform float SIGMA;
            uniform float BSIGMA;
            

            
            //https://www.shadertoy.com/view/4dfGDH
            float normpdf(in float x, in float sigma)
            {
            	return 0.39894*exp(-0.5*x*x/(sigma*sigma))/sigma;
            }
            
            float normpdf3(in float3 v, in float sigma)
            {
            	return 0.39894*exp(-0.5*dot(v,v)/(sigma*sigma))/sigma;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            #define MSIZE 15
            fixed4 frag (v2f im) : SV_Target
            {
                //fixed4 col = tex2D(_CameraDepthTexture, i.uv);
                //float depth = Linear01Depth(tex2D(_CameraDepthTexture, i.uv).r);
                float depth = tex2D(_CameraDepthTexture, im.uv).r;
                const int kSize = (MSIZE-1)/2;
		        float kernel[MSIZE];
                
                float Z = 0.0;
		        for (int j = 0; j <= kSize; ++j)
		        {
		        	kernel[kSize+j] = kernel[kSize-j] = normpdf(float(j), SIGMA);
		        }
		        float cc;
	            float factor;
	            float bZ = 1.0/normpdf(0.0, BSIGMA);
	            float final =0;
	            //read out the texels
	            for (int i=-kSize; i <= kSize; ++i)
	            {
	            	for (int j=-kSize; j <= kSize; ++j)
	            	{
	            		cc = tex2D(_CameraDepthTexture,
	            		            im.uv +(float2(float(i),float(j)) / _ScreenParams.xy)).r;
	            		factor = normpdf3(cc-depth, BSIGMA)*bZ*kernel[kSize+j]*kernel[kSize+i];
	            		Z += factor;
	            		final += factor*cc;

	            	}
	            }
	                    
		        return (depth);
            }
            ENDCG
        }
        
                
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldDirection : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };


            sampler2D _MainTex;
            float4 _MainTex_ST;
            uniform float AUX;
            
                float4x4 _InverseView;

          
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldDirection = mul(unity_ObjectToWorld, v.vertex).xyz - _WorldSpaceCameraPos;
                o.screenPosition = o.vertex;

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            //float3 getWpos(float2 p11_22, float2 p13_31, float2 uv){
            //
            //    const float near = _ProjectionParams.y;
            //    const float far = _ProjectionParams.z;
            //    
            //    float d =tex2D(_MainTex, uv).r;
            //         #if defined(UNITY_REVERSED_Z)
            //                 d = 1 - d;
            //         #endif
            //    float zPers = near * far / lerp(far, near, d);
            //    float3 vpos = float3((uv * 2 - 1 - p13_31) / p11_22 * lerp(vz, 1, isOrtho), -vz);
            //    return   mul(_InverseView, float4(vpos, 1));
            //}
//
            float getVPos(float2 uv){
                float3 rayD =  float3(uv*2-1,-1);
                rayD = normalize(rayD);
                float depth = LinearEyeDepth(tex2D(_MainTex, uv).r);
                return  rayD * depth ;
            }
            
            fixed4 frag (v2f im) : SV_Target
            {
                float3 ddx =getVPos(im.uv) -getVPos(im.uv +float2(1.0,0)/_ScreenParams.xy)  ;
                float3 ddy =getVPos(im.uv) -getVPos(im.uv +float2(0,2.0)/_ScreenParams.xy)  ;
                float3 n =  cross(ddx,ddy);
                
                float3 rayD =  float3(im.uv*2-1,-1);
                float3 vpos =  rayD * LinearEyeDepth(tex2D(_MainTex, im.uv).r);
                return float4(vpos/100,0);

            }
            ENDCG
        }
    }
}
