#ifndef ERING2_TEST_SHADER_INC
#define ERING2_TEST_SHADER_

			struct TextureLayerInput{
				float3 colors[4];
				float strength;
				float complexity;
				float2 flatPosition;
				float3 surfaceNormal;
			};

			TextureLayerInput new_TextureLayerInput( float3 colors[4], float strength, float complexity, float2 flatPosition, float3 surfaceNormal ) {
				TextureLayerInput o;
				o.colors = colors;
				o.strength = strength;
				o.complexity = complexity;
				o.flatPosition = flatPosition;
				o.surfaceNormal = surfaceNormal;
				return o;
			}

			struct TextureLayerOutput{
				float3 color;
				float3 normal;
				float outHeightIntensity;
				float outAlpha;
			};

			TextureLayerOutput new_TextureLayerOutput( float3 color, float3 normal, float outHeightIntensity, float outAlpha){
				TextureLayerOutput o;
				o.color = color;
				o.normal = normal;
				o.outHeightIntensity = outHeightIntensity;
				o.outAlpha = outAlpha;
				return o;
			}

			TextureLayerOutput null_TextureLayerOutput(){
				return new_TextureLayerOutput(0,0,0,0);
			}

			struct TextureDetailOutput{
				float intensity;
				float seed;
			};

			TextureDetailOutput new_TextureDetailOutput( float intensity, float seed){
				TextureDetailOutput o;
				o.intensity = intensity;
				o.seed = seed;
				return o;
			}
   
    
			#include "noise.hlsl"
			GEN_fractalNoise( groundNormalNoise, 4, simpleValueNoise2D, 0, 1)

			// -1 to 1
			half snoise_MinusOne_To_One( in half2 p ){
				return (simplePerlinNoise2D(p)-0.5)*2.;
			}

			#define OCTAVES 6
			#define grass_snoise snoise_MinusOne_To_One
			// Ridged multifractal
			// See "Texturing & Modeling, A Procedural Approach", Chapter 12
			float ridge(float h, float offset) { // TODO zamien żeby nie kraść
				h = abs(h);     // create creases
				h = offset - h; // invert so creases are at top
				h = pow(h,4) ;      // sharpen creases
				return h;
			}

			TextureDetailOutput ridgedMF(float2 p, float strength, float complexity) { // TODO zamień żeby nie kraść
				float seed = 0;

				float lacunarity = 2.0;
				float gain = 1.2 * complexity; 
				float offset = 0.9;
					
				float sum = 0.0;
				float freq = 1.0, amp = 0.5 * strength;
				float prev = 0.0;

				float biggestAdd = 0;
				float biggestAddIdx;

				for(int i=0; i < OCTAVES; i++) {
					float temp = p.x;
					p.x = p.y;
					p.y = temp;

					float n = ridge(grass_snoise(p*freq), offset);

					float valueToAdd = n*amp + n*amp*prev;
					if( valueToAdd > biggestAdd){
						biggestAdd = valueToAdd;
						biggestAddIdx = i;
					}
					biggestAdd *= 0.95;

					sum += valueToAdd;
					prev = n;
					freq *= lacunarity;
					amp *= gain;
				}
				sum /= 8.;

				seed = (biggestAddIdx+ 1./(6.*2.)) /6.;
				float outSum = sum * lerp(4., 1., complexity);

				return 
					new_TextureDetailOutput(outSum, seed*lerp(0.8, 1.2, outSum) );
			}

			//////////////// NORMALS
			float notZeroSign( float input ){
				float sig = sign(input);
				if( sig == 0 ){
					sig = 1;
				}
				return sig;
			}

			float notZero( float input ){
				return max(abs(input), 0.000001) * notZeroSign(input);
			}

			// TODO napisz, że to na podstawie tego:: http://www.rorydriscoll.com/2012/01/11/derivative-maps/
			float3 CalculateSurfaceGradient( float3 n, float3 dpdx, float3 dpdy, float dhdx, float dhdy){
				float3 r1 = cross(dpdy, n);
				float3 r2 = cross(n, dpdx);

				return (r1 * dhdx + r2 * dhdy) / notZero(dot(n, cross(dpdx, dpdy)));
			}

			float3 my_normalize(float3 input ){
				if( length(input) == 0 ){
					return input;
				} else {
					return normalize(input);
				}
			}

			// Move the normal away from the surface normal in the opposite surface gradient direction
			float3 PerturbNormal(float3 normal, float3 dpdx, float3 dpdy, float dhdx, float dhdy, float complexity)
			{
				return my_normalize(normal- my_normalize(CalculateSurfaceGradient(normal, dpdx, dpdy, dhdx, dhdy))*complexity);
			}

			float3 CalculateSurfaceNormal( float3 position, float3 normal, float height, float complexity){
				float3 dpdx = ddx(position);
				float3 dpdy = ddy(position);

				float dhdx = ddx(height);
				float dhdy = ddy(height);

				return PerturbNormal( normal, dpdx, dpdy, dhdx, dhdy, complexity);
			}

			float3 ComputeSimpleNormal( float2 pos, float intensity, float normalStrength){
				float3 dx = ddx( float3(pos.x, intensity, pos.y));
				float3 dy = ddy( float3(pos.x, intensity, pos.y));

				float3 lenX = length( dx.xz);
				float3 lenY = length( dy.xz);

				float sdx = dx.y / lenX;
				float sdy = dy.y / lenY;

				sdx *= (0.06 * normalStrength);
				sdy *= (0.06 * normalStrength);

				return normalize( float3(sdx, sdy, 1.0));
			}

			////////////////////


			float3 ComputeColor( float seed, float intensity, float3 colors[4] ){
				float3 controlPoints;
				controlPoints[0] = 0.6;
				controlPoints[1] = 0.69;
				controlPoints[2] = 0.78;
				controlPoints = controlPoints * (0.28 + 0.912*seed);

				for( int i = 0; i < 3; i++){
					if( seed < controlPoints[i]){
						return colors[i] /** intensity*/;
					}
				}
				return colors[3] /** intensity*/;
			}

			float3 ComputeNormal(float2 pos, float intensity, float3 surfaceNormal, float complexity ){
				//return CalculateSurfaceNormal( float3( pos.x, 0, pos.y), surfaceNormal, intensity, complexity);
				return normalize(float3(ddx(intensity) * 1, ddy(intensity) * 1, 1.0));
			}

			TextureLayerOutput getGrassyField( TextureLayerInput input){
				float2 pos = input.flatPosition/8;
				pos = pos * 6 + float2(231.31, 523.11);

				float colorComplexity = lerp(0.4, 1, input.complexity);
				TextureDetailOutput output = ridgedMF(pos, 1, colorComplexity);

				float3 color = ComputeColor(output.seed, output.intensity, input.colors);
				float3 normal = ComputeSimpleNormal(pos, output.intensity, 1);

				float skewedIntensity = invLerpClamp( 0.08, 0.54, output.intensity);
				skewedIntensity = saturate(skewedIntensity - (1-input.strength));

				return new_TextureLayerOutput(color, normal, skewedIntensity, 1);
			}

			////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			///////////////////////DRY SAND!/////////////////////////////////////////////////////////////////////////////////////////
			////////////////////////////////////////////////////////////////////////////////////////////////////////////////
			float euclideanDistance(  float2 addedElement, float2 minusElement){
				float2 diff = addedElement - minusElement;
				return length(diff);
			}

			#define snoise snoise_MinusOne_To_One
			#define normal_snoise simplePerlinNoise2D
			#define random rand
			#define random2 randNorm
			#define distanceFunction euclideanDistance
			// pos has values <-1, ,1>
			TextureDetailOutput getSimpleVoronoiShape( float2 pos, float strength, float complexity){

				float2 st = (pos+float2(1., 1.))/2.;
				
				float coordsMultiplication = 65.;
				float baseDisperseParam = 0.;
				float finalCoordsMultiplication =  coordsMultiplication +(normal_snoise(st) * coordsMultiplication* baseDisperseParam);

				st *= finalCoordsMultiplication;
				
				// Tile the space
				float2 i_st = floor(st);
				float2 f_st = frac(st);

				float m_dist = 10.;  // minimun distance
				float2 m_point;        // minimum point

				float minDistance = 10.;
				float2 mb; // start of closestCell as delta from (current point's cell start point)
				float2 mr; //vector from closestCell to currentPoint
				float2 currentCellStartPoint;

				for (int j=-1; j<=1; j++ ) {
					for (int i=-1; i<=1; i++ ) {
						float2 neighbour = float2( float(i), float(j));
						float2 l_point = random2( i_st + neighbour);
						float2 addedElement = neighbour + l_point;
						float2 minusElement = f_st;


						float distance = distanceFunction( addedElement, minusElement);

						if( distance < minDistance){
							minDistance = distance;
							mr = addedElement - minusElement;
							mb = neighbour;
							currentCellStartPoint = l_point;
						}
					}
				}
				

				float distanceFromBorder = 10.;
				float2 rx;
				for (int j=-2; j<=2; j++ ) { //can be set to 1 and -1, no big change
					for (int i=-2; i<=2; i++ ) {
						float2 neighbour = mb + float2( float(i) , float(j));
						// neighbour of closestCell

						float2 l_point = random2( i_st + neighbour);
						// seed point of closestCell's neighbour

						float2 r = neighbour +l_point- f_st;
						// distance from seed point of closestCell's neighbour to currentPoint

						// mr is vector from closestCell to currentPoint

						float d = dot( 0.5*(mr+r), normalize(r-mr));
						if( d < distanceFromBorder ){
							distanceFromBorder = d;
							rx = l_point; 
						}
					}
				}

				float randomSlopeStartFactor = rand2(currentCellStartPoint);
				float slopeStart = lerp(0.02, 0.08, randomSlopeStartFactor);

				float randomSlopeSizeFactor = fmod( randomSlopeStartFactor+sin(currentCellStartPoint.x), 1.0);
				float slopeSize = lerp(0.1, 
					lerp(0.25, 1. - slopeStart, 0./*-complexity*/) , // the less complex, the bigger is slope
					 randomSlopeSizeFactor);
				float outValue = smoothstep(slopeStart, slopeStart + slopeSize, distanceFromBorder);

				outValue = lerp( 1. -complexity, 1.,  outValue); // the less complex, the less deep the ridges are

				float dissapearanceFactor =  fmod( randomSlopeStartFactor+cos(currentCellStartPoint.x)*1.521, 1.0);
				outValue *= step( 1. -strength, dissapearanceFactor);

				outValue *= lerp( 0.85, 1.,complexity); // less complex  are a bit lower

				return new_TextureDetailOutput(outValue, currentCellStartPoint.y*4.12 + currentCellStartPoint.x*0.75 );
			}

			float3 getDrySandColor( float seed, float intensity, float complexity, float strength, float3 colors[4]){
				int seedMod = floor( frac(seed)*6);

				float3 finalColor = float3(0,0,0);
				float4 weights = 0;


				weights[0] = (1-complexity)*( (1/max(0.0001,strength)) *6 -6);
				weights[1] = (1-complexity)*3;
				weights[2] = (1-complexity)*2;
				weights[3] = 1-complexity;

				if( intensity  < 0.8){
					weights[0] = 6 * complexity;
				} else if( seedMod < 3 ){
					weights[1] += 3 * (complexity);
				}else if( seedMod < 5 ){
					weights[2] += 2 * (complexity);
				} else {
					weights[3] += (complexity);
				}
				return ( 
					colors[0] * weights[0] +
					colors[1] * weights[1] + 
					colors[2] * weights[2] + 
					colors[3] * weights[3]) /
					 ( weights[0] + weights[1] + weights[2] + weights[3]);
			}

			TextureLayerOutput getDrySand(TextureLayerInput input){
				float2 position = input.flatPosition / 10;
				TextureDetailOutput output = getSimpleVoronoiShape(position, input.strength, input.complexity);
				float3 color = getDrySandColor( output.seed, output.intensity, input.complexity, input.strength, input.colors);
				float3 normal = ComputeSimpleNormal(position,  output.intensity, 1);

				float outIntensity = output.intensity * lerp(0.5, 1, input.strength);
				return new_TextureLayerOutput( color, normal, outIntensity, 1);
			}
/*
half2 randNormXX(half2 seed)
{
	return half2(rand(seed.x*23.13 + seed.y*57.131), rand(seed.y*323.83 + seed.x*54.231));
}
half rand2XX(half2 p)
{
	return frac(sin(half2(dot(p,half2(127.1,311.7)),dot(p,half2(269.5,183.3))))*43758.5453);
}
*/

			///////////////////////////
			///////////DOTTED TERRAIN
			///////////////////////////
			#define snoise snoise_MinusOne_To_One
			#define normal_snoise simplePerlinNoise2D
			#define random rand2
			#define random2 randNorm
			#define distanceFunction euclideanDistance
			// pos has values <-1, ,1>
			TextureDetailOutput getSingleDottedTerrainShape( float2 pos, float strength){
				float2 st = pos * 65;
				
				// Tile the space
				float2 i_st = floor(st);
				float2 f_st = frac(st);

				float m_dist = 10.;  // minimun distance
				float m2_dist = 10.;
				float2 m_point;        // minimum point
				float2 m2_point;
				
				for (int j=-1; j<=1; j++ ) {
					for (int i=-1; i<=1; i++ ) {
						float2 neighbor = float2(float(i),float(j));
						float2 l_point = random2(i_st + neighbor);
						float2 diff = neighbor + l_point - f_st;
						float dist = length(diff);

						if( dist < m_dist ) {
							m2_dist = m_dist;
							m_dist = dist;
							m2_point = m_point;
							m_point = neighbor+l_point+i_st;
						} else if (dist < m2_dist){
							m2_point = neighbor+l_point+i_st;
							m2_dist = dist;
						}
					}
				}
				
				float sizeFactor = 1.;
				float sizeOfDotsFactor = 0.27 * (1. - (2*sizeFactor - 1.)); //0.27 jest dobre, to ustawia jak duże są  dotsy
				float lengthBetweenSeeds = length( st - m_point) + length(st - m2_point);
				float marginDiscoveryValue = abs(m_dist-m2_dist);

				float xyFac =  (lengthBetweenSeeds * sizeOfDotsFactor); // ustawia od kiedy kamien ma sie pojawiać
				xyFac /= marginDiscoveryValue; // dzięki temu wielkość dottów jest różna

				float valueToReturn = invLerp( clamp(0,1, xyFac), 1, marginDiscoveryValue);

				float baseRandomPerCell = random(m_point*33.); // todo delete
				float amountOfDots =strength;

				valueToReturn *= step( 1. - amountOfDots,  baseRandomPerCell); // we remove some dots randomly
				valueToReturn *= step( 0, sizeFactor); // delete those which are too small

				return new_TextureDetailOutput( valueToReturn, abs(m_point.x + m_point.y*13.412));
			}

			float3 getDotColor( float seed, float intensity, float3 colors[4] ){
				int index = (int)floor( (frac(seed) * 3.99) );
				float3 stoneColor = colors[index+1];
				float3 groundColor = colors[0];
					
				return lerp(groundColor, stoneColor, intensity);
			}

			TextureLayerOutput getDottedTerrain(TextureLayerInput input){
				float complexity = abs(input.complexity);

				TextureDetailOutput output = getSingleDottedTerrainShape(input.flatPosition, input.strength);

				float transparencySlopeStart = lerp( 0.1, 0.4,complexity);
				float transparencySlopeWidth = lerp( 1., 0.2,complexity);

				float finalIntensity = lerp( 0., lerp( 1., 0.5,complexity),
					 invLerp( transparencySlopeStart, transparencySlopeStart + transparencySlopeWidth, output.intensity));
				finalIntensity *= lerp( 1., 2., input.complexity);

				float3 color = getDotColor( output.seed, finalIntensity, input.colors);
				float3 normal = ComputeSimpleNormal(input.flatPosition, finalIntensity, 1);
 
				float outIntensity = finalIntensity * lerp(0.5, 1, input.strength);
				return new_TextureLayerOutput(color, normal, outIntensity, 1);
			}
			
			/////////
			//FLAT TERRAIN
			////////

			float3 CalculateBaseFlatTerrainColor(float intensity, float3 palette[4]){
				float3 controlValues = float3( 0.4, 0.75, 0.9);
				for( int i = 0; i < 3; i++){
					if(intensity < controlValues[i]){
						return palette[i];
					}
				}
				return palette[3];
			}

			TextureLayerOutput getBaseFlatTerrain(TextureLayerInput input){
				float2 pos = input.flatPosition * (1 +20);
				float intensity = fractal_improvedValueNoise2D_3(pos);
				intensity /= 1.2;
				intensity = (intensity+1)/2;

				float3 color = CalculateBaseFlatTerrainColor(intensity, input.colors);

				float3 normal = ComputeSimpleNormal(pos, intensity*4, 1);
				return new_TextureLayerOutput( color, normal, saturate(intensity)+0.01, input.strength);
			}

			//////
			// ROCKY FLAT TERRAIN
			/////

			GEN_fractalNoise( secondaryNoise, 3, snoise2D, 0.1, 0.5)
			TextureLayerOutput getRockyFlatTerrain(TextureLayerInput input){
				float2 pos = input.flatPosition * (1 +2);
				float intensity = fractal_improvedValueNoise2D_3(pos);

				intensity += secondaryNoise(pos*10)/50;
				intensity *= input.strength;

				float3 color = CalculateBaseFlatTerrainColor(intensity, input.colors);
				intensity -= 0.3;

				float3 normal = ComputeSimpleNormal(pos, intensity, 1);
				return new_TextureLayerOutput( color, normal, intensity, 1);
			}



GEN_fractalNoise( colorNoise, 3, simplePerlinNoise2D, 0.3, 0.55)
///// Color retriving and noising!
			float3 JitterColor( float3 baseColor, int colorIndex, float2 flatPos ){
				return baseColor *= lerp(0.8, 1.2, remap(colorNoise(flatPos/5 + colorIndex*54.321)));
			}

			float JitterControlValue( float oldControlValue, float seed, float2 flatPos ){
			return oldControlValue;
				float2 pos = flatPos/10 + float2(432.22 + (seed*2), 864.1+seed*5.212);
				float jitterValue = remap(rand2(pos));

				float2 jitterRange = float2(0.7, 1.3);
				return lerp(jitterRange[0],jitterRange[1], jitterValue) * oldControlValue;
			}


			/////// END LAYER OUTPUTS CALCULATION
//			#undef OT_BASE
//			#undef OT_DRY_SAND
//			#undef OT_GRASS
//			#undef OT_DOTS

			//Our Fragment Shader
			TextureLayerOutput ering2_surf(in float3 globalPos, float2 uv, int layerIndex) {
				float2 flatPos = globalPos.xz;

#ifdef INLINE_PALETTE_COLORS
				float3 colors[16];
				colors[0] = fixed3(1,0,0);
				colors[1] = fixed3(1,0.25,0);
				colors[2] = fixed3(1,0.5,0);
				colors[3] = fixed3(1,0.75,0);

				colors[4] = fixed3(0,1,0);
				colors[5] = fixed3(0,1,0.25);
				colors[6] = fixed3(0,1,0.5);
				colors[7] = fixed3(0,1,0.75);

				colors[8] =  fixed3(0,0,1);
				colors[9] =	 fixed3(0,0.25,1);
				colors[10] = fixed3(0,0.5,1);
				colors[11] = fixed3(0,0.75,1);

				colors[12] = fixed3(1,0,1);
				colors[13] = fixed3(1,0.25,1);
				colors[14] = fixed3(1,0.5,1);
				colors[15] = fixed3(1,0.75,1);
#else
#define colors _Palette
#endif 

				float2 globalFlatPosition = flatPos;// + colorNoise(flatPos/10);

				float2 controlUv = float2( (globalFlatPosition.x -_Dimensions[0]) / _Dimensions[2], (globalFlatPosition.y -_Dimensions[1]) / _Dimensions[3]); 
				float4 controlValue = tex2D( _ControlTex, controlUv);

				float randomSeed = _RandomSeeds[layerIndex];
				flatPos += + float2(fmod(randomSeed, 2231.312), fmod(randomSeed, 5212.42123));
				flatPos /= 20.0;
				float strength = 0;

#define MAX_LAYER_COUNT (4)
				float3 palette[MAX_LAYER_COUNT]; 

				TextureLayerOutput layerOutput = null_TextureLayerOutput();

				float2 turncatedInputPos = fmod(flatPos, 1000);
				//turncatedInputPos = fmod(turncatedInputPos + round(flatPos / 1000.0)*5.1232, 1000);
				

				#ifdef OT_BASE
					palette[0] = JitterColor(colors[ layerIndex*4 + 0],  0 + 0*10, flatPos);
					palette[1] = JitterColor(colors[ layerIndex*4 + 1],  1 + 0*10, flatPos);
					palette[2] = JitterColor(colors[ layerIndex*4 + 2], 2 + 0*10, flatPos);
					palette[3] = JitterColor(colors[ layerIndex*4 + 3], 3 + 0*10, flatPos);
					strength = JitterControlValue(controlValue[layerIndex], 0, flatPos);	
					layerOutput = getBaseFlatTerrain(new_TextureLayerInput(palette, strength, _DetailComplexity, turncatedInputPos, float3(0,0,1)));
				#endif

				#ifdef OT_DRY_SAND
					palette[0] = JitterColor(colors[ layerIndex*4 + 0],  0 + 1*10, flatPos);
					palette[1] = JitterColor(colors[ layerIndex*4 + 1],  1 + 1*10, flatPos);
					palette[2] = JitterColor(colors[ layerIndex*4 + 2], 2 + 1*10, flatPos);
					palette[3] = JitterColor(colors[ layerIndex*4 + 3], 3 + 1*10, flatPos);

					strength = JitterControlValue(controlValue[layerIndex],1, flatPos);	
					layerOutput = getDrySand( new_TextureLayerInput( palette, strength, _DetailComplexity, turncatedInputPos , float3(0,0,1)));
				#endif
				#ifdef OT_GRASS
					palette[0] = JitterColor(colors[ layerIndex*4 + 0],  0 + 2*10, flatPos);
					palette[1] = JitterColor(colors[ layerIndex*4 + 1],  1 + 2*10, flatPos);
					palette[2] = JitterColor(colors[ layerIndex*4 + 2], 2 + 2*10, flatPos);
					palette[3] = JitterColor(colors[ layerIndex*4 + 3], 3 + 2*10, flatPos);

					strength = JitterControlValue(controlValue[layerIndex], 2, flatPos);	
					layerOutput = getGrassyField( new_TextureLayerInput( palette, strength, _DetailComplexity,  turncatedInputPos, float3(0,0,1)));
				#endif
				#ifdef OT_DOTS
					palette[0] = JitterColor(colors[ layerIndex*4 + 0],  0 + 3*10, flatPos);
					palette[1] = JitterColor(colors[ layerIndex*4 + 1],  1 + 3*10, flatPos);
					palette[2] = JitterColor(colors[ layerIndex*4 + 2], 2 + 3*10, flatPos);
					palette[3] = JitterColor(colors[ layerIndex*4 + 3], 3 + 3*10, flatPos);

					strength = JitterControlValue(controlValue[layerIndex], 3, flatPos);	
					layerOutput = getDottedTerrain( new_TextureLayerInput( palette, strength, _DetailComplexity, turncatedInputPos, float3(0,0,1)));
				#endif

				float layerPriority = _LayerPriorities[layerIndex];
				layerOutput.outAlpha *=  layerOutput.outHeightIntensity*layerPriority * controlValue;
				return layerOutput;
			} 

#endif
