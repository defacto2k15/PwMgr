Shader "Custom/ETerrain/GenerateNewCorner"
{
	Properties
	{
		_FloorHeightTexture("_FloorHeightTexture", 2D) = "pink" {}
		_WeldingAreaCoords("_WeldingAreaCoords", Vector) = (0.0, 0.0, 1.0, 1.0)
		_MarginSize("MarginSize", Range(0,1)) = 0.4
		_CornerToWeld("CornerToWeld", Vector) = (0.0, 0.0, 0.0, 0.0) //TL TR BR BL
		_PixelSizeInUv("PixelSizeInUv", Range(0,1)) = 0.01
		_HeightMapSliceIndex("HeightMapSliceINdex", Float) = 0.0
	}

	SubShader
	{
		Pass
		{
		ZTest Always Cull Off ZWrite Off Fog { Mode Off } //Rendering settings
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc" 
			#include "common.txt"

			UNITY_DECLARE_TEX2DARRAY(_HeightMap);
			float4 _WeldingAreaCoords;
			float _MarginSize;
			float4 _CornerToWeld;
			float _PixelSizeInUv;
			float _HeightMapSliceIndex;

			struct v2f {
				float4 pos : POSITION; // niezbedna wartosc by dzialal shader
				half2 uv : TEXCOORD0;
			};

			v2f vert(appdata_img v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex); //niezbedna linijka by dzialal shader
				o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
				return o;
			}

			float2 toGlobalUv(float2 localUv) {
				return _WeldingAreaCoords.xy + float2(localUv.x * _WeldingAreaCoords.z, localUv.y * _WeldingAreaCoords.w);
			}

			float2 modToUv(float2 uv) {
				return frac(uv);
			}

			//Our Fragment Shader
			fixed4 frag(v2f i) : Color{
				float2 uv = i.uv;
				float2 globalUv = toGlobalUv(uv);

				float horizontalMasterWeight = 0;
				float verticalMasterWeight = 0;

				float2 horizontalMasterUv;
				float2 verticalMasterUv;

				if (_CornerToWeld[0] > 0.5) { //TL
					horizontalMasterUv = modToUv(toGlobalUv(float2(uv.x, 1)) + float2(0, _PixelSizeInUv));
					verticalMasterUv = modToUv(toGlobalUv(float2(0, uv.y)) + float2(-_PixelSizeInUv, 0));

					horizontalMasterWeight = invLerp(_MarginSize, 0, 1 - uv.y);
					verticalMasterWeight = invLerp(_MarginSize, 0, uv.x);

					horizontalMasterWeight = 0;
				}
				if (_CornerToWeld[1] > 0.5) { //TR // this should not really happen!!
					horizontalMasterUv =	modToUv(toGlobalUv(float2(uv.x, 1)) + float2(0, _PixelSizeInUv));
					verticalMasterUv =		modToUv(toGlobalUv(float2(1, uv.y)) + float2(_PixelSizeInUv, 0));

					horizontalMasterWeight = invLerp(_MarginSize, 0, 1 - uv.y);
					verticalMasterWeight = invLerp(_MarginSize, 0, 1-uv.x);

					horizontalMasterWeight = 0;
					verticalMasterWeight = 0;
				}
				if (_CornerToWeld[2] > 0.5) { //BR
					horizontalMasterUv = modToUv(toGlobalUv(float2(uv.x, 0)) + float2(0, -_PixelSizeInUv));
					verticalMasterUv = modToUv(toGlobalUv(float2(1, uv.y)) + float2(_PixelSizeInUv, 0));

					horizontalMasterWeight = invLerp(_MarginSize, 0,  uv.y);
					verticalMasterWeight = invLerp(_MarginSize, 0, 1- uv.x);

					verticalMasterWeight = 0;
				}
				if (_CornerToWeld[3] > 0.5) { //BL
					horizontalMasterUv = modToUv(toGlobalUv(float2(uv.x, 0)) + float2(0, -_PixelSizeInUv));
					verticalMasterUv = modToUv(toGlobalUv(float2(0, uv.y)) + float2(-_PixelSizeInUv, 0));

					horizontalMasterWeight = invLerp(_MarginSize, 0, uv.y);
					verticalMasterWeight = invLerp(_MarginSize, 0, uv.x);
				}
				horizontalMasterWeight = saturate(horizontalMasterWeight);
				verticalMasterWeight= saturate(verticalMasterWeight);

				float4 horizontalMasterHeight = UNITY_SAMPLE_TEX2DARRAY(_HeightMap, float3(horizontalMasterUv, round(_HeightMapSliceIndex)));
				float4 verticalMasterHeight =  UNITY_SAMPLE_TEX2DARRAY(_HeightMap, float3(verticalMasterUv, round(_HeightMapSliceIndex)));

				float4 slaveSegmentHeight = UNITY_SAMPLE_TEX2DARRAY(_HeightMap, float3(globalUv, round(_HeightMapSliceIndex)));
				float slaveWeight =  1 - max(horizontalMasterWeight, verticalMasterWeight);

				float4 outHeight = slaveSegmentHeight*slaveWeight + horizontalMasterHeight*horizontalMasterWeight + verticalMasterHeight*verticalMasterWeight;
				outHeight /= (slaveWeight + horizontalMasterWeight + verticalMasterWeight);

				return outHeight;
			} 

			ENDCG
		}
	}
	FallBack "Diffuse"
}
