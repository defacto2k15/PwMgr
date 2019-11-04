
#include "common.txt"
#include "noise.hlsl"
#include "grassGeneration.hlsl"

#ifndef BILLBOARD_GRASS_SHADER_INC
#define BILLBOARD_GRASS_SHADER_INC

struct Input {
	float2 uv_MainTex;
	half debColor;
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

void billboard_surf (Input l_IN, inout SurfaceOutputStandard l_o, sampler2D l_MainTex, float l_MinUv, float l_MaxUv)
{
	l_IN.uv_MainTex.x = lerp(l_MinUv, l_MaxUv, l_IN.uv_MainTex.x);	
	fixed4 c = tex2D (l_MainTex, l_IN.uv_MainTex);
	//c.g = 0;
	//c.b = 0;
	//c.r = IN.debColor;
	l_o.Albedo = c.rgb;
	l_o.Metallic = 0.0;
	l_o.Smoothness = 0.5;
	if( l_IN.uv_MainTex.y > 0.95 ){
		l_o.Alpha = 0.0;
	} else {
		l_o.Alpha = c.a;
	}
}

void billboard_surf_characteristics (Input l_IN, inout SurfaceOutput l_o, sampler2D l_MainTex, float l_MinUv, float l_MaxUv)
{
	l_IN.uv_MainTex.x = lerp(l_MinUv, l_MaxUv, l_IN.uv_MainTex.x);	
	fixed4 c = tex2D (l_MainTex, l_IN.uv_MainTex);
	//c.g = 0;
	//c.b = 0;
	//c.r = IN.debColor;
	l_o.Albedo = c.rgb;
	//l_o.Metallic = 0.0;
	//l_o.Smoothness = 0.5;
	if( l_IN.uv_MainTex.y > 0.95 ){
		l_o.Alpha = 0.0;
	} else {
		l_o.Alpha = c.a;
	}
}

#endif