Shader "Custom/Terrain/Terrain_Mei_Debug_Comparision_StepByStep"
{
	Properties 
	{
		_HeightmapSelectionScalar("HeightmapSelection", Range(0,10) ) = 0
		_HeightmapTexArray("HeightmapTexArray", 2DArray) = "white"{}

		_DetailTexArray("DetailTexArray", 2DArray) = "white"{}
		_DetailTexLength("DetailTexLength", Range(0,100)) = 10

		_DebugScalar("DebugScalar", Range(0,1))=0
		_MultiplyDelta("MultiplyDelta", Range(0,100))=1
		_DetailTextureSelector("DetailTextureSelector", Range(0,6)) = 0
		_SnapshotSelector("SnapshotSelector", Range(0,100)) = 0
		_SuppahDebugValue("Suppah", range(0,100)) = 0
	}
	SubShader 
	{
		Cull Off ZWrite On  //Rendering settings
			CGPROGRAM
			#pragma  surface surf Standard noshadow nolightmap noambient nodynlightmap vertex:disp tessellate:tessFixed
			#pragma target 4.6

			#include "HeightColorTransform.hlsl"

			UNITY_DECLARE_TEX2DARRAY(_HeightmapTexArray);

			UNITY_DECLARE_TEX2DARRAY(_DetailTexArray);
			float _DetailTexLength;

			float _HeightmapSelectionScalar;
			float _DebugScalar;
			float _MultiplyDelta;
			float _DetailTextureSelector;
			float _SnapshotSelector;
			float _SuppahDebugValue;

			struct Input{
				float2 uv_HeightmapTex : TEXCOORD0;
				float3 worldPos;
			};

			struct appdata {
				float4 vertex : POSITION;
				float4 tangent : TANGENT; 
				float3 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
			};


			float4 tessFixed(appdata v0, appdata v1, appdata v2){
				return 1;
			}

			float2 backToNormalizedUV(float2 uv, float2 textureSize){
				return uv / textureSize;
			}


			float2 getHeightmapTextureValues( float2 uvPos){ //position in object
				float2 ring1Texture = uvPos;

				float baseHeight = decodeHeight(UNITY_SAMPLE_TEX2DARRAY_LOD(_HeightmapTexArray, float3( ring1Texture,0), 0));
				float compHeight = decodeHeight(UNITY_SAMPLE_TEX2DARRAY_LOD(_HeightmapTexArray, float3( ring1Texture,_HeightmapSelectionScalar), 0));
				if (_HeightmapSelectionScalar > 5) {
					float detailIndex = round(_DetailTextureSelector - 1)*round(_DetailTexLength) + clamp(0, _DetailTexLength-1, round(_SnapshotSelector));
					float snapshot =UNITY_SAMPLE_TEX2DARRAY_LOD(_DetailTexArray, float3( uvPos,detailIndex), 0);
					compHeight = snapshot;

				}
				return float2(lerp( baseHeight, compHeight, _DebugScalar),  baseHeight-compHeight);
			}

			//Our Vertex Shader 
			void disp (inout appdata v){
				float2 hv =  getHeightmapTextureValues(v.texcoord.xy) ;
				v.vertex.y += hv.x;
			}

			float3 decodeNormal( float3 input){
				return input*2-1;
			}

			float3 encodeNormal( float3 input ){
				return (input+1)/2;
			}

			float3 ComputeSimpleNormal( float2 pos, float intensity, float normalStrength){
				float3 dx = ddx( float3(pos.x, intensity, pos.y));
				float3 dy = ddy( float3(pos.x, intensity, pos.y));

				float3 lenX = length( dx.xz);
				float3 lenY = length( dy.xz);

				float sdx = dx.y / lenX;
				float sdy = dy.y / lenY;

				sdx *= (0.06 * normalStrength);
				sdy *= (0.06 * normalStrength);

				return normalize( float3(sdx, sdy, 1.0));
			}

    
			void surf (in Input IN, inout SurfaceOutputStandard o)  
			{
				float3 funkyNormals = encodeNormal( ComputeSimpleNormal(IN.worldPos.xz, IN.worldPos.y, 1));
				//o.Albedo = IN.worldPos.y;
				//o.Albedo.r = step(IN.heightDifference, _DebugScalar);
				float2 uv = IN.worldPos.xz / 10;
				float2 hv = getHeightmapTextureValues(uv);
				hv.y *= _MultiplyDelta;

				float difference = hv.y;
				float detailIndex = round(_DetailTextureSelector - 1)*round(_DetailTexLength) + clamp(0, _DetailTexLength-1, round(_SnapshotSelector));
				float snapshot =UNITY_SAMPLE_TEX2DARRAY_LOD(_DetailTexArray, float3( uv,detailIndex), 0);


				float3 outColor = 0.0;
				if (_DetailTextureSelector < 1) {
					outColor = 0.3;
					if (hv.y > 0) {
						outColor.r += hv.y * 1000;
					}
					else {
						outColor.g += abs(hv.y) * 1000;
					}
				}
				else {
					outColor.g = snapshot *_MultiplyDelta;
				}

				float2 intUv = (round(uv * 240));
				if (intUv.x == 60 && intUv.y == 60) {
					outColor = 1;
				}

				o.Albedo = outColor;
				o.Normal = funkyNormals;
				o.Alpha = 1; 
			}

			ENDCG
	} 
	FallBack "Diffuse"
}
