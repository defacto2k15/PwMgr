Shader "Custom/Terrain/DebugLod"
{
	Properties
	{
		_LodLevel("LodLevel", Range(-10,10)) = 1
		_NodeId("NodeId", Range(-10,10)) = 1
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
			}

			void surf(in Input IN, inout SurfaceOutputStandard o)
			{
				float3 outColor;

				if (_LodLevel > 10) {
					outColor = float3(0.3, 1, 0.7);
				} else if (_LodLevel > 9) {
					outColor = float3(0, 1, 1);
				}else if (_LodLevel > 8) {
					outColor = float3(0.5, 0, 0.5);
				} else if (_LodLevel > 7) {
					outColor = float3(1, 1, 1);
				}
				else if (_LodLevel > 6) {
					outColor = float3(1, 0, 0);
				}
				else if (_LodLevel > 5) {
					outColor = float3(0.5, 0, 0);
				}
				else if (_LodLevel > 4) {
					outColor = float3(0, 1, 0);
				}
				else if (_LodLevel > 3) {
					outColor = float3(0, 0.5, 0);
				}
				else if (_LodLevel > 2) {
					outColor = float3(0, 0, 1);
				}
				else if (_LodLevel > 1) {
					outColor = float3(0, 0, 0.5);
				}
				else {
					outColor = float3(1, 0, 1); 
				}
				
				o.Albedo = outColor + float3(0.2*rand(_NodeId), 0.2*rand(_NodeId + 0.1), 0.2*rand(_NodeId + 0.2));
			}

			ENDCG
	}
		FallBack "Diffuse"
}

