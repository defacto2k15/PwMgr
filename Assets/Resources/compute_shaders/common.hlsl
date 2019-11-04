#ifndef COMMON_INC
#define COMMON_INC
	half fPI(){
		return 3.14;
	}

	half remapNeg(half x){ // from (0,1) to (-1,1)
		return (x-0.5)*2;
	}

	half remap(half x){ // from (-1,1) to (0,1)
		return (x+1)/2;
	}

	half4 assertNotZero(half4 inVal){
		return normalize(max(inVal, half4(0.01, 0.01, 0.01, 0)));
	}

	half invLerp(half min, half max, half value){
		return clamp((value - min) / (max-min),0,1);
	}

	half invLerpClamp(half min, half max, half value){
		return clamp(( clamp(value, min, max) - min) / (max-min),0,1);
	}

	half invLerpClamp2( half min, half max, half value){
		if( value < min){
			return 0.;
		}
		if( value > max){
			return 1.;
		}
		return invLerp(min, max, value);
	}

	half weightedAverage( half4 values, half4 weights ){
		return (values.x * weights.x +
				values.y * weights.y +
				values.z * weights.z +
				values.w * weights.w) / 
					( weights.x + weights.y + weights.z + weights.w);
	}

	// gets value val that is in <min, max>
	// returns normalized to <0, 1>
	half normalizeTo( half min, half max, half val){
		return clamp( (val - min)/(max-min), 0.0, 1.0);
	}

	float3 encodeNormal(float3 normal){
		return (normalize(normal) + 1)/2;
	}

	float3 decodeNormal(float3 encoded){
		return (encoded -0.5 )*2;
	}
#endif