// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/shellGrass" {
	Properties{
		_LayerHeight("LayerHeight", Range(0,1)) = 0
		_WindDirection("WindDirection", Vector) = (0.0,0.0, 0.0, 0.0)
		_BendingStrength ("BendingStrength", Range(0,1)) = 0.0
		_Scale ("Scale", Range(1,200)) = 30.0
	}
	SubShader {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		Cull Off
        LOD 200
		ZWrite On
		ColorMask 0
		//Cull Front
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows vertex:vert alpha:fade

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		 
		sampler _MainTex;
		half _LayerHeight;
		half4 _WindDirection;
		half _BendingStrength;
		half _Scale;

		#include "noise.hlsl"
		#include "color.hlsl"

		struct Input{
			float2 pos;
			float distanceToCamera;
		};

		void vert(inout appdata_full v, out Input o){
			half grassHeight = 0.2; //use uniform todo
			o.pos = v.vertex.xz;
			half3 globalVertexPosition = mul (unity_ObjectToWorld, v.vertex).xyz;
			globalVertexPosition.y -= _LayerHeight*grassHeight; //todo. At final version height will be changed by normals, so this calculation wont be needed

			o.distanceToCamera = distance(_WorldSpaceCameraPos, globalVertexPosition); 
		}  

		half3 shellGetBaseColor( half2 pos){
			pos = pos/4;
			float hueBase = fractal_improvedValueNoise2D_3(pos);   
			float saturationBase = fractal_improvedValueNoise2D_3(pos + half2(99.9, 4.12));  
			float valueBase = fractal_improvedValueNoise2D_3(pos + half2(7.7, 3.3));  

			float hue = lerp(0.25, 0.4, hueBase);
			float saturation = lerp(0.7, 0.9, saturationBase);
			float value = lerp(0.2, 0.5, valueBase); 
			   
			return HSVtoRGB(float3(hue, saturation, value) );
		}

		half getDissapearanceFactor(half distanceToCamera){
			half mostFarGrassThatIsSeen = 6; // in meters where grass disappears completly
			float alphaBlendingEnd = 4; // in meters where grass starts to disappear

			return 1 - lerp(0, 1, (distanceToCamera- alphaBlendingEnd)/(mostFarGrassThatIsSeen-alphaBlendingEnd) ); 
		}

		half2 calculateWindOffset(half4 windDirection, half scale, half layerHeight, half bendingStrength, half2 position, half2 grassDirection){
			half2 baseWindDirection = normalize(assertNotZero(windDirection).xz);
			half windDirectionRandomCoef = 0.5;
			// position is used as randomness seed!

			half2 randomWind = rand(floor(position*scale)) *windDirectionRandomCoef;
			baseWindDirection = normalize(baseWindDirection + randomWind);

			half randomTimeOffset = position.x;
			half timeFactor = lerp(-0.2,  0.2, sin(_Time[3]*2+randomTimeOffset));
	
			half windOffsetScaleFactor = 0.3;
			half grassDirectionCoef = 0.2;

			half movementCoef = pow(layerHeight,2);
			return (baseWindDirection *(bendingStrength+timeFactor) + grassDirection*grassDirectionCoef)*movementCoef* windOffsetScaleFactor;
		}

		half getPointSize( half _LayerHeight, half scale, half distanceToCamera){
			// idea -> at 5 meters and further point has size of 0.3, at 1m and closer has size 0.1, and between there is interpolation

			half distanceSizeCoef = lerp( 0.3, 0.1, 1 - invLerp(1, 5, distanceToCamera));

			return distanceSizeCoef * lerp(0.5, 1, 1-_LayerHeight) * (scale / 100); // size of one point
		}

		void surf (Input IN, inout SurfaceOutputStandard o) { 
			half2 origPos = (IN.pos + half2(5,5)) / 10;  
			half2 pos = origPos; // pos has values from 0 to 1 now

			half scale = _Scale; // def 30; // number of cells at on side

			half pointSize = getPointSize(_LayerHeight, scale, IN.distanceToCamera); 
			float margin = 0.1;

			pos *= scale;
			
			// Tile the space
			half2 i_pos = floor(pos);
			half2 f_pos = frac(pos);
			
			// Draw cell center. randomedPoint is grass position in current cell
			float dis = 99999;

			half2 tuftBasePos = i_pos + half2(margin, margin)+(1.-(margin*2.))*randNorm(i_pos); 
			const int turfCount = 5;
			for( int i = 0; i < 5; i++ ){
				half angle = ( i * 2*fPI() / turfCount);
				half2 oneGrassDirection = half2( cos( angle), sin(angle));

				half2 oneGrassPoint = tuftBasePos + oneGrassDirection*0.1;
				//pos = oneGrassPoint;
				oneGrassPoint += calculateWindOffset(_WindDirection, scale, _LayerHeight, _BendingStrength,oneGrassPoint, oneGrassDirection);

				dis = min( length(pos - oneGrassPoint), dis); 
			}

			if( dis > pointSize ){
				//discard;
			} else {
				half3 baseColor = shellGetBaseColor(pos*3); 
				o.Albedo = baseColor;
				o.Alpha = getDissapearanceFactor(IN.distanceToCamera);
			}
		} 
		ENDCG
	}
	FallBack "Diffuse"
}














