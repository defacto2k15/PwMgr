#include "noise.hlsl"
#include "common.txt"

#ifndef GRASS_GENERATION_INC
#define GRASS_GENERATION_INC

half generateDirectionCoef( half4 windDirection, half4 plantDirection){
	half cosVal = dot(normalize(windDirection.xy),normalize(plantDirection.xy));
	return(cosVal);
}

// inWindValue - value from 0 to 1
// initialBendingValue - <-1,1>
// returns value from 0 to 1
float calculateXBendStrength( half inWindValue, half4 windDirection, half4 plantDirection, half initialBendingValue, half plantBendingStiffness ){
	half directionCoefficient = generateDirectionCoef( windDirection, plantDirection);

	float directedWindValue = directionCoefficient * inWindValue;
	float directedWindStrength = abs(directedWindValue);

	half valueAfterStiffnessComputation = 
			lerp(initialBendingValue, 
				lerp(directedWindValue, directedWindValue*0.4+initialBendingValue*0.6, plantBendingStiffness), 
			 directedWindStrength);
	// has values -1, 1

	half valueAfterStiffnessComputationStrength = max(0.1, abs(valueAfterStiffnessComputation));

	const half minStrength = 0.05;
	const half maxStrength = 0.95;
	return lerp(minStrength, maxStrength,remap(sign(valueAfterStiffnessComputation)*valueAfterStiffnessComputationStrength)  );
}

// inWindValue - value from 0 to 1
// returns value from 0 to 1
float calculateYBendStrength( half inWindValue, half4 windDirection, half4 plantDirection ){
	half directionCoefficient = sqrt(generateDirectionCoef( windDirection, plantDirection.zyxw)); //rotated as we check if wind is blowing on side
	half normalized = remap(inWindValue * directionCoefficient);
	// now normalized has value from -1 to 1, which also sets the direction of bend

	const half minStrength = 0.05;
	const half maxStrength = 0.9;
	return 0.5;// lerp(minStrength, maxStrength,normalized  );
}

float bendingAngle(float bendStrengthValue ){
	return radians(90 - lerp(-90,90,bendStrengthValue));
}

// returns values from -1 to 1
half generateRandomWindMoveMultiplier( half bendingStrength, half randSeed ){
	half currentNoiseValue = noise1D(_Time[3]);
	half randomSinusDegreeOffsetAccordingToPosition = randSeed*10;
	half randomNoiseMultiplier = lerp(0.8, 1.2, currentNoiseValue);
	half bendStrengthMultiplier = lerp(0.4, 0.6, sin(pow(bendingStrength,2) * fPI()));  
	half frequencyMultiplier = lerp(0.5, 1.5, pow(bendingStrength,2));

	return  sin(_Time[3] * frequencyMultiplier * 2 + randomSinusDegreeOffsetAccordingToPosition*30) * randomNoiseMultiplier*bendStrengthMultiplier ;
}

// first element is xStrength, second is yStrength. Each strength has values from -1 to 1
half2 generateStrengths( half l_BendingStrength, half l_InitialBendingValue, half l_PlantBendingStiffness, half4 l_WindDirection, half4 l_PlantDirection, half l_RandSeed){
		l_WindDirection = assertNotZero(l_WindDirection);
		l_PlantDirection = assertNotZero(l_PlantDirection);

		half randomWindMultiplier =  generateRandomWindMoveMultiplier(l_BendingStrength, l_RandSeed);
		half xBendStrength = remapNeg(calculateXBendStrength(l_BendingStrength, l_WindDirection, l_PlantDirection, l_InitialBendingValue, l_PlantBendingStiffness));
		xBendStrength *= ( 1 + 0.2* randomWindMultiplier ); 

		half yBendStrength = remapNeg(calculateYBendStrength(l_BendingStrength, l_WindDirection, l_PlantDirection));
		yBendStrength *= ( 1 + 0.2* randomWindMultiplier ); 
		return half2( xBendStrength, yBendStrength);
} 

half calculateZ( half xBendStrength, half l ){
	return xBendStrength*(  (sin(l*fPI()/2-0.5*fPI())+1)/2)  * (1 + (sin(fPI()/2 * abs(xBendStrength))))*0.9;
}

half calculateY (half xBendStrength, half l){
	return l *  cos(lerp( 0,fPI()/2,  abs(xBendStrength)))*0.9;
}

half calculateX(half yBendStrength, half vertexX, half vertexY ){
	half remapedXVertex = remapNeg(vertexX);
	// now xVertex has values from -1 + 1
	return remap( remapedXVertex  + vertexY*(1+(pow(vertexY ,2))) * (yBendStrength*2));
}

#endif
