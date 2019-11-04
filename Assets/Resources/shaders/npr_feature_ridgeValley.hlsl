#ifndef H_NPR_FEATURE_RIDGEVALLEY_INC
#define H_NPR_FEATURE_RIDGEVALLEY_INC

#include "npr_feature_common.txt"

			float ridgeFeature_geometry_traitGenerator(geometry_edge_situation s, geometry_camera_situation camera_situation) {
				return signedAngle(s.t1Norm, s.t2Norm, normalize(s.v1Pos - s.v2Pos));
			}

			float valleyFeature_geometry_traitGenerator(geometry_edge_situation s, geometry_camera_situation camera_situation) {
				return signedAngle(s.t1Norm, s.t2Norm, normalize(s.v1Pos - s.v2Pos));
			}
#endif