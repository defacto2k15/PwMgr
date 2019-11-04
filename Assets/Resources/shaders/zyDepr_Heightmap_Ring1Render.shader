Shader "Custom/Heightmap/Ring1Creator"
{
	Properties 
	{
		_MainTex ("", 2D) = "white" {}
		_Seed("Seed", Range(0,100)) = 0.0
		_HeightMultiplier("HeightMultiplier", Range(1,10000))=1
		_InputAndOutputTextureSize("InputAndOutputTextureSize", Vector) = (1,1,1,1)
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
			//we include "UnityCG.cginc" to use the appdata_img struct

			#include "noise.hlsl"
			#include "HeightColorTransform.hlsl"

			GEN_fractalNoise( fractal_simplexNoise_7, 7, snoise2D, 0, 1)
    
			struct v2f {
				float4 pos : POSITION; // niezbedna wartosc by dzialal shader
				float2 uv : TEXCOORD0;
			};
   
			//Our Vertex Shader 
			v2f vert (appdata_img v){
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex); //niezbedna linijka by dzialal shader
				//o.uv =  MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord.xy); // it was like this before, but now it is not needed
				o.uv =   v.texcoord.xy; 
				return o; 
			}
    
			sampler2D _MainTex; //Reference in Pass is necessary to let us use this variable in shaders
			float _Seed;
			float _HeightMultiplier;


			// Parameters:
			// H is fractal increment
			// lacunarity is gap between succesive frequencies
			// octaves is number of frequencies in the fbm
			float fBm1( float2 pos, float H, float lacunarity, int octaves ){
				float frequency = 1.0f;
				float value = 0.0f;
				float remainder;
				for( int i = 0; i < octaves; i++){
					value += snoise2D(pos) * pow(frequency, -H);
					pos *= lacunarity;
					frequency *= lacunarity;					
				}
				remainder = octaves - floor(octaves);
				if( remainder > 0){
					value += remainder * snoise2D(pos) * pow(frequency, -H);
				}
				return value; 
			}

			//Hybrid additive/multiplicative multifractal terrain model.
			// Offset jest uzywany aby zamienić dziedzinę funkcji losowej z <-1,1> do bardziej 0,2
			float hybridMultifractal( float2 pos, float H, float lacunarity, int octaves, float offset ){
				float frequency = 1.0f;
				float value = 0.0f;
				float remainder;

				value = snoise2D(pos)*2 + offset * pow(frequency, -H);
				frequency *= lacunarity; //precomputing first octave					
				pos *= lacunarity;
				float weight = value;

				for( int i = 1; i < octaves; i++){ //we start from 1
					weight = min(weight, 1.0f);

					float signal = (snoise2D(pos)*2 + offset) * pow(frequency, -H);
					value += weight * signal;

					weight *= signal;

					pos *= lacunarity;
					frequency *= lacunarity;					
				}
				remainder = octaves - floor(octaves);
				if( remainder > 0){
					value += remainder * snoise2D(pos)*2 * pow(frequency, -H);
				}
				return value; 
			}

			// simple distortion - Bryce's Laval terrain Model
			float WarpedFBm(float2 pos, float distortion ){
				float2 tmp = pos;
				float2 distort;

				distort.x = fractal_simplexNoise_7( tmp);
				tmp.x += 12.3f;
				distort.y =  fractal_simplexNoise_7( tmp);

				pos += (distortion * distort);
				return  fBm1( pos*2, 2.25f, 3.0f, 9.0f )/4;
			}

			float terrainGeneratingFunc(float2 pos){
				float H = 0.25f;
				float lacunarity = 2.0f;
				float octaves = 9;
				float offset = 0.7;

				//return fBm1(pos, H, lacunarity, octaves)/2;
				//return  max(0/2, hybridMultifractal(pos, H, lacunarity, octaves, 0)/10);
				return WarpedFBm(pos, 0.9);
			}

			float2 backToNormalizedUV(float2 uv, float2 textureSize){
				return uv / textureSize;
			}

			float takeValueFromTextureWithInterpolation(float2 uv, float2 textureSize){
				float2 positionInTextureSpace = uv * textureSize;
				float2 integerPosition = floor(positionInTextureSpace);

				float v1 = tex2D(_MainTex, backToNormalizedUV(integerPosition, textureSize));
				float v2 = tex2D(_MainTex, backToNormalizedUV(integerPosition + float2(1,0), textureSize));
				float v3 = tex2D(_MainTex, backToNormalizedUV(integerPosition + float2(1,1), textureSize));
				float v4 = tex2D(_MainTex, backToNormalizedUV(integerPosition + float2(0,1), textureSize));

				float2 weight = frac(positionInTextureSpace);
				float4 bottom = lerp( 
						tex2D(_MainTex, backToNormalizedUV(integerPosition, textureSize)),
						tex2D(_MainTex, backToNormalizedUV(integerPosition + float2(1,0), textureSize)),
						weight.x);

				float4 top = lerp( 
						tex2D(_MainTex, backToNormalizedUV(integerPosition + float2(0,1), textureSize)),
						tex2D(_MainTex, backToNormalizedUV(integerPosition + float2(1,1), textureSize)),
						weight.x);
				
				//return weight.x;
//				if(  ( abs(v1-v2)<0.001) && ( abs(v2-v3)<0.001) && ( abs(v3-v4)<0.001) ){
//					return 1;
//				} TODO
				return lerp(bottom, top, weight.y);
			}

			float4 _InputAndOutputTextureSize;
    
			//Our Fragment Shader
			fixed4 frag (v2f i) : Color{
				// i.uv.x has values from 0 to 0.125
				// i.uv.y has values from 0.875 to 1

				float xMultiplier =  _InputAndOutputTextureSize[0]/ _InputAndOutputTextureSize[2];
				float yMultiplier =  _InputAndOutputTextureSize[1]/ _InputAndOutputTextureSize[3];

				//i.uv.x = i.uv.x * 8; // this works then outtexture was 256/256
				//i.uv.y = i.uv.y- (1.0f-(1.0f/yMultiplier))*yMultiplier;

				// sample texture and return it

				float2 realPos = i.pos.xy / 2048;

				fixed value = takeValueFromTextureWithInterpolation( realPos, float2(256,256)).r;
				//fixed value = tex2D(_MainTex, float4(i.uv, 0, 0));
				//value += terrainGeneratingFunc(realPos);// * _HeightMultiplier; // was i.uv * 22
				 
				return encodeHeight(value);
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}