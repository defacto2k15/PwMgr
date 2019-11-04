Shader "Custom/Test/TerrainWelding"
{
	Properties
	{
		_HeightmapTex ("HeightmapTex", 2D) = "white" {}
		_HeightmapUv ("HeightmapUv", Vector) = (0.0, 0.0, 1.0, 1.0)
		_HeightmapLodOffset("HeightmapLodOffset", Range(-10,10))=0
		_WeldTexture("WeldTexture", 2D) = "white" {}
		_LeftWeldTextureUvRange("LeftWeldTextureUvRange", Vector) = (0.0, 0.0, 1.0, 1.0)
		_RightWeldTextureUvRange("RightWeldTextureUvRange", Vector) = (0.0, 0.0, 1.0, 1.0)
		_TopWeldTextureUvRange("TopWeldTextureUvRange", Vector) = (0.0, 0.0, 1.0, 1.0)
		_BottomWeldTextureUvRange("BottomWeldTextureUvRange", Vector) = (0.0, 0.0, 1.0, 1.0)
	}
		SubShader
	{
		Tags { "RenderType"="Opaque" }
		ZWrite On  //Rendering settings
			CGPROGRAM
			#pragma shader_feature DYNAMIC_NORMAL_GENERATION
			#pragma  surface surf Standard noshadow nolightmap noambient nodynlightmap vertex:disp addshadow
			#pragma target 4.6

			#include "common.txt"
			#include "UnityCG.cginc" 

			sampler2D _HeightmapTex;
			float4 _HeightmapUv;
			float _HeightmapLodOffset;
			sampler2D _WeldTexture;
			float4 _LeftWeldTextureUvRange;
			float4 _RightWeldTextureUvRange;
			float4 _TopWeldTextureUvRange;
			float4 _BottomWeldTextureUvRange;

			struct Input {
				float2 uv_HeightmapTex : TEXCOORD0;
				float3 worldPos;
			};

			struct appdata {
				float4 vertex : POSITION;
				float4 tangent : TANGENT;
				float3 normal : NORMAL;
				float2 texcoord : TEXCOORD0;
			};
			//Our Vertex Shader 
			void disp(inout appdata v, out Input o) {
				float2 baseUv = v.texcoord.xy;
				float2 heightUv = v.texcoord.xy;

				heightUv.x *= _HeightmapUv[2];
				heightUv.y *= _HeightmapUv[3];

				heightUv.x += _HeightmapUv[0];
				heightUv.y += _HeightmapUv[1];

				float mipMapIndex = _HeightmapLodOffset;
				if( min(baseUv.x, baseUv.y) < 0.0001 || max(baseUv.x, baseUv.y) > (1-0.0001) ){
					mipMapIndex = 0;
				}
				float heightmapValue = tex2Dlod(_HeightmapTex, float4( heightUv,0,mipMapIndex)).r;

				float2 distancesToMargins;
				float4 weldTextureUvs;
				// 0 - Left/Right 1 - Top/Bottom

				float4 leftRightRange;
				if (baseUv.x < 0.5) { //closer to left margin
					distancesToMargins[0] = baseUv.x;
					leftRightRange = _LeftWeldTextureUvRange;
				}
				else {
					distancesToMargins[0] = 1-baseUv.x;
					leftRightRange = _RightWeldTextureUvRange;
				}
				weldTextureUvs.xy = 
					float2(leftRightRange[0], leftRightRange[1] + baseUv.y* (leftRightRange[2] - leftRightRange[1]));
				float leftRightHeight = tex2Dlod(_WeldTexture, float4(weldTextureUvs.xy, 0, 0));

				float4 topBottomRange;
				if (baseUv.y < 0.5) { //closer to bottom margin
					distancesToMargins[1] = baseUv.y;
					topBottomRange = _BottomWeldTextureUvRange;
				}
				else {
					distancesToMargins[1] = 1-baseUv.y;
					topBottomRange = _TopWeldTextureUvRange;
				}
				weldTextureUvs.zw = 
					float2(topBottomRange[0], topBottomRange[1] + baseUv.x* (topBottomRange[2] - topBottomRange[1]));
				float topBottomHeight = tex2Dlod(_WeldTexture, float4(weldTextureUvs.zw, 0, 0));

				float startMergingMargin = 0.005;

				float2 marginWeights;
				if (leftRightRange[0] < 0) {
					marginWeights[0] = 0;
				}
				else {
					float heightDifference = abs(heightmapValue - leftRightHeight);
					marginWeights[0] =  1 - invLerpClamp(0.0, 
						lerp(
							0.001,
							startMergingMargin,
							saturate( heightDifference*40)
						),
						distancesToMargins[0]);
				}

				if (topBottomRange[0] < 0) {
					marginWeights[1] = 0;
				}
				else {
					float heightDifference = abs(heightmapValue - topBottomHeight);
					marginWeights[1] =  1 - invLerpClamp(0.0, 
						lerp(
							0.001,
							startMergingMargin,
							saturate( heightDifference*40)
						),
						distancesToMargins[1]);
				}

				float originalHeightWeight = 1 - max(marginWeights[0], marginWeights[1]);
				float weightsSum = marginWeights[0] + marginWeights[1] + originalHeightWeight;

				float finalHeight = heightmapValue* originalHeightWeight + leftRightHeight*marginWeights[0] + topBottomHeight*marginWeights[1];
				finalHeight /= weightsSum;


				v.vertex.y = finalHeight; // leftRightHeight;
				//leftRightHeight = tex2Dlod(_WeldTexture, float4(0, baseUv.y*(240/1024.0), 0, 0));
				//if (abs(heightUv.y - 0.5) > 0.4) {
				//	v.vertex.y = topBottomHeight;
				//}

				UNITY_INITIALIZE_OUTPUT(Input, o);
			}

			float3 ComputeSimpleNormal( float2 pos, float intensity){
				float3 dx = ddx( float3(pos.x, intensity, pos.y));
				float3 dy = ddy( float3(pos.x, intensity, pos.y));

				float3 lenX = length( dx.xz);
				float3 lenY = length( dy.xz);

				float sdx = dx.y / lenX;
				float sdy = dy.y / lenY;

				return normalize( float3(sdx, sdy, 1.0));
			}

			void surf(in Input IN, inout SurfaceOutputStandard o)
			{
				float3 worldPos = IN.worldPos;

				float3 normal = ComputeSimpleNormal( worldPos.xz, worldPos.y);

				float singleValue = tex2D(_WeldTexture, float2(0, 0));
				//o.Albedo = float3(0.2, 0.1, 0.8);
				o.Albedo = 1; // singleValue * 100;
				o.Normal = normal;
			}

			ENDCG
	}
	FallBack "Diffuse"
}