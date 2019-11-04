Shader "Custom/TerrainTextureBombing"
	{
	Properties 
	{
		_BombsTex("BombsTex", 2D) = "white" {}
		_BombsTexCount("BombTexCount", vector) = (1,1,0,0)
		_DebugScalar("DebugScalar", range(0, 5)) = 0
		_DebugScaleX("DebugScaleX", range(0, 5)) = 1
		_DebugScaleY("DebugScaleY", range(0, 5)) = 1
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

			struct Input {
				float4 pos ; // niezbedna wartosc by dzialal shader
				half2 uv_MainTex ;
				float2 flatPos;
			};

			//Our Vertex Shader 
			void vert (inout appdata_full v, out Input o){
				o.flatPos = v.vertex.xz;
				o.pos = float4(UnityObjectToViewPos(v.vertex),0);
				o.uv_MainTex = v.texcoord.xy;
			}
    

			#include "noise.hlsl"
			#include "common.txt"

			sampler2D _BombsTex; 
			float _DebugScalar;
			float _DebugScaleX;
			float _DebugScaleY;
			float4 _BombsTexCount;

			const float PI = 3.14159;

			half2 rotateScale( half2 pos, half rotation, half2 scale){
				float sinX = sin (rotation);
				float cosX = cos (rotation);
				float sinY = sin (rotation);
				float2x2 rotationMatrix = float2x2( cosX, -sinX, sinY, cosX);
				float2x2 scaleMatrix = float2x2( scale.x, 0, 0, scale.y);
				return mul( mul(pos, rotationMatrix), scaleMatrix);
			}

			fixed isPointInShape( half2 input){
				half radiusFromCenter = length(input - half2(0.5, 0.5));
				return step(radiusFromCenter, 0.5);
			}

			//Our Fragment Shader
			void surf(in Input i, inout SurfaceOutputStandard o) {
				half2 pos = (i.flatPos/10)+fixed2(0.5, 0.5); // pos has values <0,1>

				// STANDARD STRENGTH CALCUALTING
				//half strength = 1. - length( (pos - half2(0.5, 0.5))*2);//_DebugScalar;

				half bombStandCount = 64;
				pos *= bombStandCount;
				half2 i_pos = floor(pos);
				half2 f_pos = frac(pos);

				fixed4 outColor = fixed4(0.5, 0.0, 0.5, 0.0);
				half currentPriority = -100;

				float debugScalar = 0;
				fixed seedPointRandomSeed;

				for( int i = -1; i <=1; i++){
					for(int j = -1; j <=1; j++){
						half2 neighbour = half2( float(i), float(j));
						half2 offsetAtCell = randNorm( i_pos + neighbour);
						half2 cellStartPoint = neighbour + offsetAtCell;

						half2 localUv = f_pos - cellStartPoint;
						half2 uv = localUv; 


						half scale = lerp( 0.5, 1., fmod(offsetAtCell.x*22, 1.));
						half rotation = lerp( 0., 2.*fPI(), fmod(offsetAtCell.x*13, 2*fPI()));
						uv = rotateScale(uv-0.5, rotation /*_DebugScalar*/, half2( scale /*_DebugScaleX*/, scale /*_DebugScaleY*/)) + 0.5;

						//if( min(uv.x, uv.y) >= 0 && max(uv.x, uv.y) <= 1){
						if( isPointInShape(uv) > 0){
							uv.x /= (_BombsTexCount.x*64);
							uv.y /= (_BombsTexCount.y*64);

							half2 bombOffset = floor(offsetAtCell * 1000); // neighbour is random, and i am making new random numbers from it
							debugScalar =   bombOffset / 1000;
							bombOffset.x = fmod(bombOffset.x, _BombsTexCount.x*64) / (_BombsTexCount.x*64);
							bombOffset.y = fmod(bombOffset.y, _BombsTexCount.y*64) / (_BombsTexCount.y*64);
							uv += bombOffset;


							fixed4 newColor = tex2D( _BombsTex, uv);
							half newPriority = max( neighbour, neighbour);
							seedPointRandomSeed = rand2(i_pos + neighbour+offsetAtCell); // can be generated from offsetAtCell itd

							// more intelligent strength calculating for seed point
							half2 globalCellStartPoint = cellStartPoint+i_pos;
							fixed strength =  1. - length( (globalCellStartPoint- half2(64/2, 64/2))/(64/2));

							if( newColor.a * step(seedPointRandomSeed, strength) != 0 ){
								if( newPriority > currentPriority){
									outColor = newColor;
									currentPriority = newPriority;
								}
							}
						}
					}
				}

				o.Albedo = outColor.rgb;

				debugScalar /= 6;
				//o.Albedo = float3(debugScalar, debugScalar, debugScalar);
				o.Alpha = outColor.a;			
			} 
			ENDCG
	} 
	FallBack "Diffuse"
}
