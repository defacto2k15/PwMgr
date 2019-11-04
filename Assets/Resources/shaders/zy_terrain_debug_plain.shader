Shader "Custom/Terrain/Terrain_Debug_Plain"
{
	Properties 
	{
		_HeightmapTex0 ("HeightmapTex0", 2D) = "white" {}
		_HeightmapTex1 ("HeightmapTex1", 2D) = "white" {}
		_HeightmapSelectionScalar("HeightmapSelection", Range(0,1) ) = 0

		_DebugScalar("DebugScalar", Range(-10,10))=0
	}
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		ZWrite On  //Rendering settings
			CGPROGRAM
			#pragma  surface surf Standard noshadow nolightmap noambient nodynlightmap vertex:disp addshadow
			#pragma target 4.6

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
				float heightmapValue0 = tex2Dlod(_HeightmapTex0, float4( ring1Texture,1,0)).r;
				float heightmapValue1 = tex2Dlod(_HeightmapTex1, float4( ring1Texture,0,0)).r;
				return float2(lerp( heightmapValue0, heightmapValue1, _HeightmapSelectionScalar), heightmapValue0 - heightmapValue1);
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
				float2 uv = IN.worldPos.xz / 10 ;
				float2 hv = getHeightmapTextureValues(uv);

				float3 outColor = 0.3;
				if (hv.y > 0) {
					outColor.r += hv.y * 1000 * _DebugScalar;
				}
				else {
					outColor.g += abs(hv.y) * 1000 * _DebugScalar;
				}

				o.Albedo =  outColor;
				o.Normal =  ComputeSimpleNormal(IN.worldPos.xz, IN.worldPos.y*5, 6);
				o.Alpha = 1; 
			}

			ENDCG
	} 
	FallBack "Diffuse"
}
