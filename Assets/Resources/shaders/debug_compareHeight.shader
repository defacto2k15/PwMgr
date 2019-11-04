Shader "Custom/Debug/CompareHeight"
{
	Properties 
	{
		_HeightmapTex ("HeightmapTex", 2D) = "white" {}
		_HeightmapSelectionScalar("HeightmapSelection", Range(0,1) ) = 0

		_DebugScalar("DebugScalar", Range(-10,10))=1
	}
	SubShader 
	{
		Cull Off ZWrite On  //Rendering settings
			CGPROGRAM
			#pragma  surface surf Standard noshadow nolightmap noambient nodynlightmap vertex:disp tessellate:tessFixed
			#pragma target 4.6

			#include "HeightColorTransform.hlsl"

			sampler2D _HeightmapTex0;
			sampler2D _HeightmapTex1;
			float _HeightmapSelectionScalar;

			float _DebugScalar;

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
				float heightmapValue0 = decodeHeight(tex2Dlod(_HeightmapTex0, float4( ring1Texture,0,0)));
				return float2(heightmapValue0, 0);
			}

			//Our Vertex Shader 
			void disp (inout appdata v){
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
				float heightmapValue0 = decodeHeight(tex2Dlod(_HeightmapTex0, float4(IN.worldPos.xz,0,0)));
				float heightmapValue1 = decodeHeight(encodeHeight(heightmapValue0));
				float diff = heightmapValue0 - heightmapValue1;
				diff *= 10000;

				float3 outColor = 0.3;
				if (diff == 0) {
					outColor.r += 0.4;
				}
				else {
					outColor.g += abs(diff);
				}

				o.Albedo = outColor;
				o.Alpha = 1; 
			}

			ENDCG
	} 
	FallBack "Diffuse"
}
