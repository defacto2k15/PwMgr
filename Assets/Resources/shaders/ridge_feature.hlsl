﻿
// Required:
//		_EdgeAngleBuffer
//		_RidgeTreshold
//		_SurfaceLineWidth



struct  Ridge_VertexOutBuffer{
#ifdef IN_RIDGE_FEATURE_DETECTION_MODE_VERTEX
	float ridgeStatus[3]; //todo
#endif
};

struct Ridge_GeometryOutBuffer {

};

void Ridge_VertexFilter(VertexSituation situation, Ridge_VertexOutBuffer buffer) {

#ifdef IN_RIDGE_FEATURE_DETECTION_MODE_VERTEX
	float edgeAngle = _EdgeAngleBuffer[situation.vid];
	buffer.ridgeStatus[situation.vertexInTriangleIndex] = edgeAngle < _RidgeTreshold ? 1 : 0;
#endif
}

#ifdef IN_RIDGE_FEATURE_DETECTION_MODE_VERTEX
	#define Ridge_FragmentInBuffer Ridge_VertexOutBuffer
#endif

FeatureFragmentOut Ridge_FragmentFilter(FragmentSituation situation, Ridge_FragmentInBuffer ridgeIn) {
	FeatureFragmentOut o;
	o.renderTargetIndex = 0;
	o.color = 0;
	o.applyColor = false;

	if (ridgeIn.ridgeStatus[situation.edgeIndex] < _RidgeTreshold) {
		o.applyColor = true;
		o.color = fixed4(1,0,0,1);
	}
	else {
		o.applyColor = true;
		o.color = fixed4(1,1,0,1);
	}
	asd
	return o;
}