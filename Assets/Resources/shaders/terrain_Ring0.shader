Shader "Custom/Terrain/Ring0"
{
	Properties
	{
		_HeightmapTex ("HeightmapTex", 2D) = "white" {}
		_HeightmapUv ("HeightmapUv", Vector) = (0.0, 0.0, 1.0, 1.0)
		_NormalmapTex ("NormalmapTex", 2D) = "white" {}
		_NormalmapUv ("NormalmapUv", Vector) = (0.0, 0.0, 1.0, 1.0)
		_HeightmapLodOffset("HeightmapLodOffset", Range(-10,10))=0

		_WeldTexture("WeldTexture", 2D) = "white" {}
		_LeftWeldTextureUvRange("LeftWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
		_RightWeldTextureUvRange("RightWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
		_TopWeldTextureUvRange("TopWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
		_BottomWeldTextureUvRange("BottomWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
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
			#include "noise.hlsl"

			sampler2D _HeightmapTex;
			float4 _HeightmapUv;
			sampler2D _NormalmapTex;
			float4 _NormalmapUv;
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

			#include "terrain.hlsl"
			//Our Vertex Shader 
			void disp(inout appdata v, out Input o) {
				v.vertex.y =  CalculateHeightWithMargins(v.texcoord.xy);  
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

//GEN_fractalNoise( fractal_improvedPerlinNoise2D_3, 3, snoise2D, 0.3, 0.55)

			void surf(in Input IN, inout SurfaceOutputStandard o)
			{
				float3 worldPos = IN.worldPos;
				float2 normalUv = IN.uv_HeightmapTex;// IN.worldPos.xz /*/ 10 ;*/;

				normalUv.x *= _NormalmapUv[2];
				normalUv.y *= _NormalmapUv[3];

				normalUv.x += _NormalmapUv[0];
				normalUv.y += _NormalmapUv[1];

				float3 normal;
//#ifdef DYNAMIC_NORMAL_GENERATION
				//normal = ComputeSimpleNormal( worldPos.xz, worldPos.y);
//#else
				float3 encodedNormal = tex2D(_NormalmapTex, normalUv);
				normal = decodeNormal(encodedNormal);

				//normal.y *= 50;
				//normal = normalize(normal);
//#endif

				float3 downColors[2];
				downColors[0] = float3(0.293, 0.451, 0.256);
				downColors[1] = float3(0.624, 0.565, 0.486);


				float3 topColors[2];
				topColors[0] = float3(0.141, 0.291, 0.09);
				topColors[1] = float3(0.199, 0.278, 0.19)/2;

				float3 sideColors[2];
				sideColors[0] = float3(0.073, 0.30, 0.05);
				sideColors[1] = float3(0.11, 0.235, 0.04);

				float2 noisePos = IN.worldPos.xz / 200;

				float noiseValue = fractal_simpleValueNoise2D_3(float2(
					fractal_simpleValueNoise2D_3(noisePos),
					fractal_simpleValueNoise2D_3(noisePos + float2(432.21, -264.8))
					));

				noiseValue = remap(noiseValue);
				
				
				float mapHeight = tex2Dlod(_HeightmapTex, float4( normalUv,0,0)).r;
				float height = DenormalizePlainHeight(mapHeight);
				float angle = abs(dot(normal, float3(0, 1, 0)));

				float3 color;
				if (angle > 0.25) {
					color = lerp(sideColors[0], sideColors[1], noiseValue);
				}else if (height < 800) {
					color = lerp(downColors[0], downColors[1], noiseValue);
				}
				else {
					color = lerp(topColors[0], topColors[1], noiseValue);
				}

				o.Albedo = color;
				o.Normal = normal;
			}

			ENDCG
	}
	FallBack "Diffuse"
}