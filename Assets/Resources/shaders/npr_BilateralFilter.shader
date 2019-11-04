Shader "Custom/NPR/BilateralFilter" {
     Properties
     {
			_MainTex ("", any) = "" {}
			_Sigma_R("_Sigma_R", Range(0,10)) = 0.1
			_Sigma_D("_Sigma_D", Range(0,100)) = 10
			_Sigma_F("_Sigma_F", Range(0,10)) = 0.001 // depth-based
			_FilterFlag("FilterFlag", Range(0,1)) = 0
     }
	SubShader{
	// Na bazie kodu https://www.shadertoy.com/view/4dfGDH
		Tags { "RenderType" = "Opaque" }
		Pass {
			Fog { Mode Off }
			Cull Off
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "noise.hlsl"

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : TEXCOORD0;
			}; 

			struct pixelSample {
				float3 normal;
				float depth;
			};

			float4x4 _ClipToWorld;

			sampler2D _CameraDepthTexture;
			sampler2D _CameraDepthNormalsTexture;
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			float _NormalsSensivity;
			float _DepthSensivity;
			float _FilterFlag;
			float _Sigma_D;
			float _Sigma_R;
			float _Sigma_F;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				return o;
			}

			pixelSample samplePixel(float2 uv){
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
				depth = LinearEyeDepth(depth);

				float3 normalValues;
				float inDepth = depth;
				DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv), inDepth, normalValues);

				pixelSample sample;
				sample.normal = normalValues;
				sample.depth = round(depth/20*255)/255;;
				return sample;
			}

			float calculateDepth(float2 uv){
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
				return LinearEyeDepth(depth);
			}

			// funkcja okreslajaca gestrosc p-stwa dla 
			float normpdf(in float x, in float sigma)
			{ // 0.39894 = 1/sqrt(2*PI)
				return 0.39894*exp(-0.5*x*x/(sigma*sigma))/sigma;
			}

			float normpdf3(in float3 v, in float sigma)
			{
				return 0.39894*exp(-0.5*dot(v,v)/(sigma*sigma))/sigma;
			}

			// dzialanie ladnie pokazane tu: https://dsp.stackexchange.com/questions/6289/understanding-the-parameters-of-the-bilateral-filter
			float4 frag(v2f i) : COLOR {
				int2 srcSize = _MainTex_TexelSize.zw;
				float2 uv = i.projPos.xy;
				float3 originalColor = tex2D(_MainTex, uv);

				const float MSIZE = 15;
				float sigma_r = _Sigma_R; // - bigger - filter becomes similar to gaussian one
				float sigma_d = _Sigma_D; // - bigger - smooths larger features
				float sigma_f = _Sigma_F; // Smoothing depth-based 
				
				//declare stuff
				const int kSize = (MSIZE-1)/2;
				float kernel[MSIZE];
				float3 final_colour = (0.0);
				
				//create the 1-D kernel TODO to preprocessing
				float Z = 0.0;
				for (int j = 0; j <= kSize; ++j)
				{
					kernel[kSize+j] = kernel[kSize-j] = normpdf(float(j), sigma_d); // normal distribution?
				}
				
				
				float3 c = tex2D(_MainTex, uv).rgb;
				float originalDepth = calculateDepth(uv);
				float3 cc;
				float factor;
				float bZ = 1.0/normpdf(0.0, sigma_r);
				float bF = 1.0/normpdf(0.0, sigma_f);
				//return normpdf(_Sigma_D, sigma_r);
				//read out the texels
				for (int i=-kSize; i <= kSize; ++i)
				{
					for (int j=-kSize; j <= kSize; ++j)
					{
						float2 cUv =  uv+(float2(float(i),float(j)) / srcSize);
						cc = tex2D(_MainTex,cUv).rgb;
						float ccDepth = calculateDepth(cUv);

						factor = 
							min(
								normpdf3(cc-c, sigma_r)*bZ,
								normpdf(originalDepth-ccDepth, sigma_f)  * bF * 0.001
								) *
							kernel[kSize+j]*kernel[kSize+i];

						Z += factor;
						final_colour += factor*cc;

					}
				}
				float3 outColor = final_colour/Z;

				return float4(lerp(outColor, originalColor, _FilterFlag) , 1); 
			}
			ENDCG
		}
	}
}