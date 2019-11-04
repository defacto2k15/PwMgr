Shader "Custom/Sandbox/PrintingShader" {
    Properties {
		_NumberToShow("NumberToShow", Range(-100000,100000)) = 0
		_CellsCount("CellsCount", Vector) = (0,0,0,0)
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
						
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 nrm : TEXCOORD1;
			};

			float _ArrayElementSelector;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.nrm = mul(float4(v.norm, 0.0), unity_WorldToObject).xyz;
				return o;
			}

			float _NumberToShow;
			float4 _CellsCount;

#include "text_printing.hlsl"





			fixed4 frag (v2f i) : SV_Target
			{
				//fixed4 color = printNumber(_NumberToShow, i.uv);
				fixed4 color = boxedPrintNumber(_NumberToShow, i.uv, float4(0.2, 0.2, 0.6, 0.6));
				if (color.a <= 0.01) {
					color.rgb = 1;
				}
				return color;
			}
			ENDCG
		}
	}
}