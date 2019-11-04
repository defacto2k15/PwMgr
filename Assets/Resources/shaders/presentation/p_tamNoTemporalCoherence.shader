Shader "Custom/Presentation/TamNotTemporalCoherence" {
    Properties {
		_ArrayElementSelector("ArrayElementSelector", Range(0,16)) = 0
		_ArrayLodSelector("ArrayLodSelector", Range(0,16)) = 0
		_MainTex("MainTex", 2DArray) = "white" {}
    }
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : ANY_UV;
				float diffuse : ANY_DIFFUSE;
			};

			float _ArrayElementSelector;
			float _ArrayLodSelector;
			UNITY_DECLARE_TEX2DARRAY(_MainTex);

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;  

                half3 worldNormal = UnityObjectToWorldNormal(v.norm);
                half nl = max(0, dot(worldNormal.xyz, _WorldSpaceLightPos0.xyz));
                o.diffuse = nl * _LightColor0;

				return o; 
			}

			float4 frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;
				float2 screenUv = float2(input.pos.x / _ScreenParams.x, input.pos.y / _ScreenParams.y);

				float2 lenX = (_WorldSpaceCameraPos.xy);
				lenX *= 523.5431;
				lenX = frac(lenX) * 10;

				float diffuse = input.diffuse;
				float intensity = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(lenX+screenUv, _ArrayElementSelector)).a;

				return (1 - intensity);
			}

			ENDCG
		}
	}
}
