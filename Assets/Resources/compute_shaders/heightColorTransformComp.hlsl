#ifndef HEIGHT_COLOR_TRANSFORM_COMP_INC
#define  HEIGHT_COLOR_TRANSFORM_COMP_INC


float4 encodeHeight(float height){
	return float4(
		floor(height*256)/256,
		fmod(height*256,1),
		fmod(height*256*256,1),
		fmod(height*256*256*256,1)
	);
}

float decodeHeight(float4 input){
	return input.r + input.g/256 + input.b/(256*256) + input.a/(256*256*256);
}

#endif
