Shader "Custom/Misc/RandomImageGenerator"
	{
	Properties
	{
		_Coords("Coords", Vector) = (0.0, 0.0, 10.0, 10.0)
		_Seed("Seed", Range(0, 1000)) = 0.0
		_DebugTop("DebugTop", Range(0,2)) = 1
		_DebugBottom("DebugBottom", Range(0,2)) = 1
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
    
			struct v2f {
				float4 pos : POSITION; // niezbedna wartosc by dzialal shader
				half2 uv : TEXCOORD0;
			};
   
			//Our Vertex Shader 
			v2f vert (appdata_img v){
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex); //niezbedna linijka by dzialal shader
				o.uv = MultiplyUV (UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
				return o; 
			}
    
			float4 _Coords;
			float _Seed;

			float _DebugTop;
			float _DebugBottom;
    
			#include "common.txt"
			#include "noise.hlsl"

			float myRemap(float initial, float multiplier) {
				return remap(initial) * multiplier;
			}

#define GEN2_fractalNoise( noiseName, count, changeTop, changeBottom, positionMultiplier ) \
			GEN_fractalNoise( fractalFunc, count, noiseName, 0.0, changeTop); \
			float noiseFunc(float2 x) { \
				return myRemap( fractalFunc(x * positionMultiplier), changeBottom); \
			}

GEN_fractalNoise( ri_fractal_simpleValueNoise2D_1, 1, simpleValueNoise2D, 0.0, _DebugTop) //1.00 0.96
GEN_fractalNoise( ri_fractal_simpleValueNoise2D_3, 3, simpleValueNoise2D, 0.0, _DebugTop) //0.79 0.99
GEN_fractalNoise( ri_fractal_simpleValueNoise2D_5, 5, simpleValueNoise2D, 0.0, _DebugTop) //0.74 0.99
GEN_fractalNoise( ri_fractal_simpleValueNoise2D_7, 7, simpleValueNoise2D, 0.02, _DebugTop) //0.76 1.01

GEN_fractalNoise( ri_fractal_improvedPerlinNoise2D_3, 2, snoise2D, 0, _DebugTop) // 0.87 1.26
GEN_fractalNoise( ri_fractal_improvedPerlinNoise2D_10, 10, snoise2D, 0, _DebugTop) //0.79 1.31

//#define noiseFunc( x ) remap( ri_fractal_improvedPerlinNoise2D_10( x ) ) 
//\#define noiseFunc( x ) myRemap( ri_fractal_improvedPerlinNoise2D_3( x ), _DebugBottom ) 

GEN2_fractalNoise(simpleValueNoise2D, 7, 0.79, 0.99, 0.9 )
			
			//Our Fragment Shader
			fixed4 frag (v2f i) : Color{
				float2 inPos = i.uv;
				inPos.x *= _Coords[2];
				inPos.y *= _Coords[3];
				
				float2 pos = inPos + float2(_Coords[0], _Coords[1]);
				pos.x += _Seed * 162.64212;
				pos *= 0.15;

				float scalar = noiseFunc(pos);

				float3 outColor = 0;
				if (scalar < 0.00001) {
					outColor = float3(0, 1, 1);
				}
				else if (scalar > 0.99999) {
					outColor = float3(1, 0, 1);
				}
				else {
					outColor = float3(scalar, 0, 0);
				}

				return fixed4(outColor, 1);
			}
			ENDCG
		}
	} 
	FallBack "Diffuse"
}
