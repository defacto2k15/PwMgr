Shader "Custom/TerrainTextureTest2"
	{
	Properties 
	{
		_PaletteTex ("PaletteTex", 2D) = "white" {}
		_PaletteIndexTex ("PaletteIndexTex", 2D) = "white" {}
		_ControlTex ("ControlTex", 2D) = "white" {}
		_TerrainTextureSize("TerrainTextureSize", float) = 16.0
		_PaletteMaxIndex("PaletteMaxIndex", float) = 255.0

		_PaletteOffset ("PaletteOffset", Range(-1,1)) = 1.0 
		_Coords("Coords", Vector) = (0.0, 0.0, 1.0, 1.0)
	}
 
	SubShader 
	{
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		Cull Off
        LOD 200
		ZWrite On
		ColorMask 0
			CGPROGRAM
			#pragma surface surf Standard fullforwardshadows vertex:vert alpha:fade
			#include "UnityCG.cginc" 
			#include "noise.hlsl"

			struct Input {
				float4 pos ; // niezbedna wartosc by dzialal shader
				half2 uv_MainTex ;
				float2 flatPos;
			};

			//Our Vertex Shader 
			void vert (inout appdata_full v, out Input o){
				o.flatPos = v.texcoord.xy;
				o.pos = float4(UnityObjectToViewPos(v.vertex),0);
				o.uv_MainTex = v.texcoord.xy;
			}

			GEN_fractalNoise( groundFractalNoise, 6, simpleValueNoise2D, 0, 1)
			GEN_fractalNoise( groundNormalNoise, 4, simpleValueNoise2D, 0, 1)

			sampler2D _PaletteTex; 
			sampler2D _ControlTex; 
			sampler2D _PaletteIndexTex;
			float _PaletteOffset;
			float _TerrainTextureSize;
			float _PaletteMaxIndex;
			float4 _Coords;
    
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
			half2 correctTexturePos2( half2 inPos){
				float2 newPos = inPos; // <-1, 1>  
				float radius = max( abs(newPos.x), abs(newPos.y));
					// <radius has values <0, 1>
				radius *= 100; // radius:0-100
				radius = sqrt(radius); // radius:0-10

				newPos *= 10; // newPos:<-10, 10>
				newPos /= max(2, radius); // newPos: <-1, 1> 

				//return newPos;
				if (radius < 2) {
					return half2(0.1, 0.1);
				}
				return lerp( inPos, newPos, _PaletteOffset);
			} 
			// inPos has values <-1,1>
			half2 correctTexturePos( half2 inPos){
				float2 newPos = inPos; // <-1, 1>  
				float radius = max( abs(newPos.x), abs(newPos.y)); // <radius has values <0, 1>
				radius = sqrt(radius); // radius:0-1
				newPos /= max(0.2, radius); // newPos: <-1, 1> 

				return lerp( inPos, newPos, _PaletteOffset);
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

				float paletteIndex = tex2D(_PaletteIndexTex, fixed2( pos.x, pos.y)).r * _PaletteMaxIndex ;

				color[0] = tex2D(  _PaletteTex, fixed2( 0.001+ (paletteIndex * 4. + 0)/ (_PaletteMaxIndex*4.), 0))+0.001;
				color[1] = tex2D(  _PaletteTex, fixed2( 0.001+(paletteIndex * 4. + 1)/ (_PaletteMaxIndex*4.), 0))+0.001;
				color[2] = tex2D(  _PaletteTex, fixed2( 0.001+(paletteIndex * 4. + 2)/ (_PaletteMaxIndex*4.), 0))+0.001;
				color[3] = tex2D(  _PaletteTex, fixed2( 0.001+(paletteIndex * 4. + 3)/ (_PaletteMaxIndex*4.), 0))+0.001;
				
				//groundFractalNoise has values 0.25 0.25
				half2 noisePos = (inNoisePos)* 300;
				float s1 = normalizeTo(0.2, 0.8, remap(groundFractalNoise(noisePos)));
				float s2 = normalizeTo(0.2, 0.8, remap(groundFractalNoise( noisePos.yx + half2(1.4123, 9.32))));
				float margin = 0.2;
				float4 outColor = 
					lerp(
						lerp( color[0], color[1], skewedMix( control[0], margin, s1)),
						lerp( color[2], color[3], skewedMix( control[1], margin, s1)),
						skewedMix( control[2], margin, s2));

				float debugScalar = 0; //generates checkered pattern
				debugScalar +=step( 0.5/_TerrainTextureSize, fmod(abs(pos.x), 1./_TerrainTextureSize));
				debugScalar +=step( 0.5/_TerrainTextureSize, fmod(abs(pos.y), 1./_TerrainTextureSize));
				if( debugScalar > 1.9 ){
					debugScalar = 0.;
				}

				//return half4(debugScalar, debugScalar, debugScalar, 1.);
				return outColor;
			}

			//Our Fragment Shader
			void surf(in Input i, inout SurfaceOutputStandard o) {
				half2 flatPos = i.flatPos;
				flatPos.x *= _Coords[2];
				flatPos.y *= _Coords[3];
				flatPos.x += _Coords[0];
				flatPos.y += _Coords[1];
				

				flatPos -= 0.5;
				flatPos *= 2; // flatPos has: <-1,1>

				half2 newPos =  correctTexturePos( flatPos);

				o.Albedo = getGroundColor(newPos, 0);
				o.Alpha = 1;			
			} 
			ENDCG
	} 
	FallBack "Diffuse"
}
