#ifndef COMMON_COMP_INC
#define COMMON_COMP_INC

#define CS_NUMTHREADS_ONE [numthreads(1,1,1)]

int Compute2DIndex(uint2 pos, uint2 size) {
	return size.y*pos.y + pos.x;
}

float3 encodeNormal(float3 normal){
	return (normalize(normal) + 1)/2;
}

float3 decodeNormal(float3 encoded){
	return (encoded -0.5 )*2;
}

#endif