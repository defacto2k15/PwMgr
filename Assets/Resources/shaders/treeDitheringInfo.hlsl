#ifndef TREE_DITHERING_INFO_INC
#define TREE_DITHERING_INFO_INC

#define VERY_BIG_INT (9999999)

#define DITHERING_TREE_DISABLED ( float4(-2, -1, VERY_BIG_INT, VERY_BIG_INT+1))
#define DITHERING_TREE_FULL_DETAIL ( float4(-2, -1, VERY_BIG_INT, VERY_BIG_INT+1))
#define DITHERING_TREE_REDUCED_DETAIL ( float4(-2, -1, VERY_BIG_INT, VERY_BIG_INT+1))
#define DITHERING_TREE_BILLBOARD ( float4(-2, -1, VERY_BIG_INT, VERY_BIG_INT+1))

//#define DITHERING_TREE_FULL_DETAIL (float4(-2, -1, 30, 50))
//#define DITHERING_TREE_REDUCED_DETAIL (float4(30, 50, 100, 150))
//#define DITHERING_TREE_BILLBOARD (float4(100, 150, VERY_BIG_INT ,  VERY_BIG_INT+1))

// used shaders:
//	*tree_bark_incident
//	*tree_leaf_instanced
//	GenericBillboard.instanced.shader -> GenericBillboard.inc

float4 RetriveDitheringMode(float modeIndex){
	modeIndex += 0.5;
	if( modeIndex > 3){
		return DITHERING_TREE_BILLBOARD;
	} else if (modeIndex > 2 ){
		return DITHERING_TREE_REDUCED_DETAIL;
	} else if (modeIndex > 1 ){
		return DITHERING_TREE_FULL_DETAIL;
	} else {
		return DITHERING_TREE_DISABLED;
	}
}

#endif