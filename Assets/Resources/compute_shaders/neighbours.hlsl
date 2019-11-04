#ifndef NEIGHBOURS_INC
#define NEIGHBOURS_INC

#include "structureGeneration.hlsl"
#include "commonComp.hlsl"

GENERATE_CLASS_2( OneNeighbourData,
	uint2, position,
	float, heightDifference )

#if defined(VARIANT_NHOOD_BIG9)
#define NEIGHBOURHOOD_COUNT 8
#elif defined(VARIANT_NHOOD_CROSS4)
#define NEIGHBOURHOOD_COUNT 4
#elif defined(VARIANT_NHOOD_X4)
#define NEIGHBOURHOOD_COUNT 4
#else
#error "NO NEIGHBOURHOOD DEFINITIONS FOUND!"
#endif


struct NeighboursList{
	OneNeighbourData array[NEIGHBOURHOOD_COUNT];
};

void NeighboursList_add( inout NeighboursList list, OneNeighbourData valueToAdd, int neighbourPos) {
	list.array[neighbourPos] = valueToAdd;  
}

bool is_OneNeighbourData_active(uniform OneNeighbourData data) {
	return data.position.x + data.position.y < 18000;
}

int NeighboursList_countActive( in NeighboursList list) {
	int outValue = 0;
	for (int i = 0; i < NEIGHBOURHOOD_COUNT; i++) {
		if (is_OneNeighbourData_active(list.array[i])) {
			outValue++;
		}
	}
	return outValue;
}


NeighboursList new_NeighboursList() {
	NeighboursList list;
	for (int i = 0; i < NEIGHBOURHOOD_COUNT; i++) {
		list.array[i] = new_OneNeighbourData(int2(9999, 9999), 0.0);
	}
	return list;
}


NeighboursList FindNeighbours(uint2 position, uint2 size) {
	uint x = position.x;
	uint y = position.y;

	NeighboursList outList = new_NeighboursList();

#if defined(VARIANT_NHOOD_BIG9)
	if (x > 0 && y > 0) {
		NeighboursList_add(outList, new_OneNeighbourData(position + int2(-1, -1), 0.0), 0);
	}
	if (x > 0) {
		NeighboursList_add(outList, new_OneNeighbourData( uint2(-1, 0), 0.0),1);
	}
	if (x > 0 && y < size.y-1) {
		NeighboursList_add(outList, new_OneNeighbourData(position + uint2(-1, 1), 0.0),2);
	}
	if (y < size.y-1) {
		NeighboursList_add(outList, new_OneNeighbourData(position + uint2(0, 1), 0.0),3);
	}
	if (x < size.x-1 && y < size.y-1) {
		NeighboursList_add(outList, new_OneNeighbourData(position + uint2(1, 1), 0.0),4);
	}
	if (x < size.x-1) {
		NeighboursList_add(outList, new_OneNeighbourData(position + uint2(1, 0), 0.0), 5);
	}
	if (x < size.x-1 && y > 0) {
		NeighboursList_add(outList, new_OneNeighbourData(position + uint2(1, -1), 0.0), 6);
	}
	if (y > 0) {
		NeighboursList_add(outList, new_OneNeighbourData(position + uint2(0, -1), 0.0), 7);
	}
#elif defined(VARIANT_NHOOD_CROSS4)
	if (x > 0) {
		NeighboursList_add(outList, new_OneNeighbourData( uint2(-1, 0), 0.0),0);
	}
	if (y < size.y-1) {
		NeighboursList_add(outList, new_OneNeighbourData(position + uint2(0, 1), 0.0),1);
	}
	if (x < size.x-1) {
		NeighboursList_add(outList, new_OneNeighbourData(position + uint2(1, 0), 0.0), 2);
	}
	if (y > 0) {
		NeighboursList_add(outList, new_OneNeighbourData(position + uint2(0, -1), 0.0), 3);
	}
#elif defined(VARIANT_NHOOD_X4)
	if (x > 0 && y > 0) {
		NeighboursList_add(outList, new_OneNeighbourData(position + int2(-1, -1), 0.0), 0);
	}
	if (x > 0 && y < size.y-1) {
		NeighboursList_add(outList, new_OneNeighbourData(position + uint2(-1, 1), 0.0),1);
	}
	if (x < size.x-1 && y < size.y-1) {
		NeighboursList_add(outList, new_OneNeighbourData(position + uint2(1, 1), 0.0),2);
	}
	if (x < size.x-1 && y > 0) {
		NeighboursList_add(outList, new_OneNeighbourData(position + uint2(1, -1), 0.0), 3);
	}
#endif


	return outList;
}

#endif