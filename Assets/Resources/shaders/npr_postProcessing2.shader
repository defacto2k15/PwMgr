Shader "Custom/NPR/PostProcessing2" {
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
			sampler2D _CameraDepthNormalsTexture;
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

// KOD Z // by Jan Eric Kyprianidis <www.kyprianidis.com>
			float4 frag(v2f i) : COLOR {
				float3 normalValues;
				float depthValue;
				DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.projPos.xy), depthValue, normalValues);

				return float4(normalValues,1);
			}
			ENDCG
		}
	}
}