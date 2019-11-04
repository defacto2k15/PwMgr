
#ifndef TEXT_PRINTING_INC
#define  TEXT_PRINTING_INC

#include "common.txt"

sampler2D _G_FontMap;

			fixed4 printCharacter(int2 characterPos, float2 uv) {
				uv = inv_remapUVToBox(uv, float2(-0.2, -0.2), float2(1.4, 1.4));
				float2 fontBoxSize = float2(0.07684, 0.12066);
				return tex2D(_G_FontMap, float2(0, 0) +  memberwiseMultiply(uv, fontBoxSize) + float2(fontBoxSize[0] * characterPos[0], fontBoxSize[1] * characterPos[1]));
			}
			
			fixed4 printDigit(int digit, float2 uv) {
				uv = inv_remapUVToBox(uv, float2(-0.2, -0.2), float2(1.4, 1.4));
				float2 fontBoxSize = float2(0.07684, 0.12066);
				return tex2D(_G_FontMap, float2(0, 0.295) +  memberwiseMultiply(uv, fontBoxSize) + float2(fontBoxSize[0] * digit,0));
			}

			fixed4 printDigitInCell(int digit, float2 uv, int2 cellIndex, int2 allCells, int textRow) {
				float width = 1.0 / allCells.x;
				float height = 1.0/allCells.y;

				float offsetY = height * cellIndex.y;
				float offsetX = width * cellIndex.x;

				float2 newUv = inv_remapUVToBox(uv, float2(offsetX, offsetY), float2(width, height));

				if (isInUvRange(newUv)) {
					return printDigit(digit, newUv);
				}
				else {
					return 0;
				}
			}

			fixed4 printCharacterInCell(int character, float2 uv, int2 cellIndex, int2 allCells, int textRow) {
				float width = 1.0 / allCells.x;
				float height = 1.0/allCells.y;

				float offsetY = height * cellIndex.y;
				float offsetX = width * cellIndex.x;

				float2 newUv = inv_remapUVToBox(uv, float2(offsetX, offsetY), float2(width, height));

				if (isInUvRange(newUv)) {
					return printCharacter(int2(character,1), newUv);
				}
				else {
					return 0;
				}
			}

			int2 getPlaceInGrid(float2 uv, int2 gridSize) {
				return floor(memberwiseMultiply(uv , gridSize));
			}

			fixed4 printNumber(float num, float2 uv) {
				int textRow = 1;
				int2 gridSize = int2(10, 3);

				int2 placeInGrid = getPlaceInGrid(uv, gridSize);

				if (placeInGrid.y < textRow) {
					return float4(1, 0, 0, 1);
				}
				else if (placeInGrid.y > textRow) {
					return float4(1, 1, 0, 1);
				}

				if (placeInGrid.x < 0) {
					return float4(0, 0, 1, 1);
				}
				else if (placeInGrid.x >= gridSize.x) {
					return float4(0, 1, 1, 1);
				}

				int integerPartPlaces = 4;
				int floatPartPlaces = 9;

				if (num < 0) {
					int digitCount = min(4,max(0, floor( log10(abs(num))) + 1));
					if (placeInGrid[0] == integerPartPlaces - digitCount) {
						return printCharacterInCell(3, uv, placeInGrid, gridSize, textRow);
					}else{
						num = -num;
					}
				}

				if ((num < 0 && abs(num) >= pow(10, 4)) || (num > 0 && abs(num) >= pow(10, 5))) {
					return printCharacterInCell(5, uv, placeInGrid, gridSize, textRow);
				}

				if (placeInGrid[0] <= 4) {
					int tenPow = round(pow(10, integerPartPlaces - placeInGrid[0]));
					if (abs(num) < tenPow) {
						return 0;
					}
					int digit = (abs(num) / tenPow) % 10;
					return printDigitInCell(digit, uv, placeInGrid, gridSize, textRow);
				}
				else if (placeInGrid[0] == 5) {
					return printCharacterInCell(0, uv, placeInGrid, gridSize, textRow);
				}
				else {
					num = floor(frac(num) * 10000.0);
					int tenPow = round(pow(10, floatPartPlaces - placeInGrid[0]));
					int digit = (abs(num) / tenPow) % 10;
					return printDigitInCell(digit, uv, placeInGrid, gridSize, textRow);
				}
			}

			fixed4 boxedPrintNumber(float num, float2 uv, float4 box) { //box.xy - offset  box.zw - size
				float2 newUv = inv_remapUVToBox(uv, box.xy, box.zw);
				fixed4 c = printNumber(num, newUv);
				if (c.a <= 0.01) {
					c = 1;
				}
				return c;
			}

			bool isInBox(float2 uv, float4 box){
				return (uv.x > box.x && uv.x < box.x+box.z && uv.y > box.y && uv.y < box.y+box.w);
			}

			fixed4 boxedPrintVector(float4 vec, float2 uv, float4 box) { //box.xy - offset  box.zw - size
				for (int i = 0; i < 4; i++) {
					float4 smallBox = float4(box.x, box.y + box.w / 4 * i, box.z, box.w / 4);
					if (isInBox(uv, smallBox)) {
						return boxedPrintNumber(vec[i], uv, smallBox);
					}
				}
				return 0;
			}

#endif
