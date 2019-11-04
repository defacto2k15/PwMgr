Shader "Custom/EProp/Dummy"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_ScopeLength("ScopeLength", Int) = 0
		_LocaleBufferIndexScope("_LocaleBufferIndexScope", Int) = 0
		_InScopeIndex("_InScopeIndex", Int) = 0
		_Pointer("Pointer", Int) = 0
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
			#include "UnityCG.cginc"

			float4 _Color;
			int _ScopeLength;

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			int _LocaleBufferIndexScope;
			int _InScopeIndex;
			int _Pointer;

#include "eterrain_EPropLocaleCommon.hlsl"

			StructuredBuffer<EPropLocale> _EPropLocaleBuffer;
			StructuredBuffer<EPropElevationId> _EPropIdsBuffer;

			v2f vert(appdata v)
			{
				v2f o;

				uint scopeIndexArray = (uint)( _LocaleBufferIndexScope);
				uint InScopeIndex =(uint)(  _InScopeIndex);

				EPropElevationId  elevationId = _EPropIdsBuffer[_Pointer];

				//uint localeIndex = ComputeIndexInLocaleBuffer(_ScopeLength,  scopeIndexArray, InScopeIndex);
				uint localeIndex = ComputeIndexInLocaleBuffer(_ScopeLength, elevationId.LocaleBufferScopeIndex, elevationId.InScopeIndex);
				float height = _EPropLocaleBuffer[localeIndex].Height;

				float4 vertexWorldPos =  mul(unity_ObjectToWorld , v.vertex);
				vertexWorldPos.y += height;

				o.vertex =  mul(UNITY_MATRIX_VP, float4(vertexWorldPos.xyz, 1.0));
					//UnityObjectToClipPos(v.vertex);

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

				uint scopeIndexArray = (uint)( _LocaleBufferIndexScope);
				uint InScopeIndex =(uint)(  _InScopeIndex);

				uint localeIndex = ComputeIndexInLocaleBuffer(_ScopeLength,  scopeIndexArray, InScopeIndex);
				EPropLocale locale = _EPropLocaleBuffer[localeIndex];
				float height = locale.Height;
				int levelIndex = round(locale.Normal.x);
				int ringIndex = round(locale.Normal.y);

				levelIndex = ringIndex;
				float4 finalColor = 0;
				if (levelIndex == 0) {
					finalColor = float4(1, 0, 0, 1);
				}
				else if (levelIndex == 1) {
					finalColor = float4(0, 1, 0, 1);
				} else if (levelIndex == 2) {
					finalColor = float4(0, 0, 1, 1);
				}
				else {
					finalColor = float4(1, 1, 1, 1);
				}
				finalColor = float4(locale.Normal.z, 0, 0, 1);

				return finalColor;

				return height;
				return generateRandomColor(scopeIndexArray).xyzz;
			}
			ENDCG
		}
	}
}