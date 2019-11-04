Shader "Custom/Sandbox/Filling/BreslavDebug" {
	Properties{
		_BreslavU("BreslavU", Vector) = (0.0, 0.0, 0.0, 0.0)
		_BreslavO("BreslavO", Vector) = (0.0, 0.0, 0.0, 0.0)
		_BreslavV("BreslavV", Vector) = (0.0, 0.0, 0.0, 0.0)
		_BreslavSt("BreslavSt", Range(0,1)) = 0
    }
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
						
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 projPos : ANY_PROJ_POS;
			};

			v2f vert (appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.projPos = ComputeScreenPos (o.pos);

				return o;
			}

			float2 _BreslavU;
			float2 _BreslavO;
			float2 _BreslavV;
			float _BreslavSt;

			fixed4 colorFromUv(fixed2 uv) {
				fixed4 c = 0;
				c.rg = frac(uv * 10);
				return c;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = 0;
				float2 uv = i.projPos.xy / i.projPos.w;
				uv -= _BreslavO;
				uv = float2(
					dot(uv, _BreslavU) / (dot(_BreslavU, _BreslavU)),
					dot(uv, _BreslavV) / (dot(_BreslavV, _BreslavV)));

				fixed4 h_lo = colorFromUv(uv);
				fixed4 h_hi = colorFromUv(uv*2);

				if (_BreslavSt <= 0) {
					color = h_lo;
				}
				else if (_BreslavSt >= 1){
					color = h_hi;
				}
				else {
					color = lerp(h_lo, h_hi, _BreslavSt);
				}

				return color;
			}
			ENDCG
		}
	}
}
