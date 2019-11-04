Shader "Custom/Measurements/DebugTam" {
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

			struct MRTFragmentOutput
			{
				half4 dest0 : SV_Target0;
				half4 dest1 : SV_Target1;
				half4 dest2 : SV_Target2;
				half4 dest3 : SV_Target3;
			};

			MRTFragmentOutput make_MRTFragmentOutput(half4 dest0, half4 dest1, half4 dest2, half4 dest3) {
				MRTFragmentOutput o;
				o.dest0 = dest0;
				o.dest1 = dest1;
				o.dest2 = dest2;
				o.dest3 = dest3;
				return o;
			}

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

		#define CUSTOM_LOD 1
		#define CUSTOM_LEVEL 1

			MRTFragmentOutput frag(v2f input) : SV_Target
			{
				float2 uv = input.uv;
				uv.y /= 8;

				float intensity = 0;
				float diffuse = input.diffuse;

		#if CUSTOM_LEVEL

		#if CUSTOM_LOD
				intensity = UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, float3(uv, _ArrayElementSelector), _ArrayLodSelector).a;
		#else
				intensity = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(uv, _ArrayElementSelector)).a;
		#endif

		#else
				int diffuseIntensity = 4 - round(diffuse * 5);
				intensity = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(uv, diffuseIntensity + 1)).a;
				if (diffuseIntensity == -1) {
					intensity = 0;
				}
		#endif

				float4 outColor = 0;
				outColor.a = 1;
				outColor.b = 1; // signal that this is hatched element
				outColor.r = round(intensity);

				float4 idColor = 0;
				idColor.r = 0.12;
				idColor.g = 0.15;
				idColor.b = 0.5;

				return make_MRTFragmentOutput(outColor,idColor,0,0);
			}

			ENDCG
		}
	}
}
