#include "grassGeneration.hlsl"  

#ifndef SINGLE_GRASS_GENERATION_INC
#define SINGLE_GRASS_GENERATION_INC


		half3 calculateNormal(half yPos, half zPos ){
			zPos = max(zPos, 0.001); // not to divide by 0
			half angle = atan( yPos / zPos );
			return normalize( half3(1.0, cos(angle), -sin(angle)));
		}

		struct Input {
			float2 objectSpacePos;
			float3 normal; 
			float3 viewDir;
		};

		void grass_vert(inout appdata_full v, out Input o, 
				half l_BendingStrength, half l_InitialBendingValue, half l_PlantBendingStiffness, 
				half4 l_WindDirection, half4 l_PlantDirection, fixed4 l_Color, half l_RandSeed, half distanceScale  ){
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.objectSpacePos = v.vertex.xy;

			float l = v.texcoord[1]; // height of vertex from 0 to 1

			half2 strengths = generateStrengths( l_BendingStrength, l_InitialBendingValue, l_PlantBendingStiffness, 
				l_WindDirection, l_PlantDirection, l_RandSeed);
			half xBendStrength = strengths.x;
			half yBendStrength = strengths.y;

			
			v.vertex.z = calculateZ(xBendStrength, l) * distanceScale; //forth-back bending
			v.vertex.y = calculateY(xBendStrength, l) * distanceScale; // top-down bending
			v.vertex.x = calculateX(yBendStrength, v.vertex.x, v.vertex.y) * distanceScale;

			// calculating normals
			o.normal = v.normal = calculateNormal(v.vertex.y, v.vertex.z);
		}

		void grass_surf (Input IN, inout SurfaceOutputStandard o, fixed4 l_Color) {
			// IN.objectSpacePos.x  is from -0.5 to 0.5. We change it to from 0 to 1, and then multiply
			half fixedXPos = (IN.objectSpacePos.x + 0.5) ;
			// now it has values from 0 to 1
			fixed4 c = l_Color * 0.9 + 0.2* (1-pow(abs((fixedXPos*2-1)),0.3));
			//c.x = dot(IN.viewDir, float3(0, 0, 1));

			o.Albedo = c;
			o.Emission = half3(0,0,0);
			o.Metallic = 0.8; 
			o.Smoothness = 0.8;

			o.Normal = IN.normal;
		}

#endif