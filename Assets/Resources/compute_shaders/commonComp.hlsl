#ifndef COMMON_COMP_INC
#define COMMON_COMP_INC

#define CS_NUMTHREADS_ONE [numthreads(1,1,1)]
#define CS_NUMTHREADS_FAST [numthreads(64,1,1)]

int Compute2DIndex(uint2 pos, uint2 size) {
	return size.y*pos.y + pos.x;
}

bool threadIsInWorkSpace(uint2 id, int sideLength) {
	return id.x < sideLength && id.y < sideLength;
}

bool threadIsInWorkSpace2(uint2 id, int2 sideLengths) {
	return id.x < sideLengths.x && id.y < sideLengths.y;
}


#endif