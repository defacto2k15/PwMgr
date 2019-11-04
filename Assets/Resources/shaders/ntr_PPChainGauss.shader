Shader "Custom/NPR/PostProcessingChain/Gauss" {
     Properties
     {
			_MainTex ("", any) = "" {}
			_Sigma("Sigma", Range(0,2)) = 0
     }
	SubShader{
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
				float3 worldDirection : TEXCOORD1;
			}; 

			float4x4 _ClipToWorld;

			float _Sigma;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				COMPUTE_EYEDEPTH(o.projPos.z);

				float4 clip = float4(o.pos.xy, 0.0, 1.0);
				o.worldDirection = mul(_ClipToWorld, clip) - _WorldSpaceCameraPos;

				return o;
			}

			sampler2D _CameraDepthTexture;
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;


// KOD Z // by Jan Eric Kyprianidis <www.kyprianidis.com>
			float4 frag(v2f i) : COLOR {
				const float PI = 3.14159;
				float3 originalColor = tex2D(_MainTex, i.projPos.xy).rgb;
				float2 uv = i.projPos.xy;

				int2 srcSize = _MainTex_TexelSize.zw;
				float2 d = 1.0 / srcSize;

				float twoSigma2 = 2 * _Sigma * _Sigma;
				int halfWidth = int(ceil(2*_Sigma));

				float3 sum = 0;
				float norm = 0;
				if(halfWidth > 0 ){
					for(int i = -halfWidth; i<= halfWidth; ++i){
						for(int j = -halfWidth; j <= halfWidth; j++){
							float d = length(float2(i,j));
							float kernel = exp(-d*d / twoSigma2);
							float3 c = tex2D(_MainTex, uv + float2(i,j)/srcSize).rgb;
							sum += kernel * c;
							norm += kernel;
						}
					}
				}
				return float4( sum/norm, 1);
			}
			ENDCG
		}
	}
}