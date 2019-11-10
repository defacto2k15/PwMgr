Shader "Custom/EProp/DebugInstancing"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_ScopeLength("ScopeLength", Int) = 0
		[PerRendererData]_LocaleBufferScopeIndexArray("_LocaleBufferScopeIndexArray", Float) = 0.0
		[PerRendererData]_InScopeIndexArray("_InScopeIndexArray", Float) = 0.0
	}

		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
#define UNITY_INSTANCING_ENABLED 
			#include "UnityCG.cginc"

			float4 _Color;
			int _ScopeLength;

			struct appdata
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID // necessary only if you want to access instanced properties in fragment Shader.
			};

			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float, _LocaleBufferScopeIndexArray)
				UNITY_DEFINE_INSTANCED_PROP(float, _InScopeIndexArray)
				UNITY_DEFINE_INSTANCED_PROP(float, _Pointer)
			UNITY_INSTANCING_BUFFER_END(Props)

#include "eterrain_EPropLocaleCommon.hlsl"

			StructuredBuffer<EPropLocale> _EPropLocaleBuffer;
			StructuredBuffer<EPropElevationId> _EPropIdsBuffer;

			v2f vert(appdata v)
			{
				v2f o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				//uint scopeIndexArray = asuint(UNITY_ACCESS_INSTANCED_PROP(Props, _LocaleBufferScopeIndexArray));
				//uint inScopeIndexArray = asuint(UNITY_ACCESS_INSTANCED_PROP(Props,  _InScopeIndexArray));
				//uint localeIndex = ComputeIndexInLocaleBuffer(_ScopeLength,  scopeIndexArray, inScopeIndexArray);

				uint pointerValue = asuint(UNITY_ACCESS_INSTANCED_PROP(Props, _Pointer));
				EPropElevationId  elevationId = _EPropIdsBuffer[pointerValue];
				uint localeIndex = ComputeIndexInLocaleBuffer(_ScopeLength, elevationId.LocaleBufferScopeIndex, elevationId.InScopeIndex);

				float height = _EPropLocaleBuffer[localeIndex].Height;

				float4 vertexWorldPos =  mul(unity_ObjectToWorld , v.vertex);
				vertexWorldPos.y += height;
				o.vertex =  mul(UNITY_MATRIX_VP, float4(vertexWorldPos.xyz, 1.0));

				return o;
			}

			float3 generateRandomColor(int seed) {
				seed = seed + 10000000;;
				float3 colorsArray[64];
				for (int r = 0; r < 4; r++) {
					for (int g = 0; g < 4; g++) {
						for (int b = 0; b < 4; b++) {
							colorsArray[r + 4 * g + 16 * b] = float3(r / 3.0, g / 3.0, b / 3.0);
						}
					}
				}
				return colorsArray[seed % 64];
			}

			fixed4 frag(v2f i) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);

				uint scopeIndexArray = asuint(UNITY_ACCESS_INSTANCED_PROP(Props, _LocaleBufferScopeIndexArray));
				uint inScopeIndexArray = asuint(UNITY_ACCESS_INSTANCED_PROP(Props,  _InScopeIndexArray));

				uint localeIndex = ComputeIndexInLocaleBuffer(_ScopeLength,  scopeIndexArray, inScopeIndexArray);
				float height = _EPropLocaleBuffer[localeIndex].Height;

				return generateRandomColor(scopeIndexArray).xyzz;
			}
			ENDCG
		}
	}
}