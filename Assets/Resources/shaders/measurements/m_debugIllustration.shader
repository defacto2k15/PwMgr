Shader "Custom/Measurements/DebugIllustration" {
     Properties
     {
			_BackgroundTex ("BackgroundTex", any) = "" {}
			_ResultTex ("ResultTex", any) = "" {}
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

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : TEXCOORD0;
			}; 


			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);

				return o;
			}

			sampler2D _BackgroundTex;
			sampler2D _ResultTex;

			float4 frag(v2f i) : COLOR {
				float2 uv = i.projPos.xy;

				float4 backgroundPixel = tex2D(_BackgroundTex, uv);
				float4 resultPixel = tex2D(_ResultTex, uv);

				float4 color = 0;
				color = backgroundPixel * 0.5 + resultPixel * 0.5;
				color = resultPixel;

				return color;
			}
			ENDCG
		}
	}
}