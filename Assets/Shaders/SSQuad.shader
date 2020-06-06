Shader "Unlit/SSQuad"
{
    Properties
    {
      _asigma ("A Sigma", Range(0.0,100)) =10 
      _bsigma ("B Sigma", Range(0.0,100)) =0.1 
      _zThreshold ("Z difference", Range(.0,100)) =1
      _BlurAmount ("Blur Amount", Float) =1
      _roughness ("roughness", Float) =1
    }
    SubShader
    {

CGINCLUDE
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
  struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                
            };

            struct v2f
            {
                float4 ProjectionSpace: TEXCOORD1;
                float4 vertex : SV_POSITION;
                float3 viewDir:TEXCOORD2;
                float2 uv : TEXCOORD0;
            };



            v2f vert (appdata v)
            {
                v2f o;
	
                o.vertex = UnityObjectToClipPos( v.vertex);
                o.uv = v.uv ;
                o.ProjectionSpace = o.vertex;
                o.viewDir = WorldSpaceViewDir(v.vertex);
                return o;
            }
ENDCG
        Pass
        {

             CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag


                       
            sampler2D Fluid; 
            sampler2D FluidNormals; 
  
            #define oneOverSqrt2pi 0.398942280401
            float gaussProfile(float x ,float sigma){
             return oneOverSqrt2pi * 1/sigma * exp(-0.5*x*x/(sigma*sigma));
            }
            
            float _asigma; 
            float _bsigma; 
            float _zThreshold; 
            float _BlurAmount;
            float _roughness;
            float3 _mainLightDir;
            #define KernelSize  11
            #define kSize  (KernelSize-1)/2
            #define Pi  acos(-1)

            float SmoothDepth(float2 uv)
            {
                
	            float kernel[KernelSize];
	            float final_colour = 0;
	            
	            float Z = 0.0;
	            for (int j = 0; j <= kSize; ++j)
	            {
	            	kernel[kSize+j] = kernel[kSize-j] = gaussProfile(float(j), _asigma);
	            }
	            
	            float c = tex2D(Fluid,uv).r;
	            float l = Linear01Depth(c);
	            float cc,ll;
	            float factor;
	            float normalizationFactor = 1.0/gaussProfile(0.0, _bsigma);
	            float texelSize = 1./ _ScreenParams.xy;
	            //read out the texels
	            for (int i=-kSize; i <= kSize; ++i)
	            {
	            	for (int j=-kSize; j <= kSize; ++j)
	            	{	            	     
	            		cc =tex2D(Fluid,uv+ float2(float(i),float(j))*texelSize ).r;
	            		ll = Linear01Depth(cc);
                        if( (ll-l)/_zThreshold <=1){
	            		    factor = gaussProfile(1-(ll-l), _bsigma)*normalizationFactor*kernel[kSize+j]*kernel[kSize+i];
	            		    Z += factor;
	            		    final_colour += factor*ll;
	            		}
	            		
	            		
	            	}
	            } 
	            return final_colour/Z;
            }
            
            
            float CharlieD(float roughness, float ndoth)
            {
                float invR = 1. / roughness;
                float cos2h = ndoth * ndoth;
                float sin2h = 1. - cos2h;
                return (2. + invR) * pow(sin2h, invR * .5) / (2. * Pi);
            }
             
            float AshikhminV(float ndotv, float ndotl)
            {
                return 1. / (4. * (ndotl + ndotv - ndotl * ndotv));
            }
                      
             float3 FresnelTerm(float3 specularColor, float vdoth)
            {
            	float3 fresnel = specularColor + (1. - specularColor) * pow((1. - vdoth), 5.);
            	return fresnel;
            }   
             float3 SmoothNormals(float2 uv)
            {
                
	            float kernel[KernelSize];
	            float3 final_colour = 0;
	            
	            float Z = 0.0;
	            for (int j = 0; j <= kSize; ++j)
	            {
	            	kernel[kSize+j] = kernel[kSize-j] = gaussProfile(float(j), _asigma);
	            }
	            
	            float3 c = tex2D(FluidNormals,uv);
	            c= normalize(c*2.-1);
	            float l = Linear01Depth(tex2D(Fluid,uv).r);
	            float3 cc;
	            float ll;
	            float factor;
	            float normalizationFactor = 1/gaussProfile(0.0, _bsigma);
	            float texelSize = lerp(1,_BlurAmount,1-l)/ _ScreenParams.xy;
	            //read out the texels
	            for (int i=-kSize; i <= kSize; ++i)
	            {
	            	for (int j=-kSize; j <= kSize; ++j)
	            	{	            	     
	            		cc =tex2D(FluidNormals,uv+ float2(float(i),float(j))*texelSize );
	            		cc = normalize(cc*2-1);
	            		ll = Linear01Depth(tex2D(Fluid,uv+ float2(float(i),float(j))*texelSize ).r);
                       //if(abs(LinearEyeDepth(tex2D(Fluid,uv+ float2(float(i),float(j))*texelSize ).r)-LinearEyeDepth(tex2D(Fluid,uv).r))<_zThreshold){
	            		    factor = gaussProfile((ll-l), _bsigma)*normalizationFactor*kernel[kSize+j]*kernel[kSize+i];
	            		    Z += factor;
	            		    final_colour += factor*cc;
	            		//}
	            	}
	            	
	            } 
	            return normalize(final_colour/Z);
            }
            float3 pal( in float t, in float3 a, in float3 b, in float3 c, in float3 d )
            {
                return a + b*cos( 6.28318*(c*t+d) );
            }

            #define BOIDPALETTE(p) pal( p,float3(0.5,0.5,0.5),float3(0.5,0.5,0.5),float3(1.0,1.0,0.5),float3(0.8,0.90,0.30) )

            float3 frag (v2f i) : SV_Target
            {
                float AR = _ScreenParams.x/_ScreenParams.y;
                float2 ClipPos = i.ProjectionSpace.xy/i.ProjectionSpace.w;
                ClipPos = (ClipPos+1.)/2.0;
                
				//#if UNITY_UV_STARTS_AT_TOP
				//	ClipPos.y = 1-ClipPos.y;
				//#endif	
     

             if(Linear01Depth(tex2D(Fluid,ClipPos)) >=1   ){
                 discard;
             }
             float3 N = SmoothNormals(ClipPos).xyz;
             float3 L = -_mainLightDir;
             float3 V = normalize(i.viewDir);
             float3 H = normalize(L +V);
            float dotnh = saturate(dot(N,H));
            float dotnv = saturate(dot(N,V));
            float dotnl = saturate(dot(N,L));
            float3 dotvh = saturate(dot(V,H));
             float d = CharlieD(_roughness, dotnh);
        	 float v = AshikhminV(dotnv, dotnl);
             float3  f = FresnelTerm(1, dotvh);
             float3 C =BOIDPALETTE(_WorldSpaceCameraPos + V*LinearEyeDepth(tex2D(Fluid,ClipPos)));
             float3 specular = f * d * v * Pi * dotnl;
             return dotnl*float3(61,106,244)/255  ;
            }
            ENDCG
        }
        
        //unused depth normal recovery.
        /*
         Pass
        {   
         ZWrite On
                
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
                       sampler2D Fluid;

            #define z(uv)  LinearEyeDepth(tex2D(Fluid,uv).r)
            #define e float3(2/_ScreenParams.xy  ,0)
            float3 GetNormal(float2 uv){

            	float d0 = z(uv).r;
            	float d1 = z(uv+e.xz).r;
            	float d2 = z(uv+e.zy).r;
            	
            	float dx = (d0 - d1) /e.x;
            	float dy = (d0 - d2) /e.y;
            	
            	float3 normal = normalize(float3(dx,-dy,d0));
            	
            	return normal;
            }
            float3 GetNormal1(float2 uv){
                
               float dzdx =  (z(uv+e.xz).r - z(uv-e.xz).r)/2.0  ;
               float dzdy =  (z(uv+e.zy).r - z(uv-e.zy).r)/2.0 ;
               float3 N=float3(-dzdx,-dzdy,1);
               return normalize(N);
            }
            #define Near _ProjectionParams.y
			#define Far  _ProjectionParams.z
            float2 computeNearPlaneDimensions() {
            	//float near = Proj//UNITY_MATRIX_P._m23 / (UNITY_MATRIX_P._m22 - 1.0);
            	float halfH = abs(Near / UNITY_MATRIX_P._m11);
            	float halfW = abs(Near / UNITY_MATRIX_P._m00);
            
            	return float2(halfW, halfH);
            
            }
            #define F UNITY_MATRIX_V[2].xyz
			#define R UNITY_MATRIX_V[0].xyz
			#define U UNITY_MATRIX_V[1].xyz
            float3 getWorldUV(float2 uv) 
            {
            
            	float2 nearDims = computeNearPlaneDimensions();
            	float3 m_pixels = float3(uv * 2. - 1.,Near );
            
            	m_pixels.x *= nearDims.x;
            	m_pixels.y *= nearDims.y;
            	
            	//R- right U-Up F-Forward basis vectors of the camera.
            	m_pixels.xyz =  m_pixels.x * normalize(R) + normalize(U) * m_pixels.y + normalize(-F) * m_pixels.z;
           
            	return m_pixels.xyz ;
            }
             float3 getEyePos(float2 uv){
                float3 D = normalize(getWorldUV(uv));
                return D * z(uv);
            }
            float _asigma;
            float _bsigma;
           float3 GetNormal2(float2 uv){
               
               if (z(uv) > Far) {
                   discard;
                   return 0;
               }
               // calculate eye-space position from depth
               float3 posEye = getEyePos(uv);
               float2 texelSize = 2./_ScreenParams.xy;
               // calculate differences
               float3 ddx = getEyePos(uv + float2(texelSize.x, 0)) - posEye;
               float3 ddx2 = posEye - getEyePos( uv + float2(-texelSize.x, 0));
               if (abs(ddx.z) > abs(ddx2.z)) {
                   ddx = ddx2;
               }
               float3 ddy = getEyePos( uv +  float2(0, texelSize.y)) - posEye;
               float3 ddy2 = posEye - getEyePos( uv + float2(0, -texelSize.y));
               if (abs(ddy2.z) < abs(ddy.z)) {
                   ddy = ddy2;
               }
               // calculate normal
               float3  n = cross(-ddx, ddy);
               n.z*=-1;
               n = normalize(n);
               
               return n;
           }
            
            float4 frag (v2f i) : SV_Target
            {
                float AR = _ScreenParams.x/_ScreenParams.y;
                float2 ClipPos = i.ProjectionSpace.xy/i.ProjectionSpace.w;
                ClipPos = (ClipPos+1.)/2.0;
                ClipPos.y = 1-ClipPos.y;
             // float id = floor((ClipPos.y*2));
                float2 uv2 = ClipPos;
                uv2.x = fmod(ClipPos.x,0.5);
                float3 N= GetNormal2(uv2).xyzz ; 
                float d = pow(Linear01Depth(tex2D(Fluid,uv2).r),2);
                return ClipPos.x>=0.5? d:dot(N , normalize(float3(1,1,0)));
            }
            ENDCG
        }
        */
    }
}
