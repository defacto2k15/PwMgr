Shader "Custom/Terrain/Ring1"
{
	Properties 
	{
		_HeightmapTex ("HeightmapTex", 2D) = "white" {}
		_HeightmapUv ("HeightmapUv", Vector) = (0.0, 0.0, 1.0, 1.0)
		_NormalmapTex ("NormalmapTex", 2D) = "white" {}
		_NormalmapUv ("NormalmapUv", Vector) = (0.0, 0.0, 1.0, 1.0)

		_DebugScalar("DebugScalar", Range(-10,10))=1
		_LodLevel("LodLevel", Range(-10,10))=1
		_HeightmapLodOffset("HeightmapLodOffset", Range(-10,10))=0

		_PaletteTex ("PaletteTex", 2D) = "white" {}
		_PaletteIndexTex ("PaletteIndexTex", 2D) = "white" {}
		_ControlTex ("ControlTex", 2D) = "white" {}
		_TerrainTextureSize("TerrainTextureSize", float) = 16.0
		_PaletteMaxIndex("PaletteMaxIndex", float) = 255.0
		_PaletteOffset ("PaletteOffset", Range(-1,1)) = 1.0 
		_TerrainStainUv("TerrainStainUv", Vector) = (0.0, 0.0, 1.0, 1.0)

		_WeldTexture("WeldTexture", 2D) = "white" {}
		_LeftWeldTextureUvRange("LeftWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
		_RightWeldTextureUvRange("RightWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
		_TopWeldTextureUvRange("TopWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)
		_BottomWeldTextureUvRange("BottomWeldTextureUvRange", Vector) = (-1.0, -1.0, -1.0, -1.0)

		_DebControl0("DebugControl0", Range(0,1)) = 0.0
		_DebControl1("DebugControl1", Range(0,1)) = 0.0
		_DebControl2("DebugControl2", Range(0,1)) = 0.0
	}
	SubShader 
	{
		Cull Off ZWrite On  //Rendering settings
			CGPROGRAM
			#pragma  surface surf Standard addshadow nolightmap noambient nodynlightmap vertex:disp tessellate:tessFixed
			#pragma target 4.6
			#pragma shader_feature DYNAMIC_NORMAL_GENERATION
			#include "common.txt"
			#include "UnityCG.cginc" 
			#include "noise.hlsl"

			sampler2D _HeightmapTex;
			float4 _HeightmapUv;
			sampler2D _NormalmapTex;
			float4 _NormalmapUv;

			float _DebugScalar;
			float _HeightmapLodOffset;

			sampler2D _PaletteTex; 
			sampler2D _ControlTex; 
			sampler2D _PaletteIndexTex;
			float _PaletteOffset;
			float _TerrainTextureSize;
			float _PaletteMaxIndex;
			float4 _TerrainStainUv;

			sampler2D _WeldTexture;
			float4 _LeftWeldTextureUvRange;
			float4 _RightWeldTextureUvRange;
			float4 _TopWeldTextureUvRange;
			float4 _BottomWeldTextureUvRange;

			float _DebControl0;
			float _DebControl1;
			float _DebControl2;

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
				float heightmapValue = tex2Dlod(_HeightmapTex, float4( ring1Texture,0,5)).r;
				return float2(heightmapValue, heightmapValue);
			}

			#include "terrain.hlsl"   
			//Our Vertex Shader 
			void disp (inout appdata v){
				v.vertex.y =  CalculateHeightWithMargins(v.texcoord.xy);  
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

			//////////////////////////////////////////////////////
			///// STAIN TERRAIN //////////////////////////////////
			//////////////////////////////////////////////////////

			GEN_fractalNoise( groundFractalNoise, 6, simpleValueNoise2D, 0, 1)
			GEN_fractalNoise( groundNormalNoise, 4, simpleValueNoise2D, 0, 1)

			////////////// mix-shader
			fixed skewedMix( fixed control, fixed margin, fixed value){
				float stX = -margin + control*(1.0 + margin);
				float enX = control * (1.0+margin);
				if( value > enX){
					return 1.0;
				} else if (value < stX){
					return 0.;
				} else {
					return (value - stX)/(enX - stX);
				}
			}


			// inPos has values <-1,1>
			half2 correctTexturePos( half2 inPos){
				return inPos;
				float2 newPos = inPos; // <-1, 1>  //TODO
				float radius = max( abs(newPos.x), abs(newPos.y)); // <radius has values <0, 1>
				radius = lerp(0.46, 0.34, radius); // radius:0-1
				newPos /= max(0.316, radius); // newPos: <-1, 1> 

				if (radius < 0.01) {

				}

				return lerp(inPos, newPos, _PaletteOffset);
			} 


			// pos has values <-1, 1>
			half4 getGroundColor( half2 pos, half radius){
				pos = correctTexturePos(pos);
				half2 inNoisePos = pos;

				pos /= 2;// 
				pos += half2(0.5, 0.5);

				float offset = (1.0/_TerrainTextureSize)/2  ;

				float4 control = tex2D( _ControlTex, fixed2(pos.x + offset, pos.y+offset));
				float4 color[4];

				float paletteIndex = tex2D(_PaletteIndexTex, fixed2(pos.x, pos.y)).r * _PaletteMaxIndex;

				color[0] = tex2D(  _PaletteTex, fixed2( 0.00+ (paletteIndex * 4. + 0 )/ (_PaletteMaxIndex*4.), 0))+0.001;
				color[1] = tex2D(  _PaletteTex, fixed2( 0.00+(paletteIndex * 4. + 1)/ (_PaletteMaxIndex*4.), 0))+0.001;
				color[2] = tex2D(  _PaletteTex, fixed2( 0.00+(paletteIndex * 4. + 2)/ (_PaletteMaxIndex*4.), 0))+0.001;
				color[3] = tex2D(  _PaletteTex, fixed2( 0.00+(paletteIndex * 4. + 3)/ (_PaletteMaxIndex*4.), 0))+0.001;
				
				//groundFractalNoise has values 0.25 0.25
				float DotSizeFactor = 30;
				half2 noisePos = (inNoisePos)* DotSizeFactor;
				float s1 = normalizeTo(0.2, 0.8, remap(groundFractalNoise(noisePos)));
				float s2 = normalizeTo(0.2, 0.8, remap(groundFractalNoise( noisePos.yx + half2(1.4123, 9.32))));
				float margin = 0.2;
				float4 outColor = 
					lerp(
						lerp( color[0], color[1], skewedMix( control[0], margin, s1)),
						lerp( color[2], color[3], skewedMix( control[1], margin, s1)),
						skewedMix( control[2], margin, s2));

				return outColor;
			}

			//////////////////////////////////////////////////////
			///// SURF SHADER ////////////////////////////////////
			//////////////////////////////////////////////////////
    
			void surf (in Input IN, inout SurfaceOutputStandard o)  
			{
				float3 worldPos = IN.worldPos;
				float2 normalUv = IN.uv_HeightmapTex;// IN.worldPos.xz /*/ 10 ;*/;

				normalUv.x *= _NormalmapUv[2];
				normalUv.y *= _NormalmapUv[3];

				normalUv.x += _NormalmapUv[0];
				normalUv.y += _NormalmapUv[1];

				float3 normal;

//#ifdef DYNAMIC_NORMAL_GENERATION
//				normal = ComputeSimpleNormal( worldPos.xz, worldPos.y)*4;
//#else
				float3 encodedNormal = tex2D(_NormalmapTex, normalUv);
				normal = decodeNormal(encodedNormal);
//#endif
				
				float2 stainUv = IN.uv_HeightmapTex;


				stainUv.x *= _TerrainStainUv[2]; 
				stainUv.y *= _TerrainStainUv[3];
				stainUv.x += _TerrainStainUv[0];
				stainUv.y += _TerrainStainUv[1];
				
				stainUv -= 0.5;
				stainUv *= 2; // stainUv has: <-1,1>

				half2 skewedStainUv = correctTexturePos(stainUv);

				o.Albedo = getGroundColor(skewedStainUv, 0)*0.6;

				float3 shitNorm = normalize(float3(normal.x, normal.y / 20, normal.z));
				o.Normal = normalize(normal.xyz);
				o.Alpha = 1; 

			}

			ENDCG
	} 
	FallBack "Diffuse"
}
