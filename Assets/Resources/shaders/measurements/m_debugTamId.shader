Shader "Custom/Measurements/DebugTamId" {
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
				float3 worldSpacePos : ANY_WORLD_SPACE_POS;
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

				o.worldSpacePos = mul(unity_ObjectToWorld, v.vertex);

				return o; 
			}

		#define CUSTOM_LOD 1
		#define CUSTOM_LEVEL 1

			float2 to2ByteValue(float input, float max) {
				float ni = (input + max) / (2 * max); //normalized to 0-1

				float r1 = round(ni * 255) / 255.0;
				float r2 = round( frac(ni * 256) * 255) / 255.0;

				return float2(r1, r2);
			}

			MRTFragmentOutput frag(v2f input)
			{
				float2 uv = input.uv;
				uint2 screenCoords = input.pos.xy;
				//uv.y /= 8;

				float4 c = 0;
				float diffuse = input.diffuse;

		#if CUSTOM_LEVEL

		#if CUSTOM_LOD
				c = UNITY_SAMPLE_TEX2DARRAY_LOD(_MainTex, float3(uv, _ArrayElementSelector), _ArrayLodSelector);
		#else
				c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(uv, _ArrayElementSelector));
		#endif

		#else
				int diffuseIntensity = 4 - round(diffuse * 5);
				c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(uv, diffuseIntensity + 1));
				if (diffuseIntensity == -1) {
					c.a = 0;
				}
		#endif

				float lightIntensity = diffuse;

				int id = round(c.r * 255) + round(c.g * 255 * 256);
				float tParam = c.b;

				float4 outColor = 0;
				outColor.a = tParam;
				outColor.b = 1; // signal that this is hatched element
				outColor.r = round(c.a);
				outColor.g = diffuse;

				float4 idColor = 0;
				idColor.r = c.r;
				idColor.g = c.g;
				idColor.b = 0;
				idColor.a = 0;
				//outColor = idColor;

				float3 worldSpacePos = input.worldSpacePos;
				float4 positions1Color = 0;
				float4 positions2Color = 0;

				float max = 10;

				positions1Color.rg = to2ByteValue(worldSpacePos.x, max);
				positions1Color.ba = to2ByteValue(worldSpacePos.y, max);
				positions2Color.rg = to2ByteValue(worldSpacePos.z, max);

				MRTFragmentOutput fo = make_MRTFragmentOutput(outColor,idColor,positions1Color,positions2Color);
				return fo;
			}

			ENDCG
		}
	}
}
