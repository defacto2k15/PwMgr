#ifndef TERRAIN_INC
#define TERRAIN_INC

float CalculateHeightWithMargins(float2 baseUv){
				float2 heightUv = baseUv;
				
				heightUv.x *= _HeightmapUv[2];
				heightUv.y *= _HeightmapUv[3];

				heightUv.x += _HeightmapUv[0];
				heightUv.y += _HeightmapUv[1];

				float mipMapIndex = _HeightmapLodOffset;
				if( min(baseUv.x, baseUv.y) < 0.0001 || max(baseUv.x, baseUv.y) > (1-0.0001) ){
					mipMapIndex = 0;
				}
				float heightmapValue = tex2Dlod(_HeightmapTex, float4( heightUv,0,mipMapIndex)).r;

				float2 distancesToMargins;
				float4 weldTextureUvs;
				// 0 - Left/Right 1 - Top/Bottom

				float4 leftRightRange;
				if (baseUv.x < 0.5) { //closer to left margin
					distancesToMargins[0] = baseUv.x;
					leftRightRange = _LeftWeldTextureUvRange;
				}
				else {
					distancesToMargins[0] = 1-baseUv.x;
					leftRightRange = _RightWeldTextureUvRange;
				}
				weldTextureUvs.xy = 
					float2(leftRightRange[0], leftRightRange[1] + baseUv.y* (leftRightRange[2] - leftRightRange[1]));
				float leftRightHeight = tex2Dlod(_WeldTexture, float4(weldTextureUvs.xy, 0, 0));

				float4 topBottomRange;
				if (baseUv.y < 0.5) { //closer to bottom margin
					distancesToMargins[1] = baseUv.y;
					topBottomRange = _BottomWeldTextureUvRange;
				}
				else {
					distancesToMargins[1] = 1-baseUv.y;
					topBottomRange = _TopWeldTextureUvRange;
				}
				weldTextureUvs.zw = 
					float2(topBottomRange[0], topBottomRange[1] + baseUv.x* (topBottomRange[2] - topBottomRange[1]));
				float topBottomHeight = tex2Dlod(_WeldTexture, float4(weldTextureUvs.zw, 0, 0));

				float startMergingMargin = 0.1;

				float2 marginWeights;
				if (leftRightRange[0] < 0) {
					marginWeights[0] = 0;
				}
				else {
					float heightDifference = abs(heightmapValue - leftRightHeight);
					marginWeights[0] =  1 - invLerpClamp(0.0, 
						lerp(
							0.05,
							startMergingMargin,
							saturate( heightDifference*40)
						),
						distancesToMargins[0]);
				}

				if (topBottomRange[0] < 0) {
					marginWeights[1] = 0;
				}
				else {
					float heightDifference = abs(heightmapValue - topBottomHeight);
					marginWeights[1] =  1 - invLerpClamp(0.0, 
						lerp(
							0.05,
							startMergingMargin,
							saturate( heightDifference*40)
						),
						distancesToMargins[1]);
				}
				if ( abs(topBottomHeight-heightmapValue) > 0.1 ) {
					topBottomHeight = heightmapValue;
				}
				if ( abs(leftRightHeight-heightmapValue) > 0.1 ) {
					leftRightHeight = heightmapValue;
				}

				float originalHeightWeight = 1 - max(marginWeights[0], marginWeights[1]);
				float weightsSum = marginWeights[0] + marginWeights[1] + originalHeightWeight;

				float finalHeight = heightmapValue* originalHeightWeight + leftRightHeight*marginWeights[0] + topBottomHeight*marginWeights[1];
				finalHeight /= weightsSum;
				return finalHeight;
}

float CalculateHeight(float2 baseUv){
		float2 heightUv = baseUv;

		heightUv.x *= _HeightmapUv[2];
		heightUv.y *= _HeightmapUv[3];

		heightUv.x += _HeightmapUv[0];
		heightUv.y += _HeightmapUv[1];

		float mipMapIndex = _HeightmapLodOffset;
		if( min(baseUv.x, baseUv.y) < 0.0001 || max(baseUv.x, baseUv.y) > (1-0.0001) ){
			mipMapIndex = 0;
		}
		float heightmapValue = tex2Dlod(_HeightmapTex, float4( heightUv,0,mipMapIndex)).r;
		return heightmapValue;
}
#endif