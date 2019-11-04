Shader "Custom/NPR/PostProcessingChain/Lic" {
     Properties
     {
			_MainTex ("", any) = "" {}
			_NoiseTex("", 2D) = "white" {}
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
			#pragma only_renders d3d11

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
			sampler2D _NoiseTex;


// KOD Z // by Jan Eric Kyprianidis <www.kyprianidis.com>

			struct lic_t { 
				float2 p; 
				float2 t;
				float w;
				float dw;
			};

			void step(inout lic_t s) {
				float2 t = tex2D(_MainTex, s.p).xy;
				if (dot(t, s.t) < 0.0) t = -t;
				s.t = t;

				s.dw = (abs(t.x) > abs(t.y))? 
					abs((frac(s.p.x) - 0.5 - sign(t.x)) / t.x) : 
					abs((frac(s.p.y) - 0.5 - sign(t.y)) / t.y);

				s.p += t * s.dw / _MainTex_TexelSize.zw;
				s.w += s.dw;
			}

			float4 frag(v2f i) : COLOR {
			return 1;
#ifdef NOT_COMPLETED
				const float PI = 3.14159;
				float2 uv = i.projPos.xy;

				int2 srcSize = _MainTex_TexelSize.zw;
				float2 d = 1.0 / srcSize;

				float sigma = 3;
				float twoSigma2 = 2.0 * sigma * sigma;
				float halfWidth = 2.0 * sigma;

				float3 c = tex2D(_NoiseTex, uv ).xyz;
				float w = 1.0;

				lic_t a, b;
				a.p = b.p = uv;
				a.t = tex2D(_MainTex, uv ).xy / srcSize;
				b.t = -a.t;
				a.w = b.w = 0.0; 

				//[unroll(830)]
				while (a.w < halfWidth) {
					step(a);
					float k = a.dw * exp(-a.w * a.w / twoSigma2);
					c += k * tex2D(_NoiseTex, a.p).xyz;
					w += k;
				}
				//[unroll(830)]
				while (b.w < halfWidth) {
					step(b);
					float k = b.dw * exp(-b.w * b.w / twoSigma2);
					c += k * tex2D(_NoiseTex, b.p).xyz;
					w += k;
				}
				
				return float4(c / w,1);			
#endif
			}
			ENDCG
		}
	}
}