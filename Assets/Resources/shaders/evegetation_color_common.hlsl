#ifndef EVEGETATION_COLOR_COMMON_HLSL
#define EVEGETATION_COLOR_COMMON_HLSL

static float3 evegetation_brownPattern = float3(132,77,16) / 255.0;
static float3 evegetation_greenPattern = float3(107,178,16) / 255.0;

float3 EVegetationCalculateColor(bool colorMarker, float3 propColor) {
	if (colorMarker) {
		return propColor; 
	}
	else {
		return evegetation_brownPattern* (0.5+((rand(propColor.r)*0.5)-0.25)); //TODO some intelligent implementation
	}
}

#endif