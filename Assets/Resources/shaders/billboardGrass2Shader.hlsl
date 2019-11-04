
#include "common.txt"
#include "noise.hlsl"
#include "grassGeneration.hlsl"

#ifndef BILLBOARD_GRASS2_SHADER_INC
#define BILLBOARD_GRASS2_SHADER_INC

struct Input {
	float2 uv_MainTex;
	half debColor;
	float3 worldPos;
};

void billboard_vert(inout appdata_full l_v, out Input l_o, half l_BendingStrength,
		 half l_InitialBendingValue, half l_PlantBendingStiffness, 
		 half4 l_WindDirection, half4 l_PlantDirection, float l_RandSeed, float l_MinUv, float l_MaxUv){

	UNITY_INITIALIZE_OUTPUT(Input, l_o);
	float l = l_v.vertex.y; // height of vertex from 0 to 1
	half2 strengths = generateStrengths( l_BendingStrength, l_InitialBendingValue, l_PlantBendingStiffness, l_WindDirection, l_PlantDirection, l_RandSeed);
	half xBendStrength = strengths.x;
	half yBendStrength = strengths.y;

	half angle = lerp(-fPI()/2, fPI()/2, remap(xBendStrength));
	// angle has values from -180 deg to 180 deg in radians

	l_v.vertex.z = l * sin(angle);
	l_v.vertex.y = abs(l * cos(angle));

	// input v.vertex.x has  values from -0.5 to 0.5
	half globalVertexX = l_MinUv + (l_MaxUv - l_MinUv )* (l_v.vertex.x + 0.5);
	// globalVertexX has values from 0 to 1;
	globalVertexX += l*yBendStrength*0.6;

	// now back : to values from -0.5 to 0.5 + bending offset
	l_v.vertex.x = ((globalVertexX - l_MinUv) / (l_MaxUv - l_MinUv)) - 0.5;
	 
	l_o.debColor = l * sin(angle/4);

	l_v.normal = normalize(half3( 0, -cos(angle), sin(angle)));
}  

		void billboard_surf_characteristics2(Input l_IN, inout SurfaceOutput l_o, sampler2D l_MainTex, float l_WorldSpaceCameraPos) {
			float distanceToCamera = length(l_WorldSpaceCameraPos - l_IN.worldPos);
			float2 distanceFactorRanging = float2(10, 40);

			//float distanceToCameraFactor = _PlantBendingStiffness;
			float distanceToCameraFactor = normalizeTo(distanceFactorRanging[0], distanceFactorRanging[1], distanceToCamera);
			// 0 - close
			//1 - far

			float4 c = tex2D(l_MainTex, l_IN.uv_MainTex);
			clip(c.a - 0.01);

			
			float colorSeed = c.r;
			float3 colorDeltas = float3(0.3, 0.3, 0.3);
			float3 driverColor = float3(0.2, 0.6, 0.1);

			colorDeltas.r = lerp(-colorDeltas.r, colorDeltas.r, fmod(rand(colorSeed)*12.33,1));
			colorDeltas.g = lerp(-colorDeltas.g, colorDeltas.g, fmod(rand(colorSeed)*15.57,1));
			colorDeltas.b = lerp(-colorDeltas.b, colorDeltas.b, fmod(rand(colorSeed)*18.98,1));

			float3 outColor = driverColor +colorDeltas * (1-distanceToCameraFactor);

			float innerDistance = c.g;
			innerDistance = lerp(0.7, 1, innerDistance);
			innerDistance = pow(innerDistance, 2);

			l_o.Albedo = outColor *  lerp(innerDistance, 0.8, distanceToCameraFactor);
		}


#endif