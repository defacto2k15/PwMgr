Shader "Custom/NPR/PostProcessingChain/Tfm" {
     Properties
     {
			_MainTex ("", any) = "" {}
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
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;


// KOD Z // by Jan Eric Kyprianidis <www.kyprianidis.com>
			float4 frag(v2f i) : COLOR {
				const float PI = 3.14159;
				float3 originalColor = tex2D(_MainTex, i.projPos.xy).rgb;
				float2 uv = i.projPos.xy;

				int2 srcSize = _MainTex_TexelSize.zw;
				float2 d = 1.0 / srcSize;

				float3 g = originalColor;
				// Paragraf 3.1 Roznica ze w jednym jest plus, a w drogim minus
				// E - g.x
				// G - g.y
				// F - g.z

				float lambda1 = 0.5 * (g.y + g.x + 
					sqrt(g.y*g.y - 2.0*g.x*g.y + g.x*g.x + 4.0*g.z*g.z));
				float lambda2 = 0.5 * (g.y + g.x -
					sqrt(g.y*g.y - 2.0*g.x*g.y + g.x*g.x + 4.0*g.z*g.z));

				float2 v = float2(lambda1 - g.x, -g.z); // gx to E gz to F
				float2 t;
				if (length(v) > 0.0) { 
					t = normalize(v);
				} else {
					t = float2(0.0, 1.0);
				}

				float phi = atan2(t.y, t.x); 

				// A to anisotropy!
				float A = (lambda1 + lambda2 > 0.0)?
					(lambda1 - lambda2) / (lambda1 + lambda2) : 0.0;
				return float4(t.x, t.y , phi, A);
			}
			ENDCG
		}
	}
}