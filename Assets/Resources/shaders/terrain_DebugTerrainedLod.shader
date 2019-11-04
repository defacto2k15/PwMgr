Shader "Custom/Terrain/DebugTerrainedLod"
{
	Properties
	{
		_LodLevel("LodLevel", Range(-10,10)) = 1
		_NodeId("NodeId", Range(-10,10)) = 1
		_HeightmapTex ("HeightmapTex", 2D) = "white" {}
		_HeightmapUv ("HeightmapUv", Vector) = (0.0, 0.0, 1.0, 1.0)
		_NormalmapTex ("NormalmapTex", 2D) = "white" {}
		_NormalmapUv ("NormalmapUv", Vector) = (0.0, 0.0, 1.0, 1.0)
		_HeightmapLodOffset("HeightmapLodOffset", Range(-10,10))=0
	}
		SubShader
	{
		Cull Off ZWrite On  //Rendering settings
			CGPROGRAM
			#pragma  surface surf Standard noshadow nolightmap noambient nodynlightmap vertex:disp 
			#pragma target 4.6

			#include "common.txt"
			#include "UnityCG.cginc" 
			#include "noise.hlsl"

			float _LodLevel;
			float _NodeId;
			sampler2D _HeightmapTex;
			float4 _HeightmapUv;
			sampler2D _NormalmapTex;
			float4 _NormalmapUv;
			float _HeightmapLodOffset;

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
			void disp(inout appdata v) {
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

				v.vertex.y += heightmapValue;
			}

			void surf(in Input IN, inout SurfaceOutputStandard o)
			{
				float3 colorsArray[8];
				colorsArray[0] = float3(1, 0, 0);
				colorsArray[1] = float3(0.5, 0, 0);
				colorsArray[2] = float3(0, 1, 0);
				colorsArray[3] = float3(0, 0.5, 0);
				colorsArray[4] = float3(0, 0, 1);
				colorsArray[5] = float3(0, 0, 0.5);
				colorsArray[6] = float3(1, 0, 1);
				colorsArray[7] = float3(0, 1, 1);
				
				int iLodLevel = (int)round(fmod(_LodLevel, 8));
				
				float3 outColor = colorsArray[iLodLevel];

				
				o.Albedo = outColor + float3(0.2*rand(_NodeId), 0.2*rand(_NodeId + 0.1), 0.2*rand(_NodeId + 0.2));
			}

			ENDCG
	}
		FallBack "Diffuse"
}