#ifndef BILLBOARD_GRASS_GENERATOR_INC
#define BILLBOARD_GRASS_GENERATOR_INC

#include "grassGeneration.hlsl"
#include "color.hlsl"

// pos.x should be <-1, 1>
// pos.y should be <0,1>
// thinningCoef - at 0 tip very thin, at 1 rather fat
// offset has values -1 to 1
// width at base has values from 0 to 1 that are good - 0 doesnt mean no width
// returns x - 0 if dont paint and 1 otherwise
//			y - distance of pixel to grass blade center in %. at 100% is is edge
half2 draw(half xScale, half yScale, half widthAtBase, half offset, half2 pos, half bendingStrength, half thinningCoef ){
	half2 xFrame = half2(-xScale + offset, xScale + offset);
	half2 yFrame = half2(-yScale, yScale);
			
	half xPos = calculateZ(pow(bendingStrength,3)*xScale, pos.y/(yScale)) +offset*0.027; // we are looking at blade "from side, so z is out x"

	half movementCorrectingCoef = pow(0.6/2, 3);

	half tempWidth = lerp(0.01, 0.1, widthAtBase) *pow(movementCorrectingCoef,0.9) * xScale;

	// idea is that if BendingStrength is high, the tip of blade is low, to we have to take it into account
	half absStrength = abs(bendingStrength);
	half widthInputPosition = (1-step(0.38, absStrength)) * pos.y +  // first, if BendingStrength is < 0.38, tip of blade is still at top
			step(0.38, absStrength) * pos.y / ( 0.15 * pow(absStrength,-2));
	// also when there is yScalling, we have to take it into account (that tip is lower)
	widthInputPosition /= yScale;

	half widthOfBlade = (1.0-pow(widthInputPosition,-1+pow(lerp(1.5, 4, thinningCoef),2))) * tempWidth;

	half currentDistance = distance(half2(pos.x*movementCorrectingCoef, pos.y), half2( xPos, pos.y));
	half pixelShouldBePainted = 0;
	if( currentDistance < widthOfBlade ){
		// take frame into consideration
		pixelShouldBePainted = 1;
		pixelShouldBePainted *= step(xFrame.x, pos.x)*(1-step(xFrame.y, pos.x));
		pixelShouldBePainted *= step(yFrame.x, pos.y)*(1-step(yFrame.y, pos.y));
	}
	pixelShouldBePainted = step(0.01, pixelShouldBePainted);
	return half2(pixelShouldBePainted, currentDistance/widthOfBlade);
}

half generateSize( half seed ){
	return lerp(0.2, 1, rand(seed)); 
}

half generateBaseWidth(half size, half seed ){
	return clamp( size * lerp( 0.75, 1.25, rand(seed))*0.5, 0, 1);
}

half2 generateScales(half size, half seed ){
	// we dont want for scales to differ very mush
	return half2( 
		clamp( lerp(0.75, 1.25, rand(seed)) * size, 0, 1),
		clamp( lerp(0.75, 1.25, rand(seed*12.321)) * size, 0, 1));
}

// returns values in <-1, 1> * xScale
// big grass cant have big offset
half generateOffset( half size, half seed, half xScale ){
	// the bigger the grass, the closer it is to center
	half randomX = rand(seed);//lerp(margin, 1-margin, rand(seed));
	half absoluteOffset = pow(randomX, lerp(0.7, 1.5,  size)) * (1-xScale)*1.2;
	return absoluteOffset * sign( rand(seed + 20.12) - 0.5); // we have to add sign;
}

half  generateBendingStrength( half size, half offset, half seed){
	// 1. the closer to center, the smaller the bending.
	// 2. the bigger the grass, the bigger the bend
	// 3. no bends are not likely at all

	half random = rand(seed);
	half bending1 = 1-pow(random, lerp(1, 3.5,  abs(offset))); //1
	half bending2 = lerp(0.75, 1.25, pow(rand(random+22), lerp(1, 2.5,  1-size))); //2 
	//3;
	return weightedAverage(half4(bending1, bending2, 0, 0), half4(1,0.5,0,0)) * sign(rand(seed + 44)-0.5); 
}

half generateThinningCoef(half seed ){
	return rand(seed);
}

half3 generateColor(half seed, half distance){
	half baseHue = rand(seed+13.12);
	half baseSaturation = rand(seed+23.12);
	half innerBaseColor = lerp(0, 0.8, rand(seed+43.12));

	half3 innerColor = generateGrassColor( baseHue, baseSaturation,innerBaseColor);
	half3 outerColor  = generateGrassColor( baseHue, baseSaturation, innerBaseColor+0.2);
	return lerp(innerColor, outerColor, pow(distance,2));
}

// pos.x is from -1 to 1
// pos.y is from 0 to 1
half4 billboard_grass_generator_surf( half2 pos, half seed, int bladesCount ){
	half4 color = half4(0.0, 0.0, 0.0, 0.0);
	for( int i = 0; i < bladesCount; i++ ){
			seed = frac(seed + rand(i));
		half size = generateSize(seed);
		half2 scales = generateScales(size, seed * 123.21);
		half baseWidth = generateBaseWidth(size, seed *234.21);
		half offset = generateOffset(size, seed *345.21, scales.x);
		half bendingStrength = generateBendingStrength(size, offset, seed*456.78);
		half thinningCoef = generateThinningCoef(seed);

		half2 res = draw(scales.x, scales.y, baseWidth, offset, pos, bendingStrength, thinningCoef);
		if(res.x > 0 ){ //pixel should be painted
			half3 rgbColor = generateColor(i, res.y);
			color  = half4(rgbColor.xyz, 1);
		}
	}
	return color;
}

// pos.x is from -1 to 1
// pos.y is from 0 to 1
float4 billboard_grass_generator_surf_point_characteristics( half2 pos, half seed, int bladesCount ){
	float4 pointCharacteristics = float4(0.0, 0.0, 0.0, 0.0);
	for( int i = 0; i < bladesCount; i++ ){
			seed = frac(seed + rand(i));
		half size = generateSize(seed);
		half2 scales = generateScales(size, seed * 123.21);
		half baseWidth = generateBaseWidth(size, seed *234.21);
		half offset = generateOffset(size, seed *345.21, scales.x);
		half bendingStrength = generateBendingStrength(size, offset, seed*456.78);
		half thinningCoef = generateThinningCoef(seed);

		half2 res = draw(scales.x, scales.y, baseWidth, offset, pos, bendingStrength, thinningCoef);
		if(res.x > 0 ){ //pixel should be painted
			pointCharacteristics.b = res.y;	
			pointCharacteristics.r = i/255.0;
			pointCharacteristics.a = 1;
		}
	}
	return pointCharacteristics;
}


#endif