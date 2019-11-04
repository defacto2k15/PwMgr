Shader "Custom/Debug/BezierCurve" {
	Properties{
		_StartPoint("StartPoint", Range(0,1)) = 0
		_StartDirection("StartDirection", Range(0, 1)) = 0
		_EndPoint("EndPoint", Range(0,1)) = 0.5
		_EndDirection("EndDirection", Range(0, 1)) = 0

		_DebPos1X("DebPos1X", Range(-1,1)) = 0
		_DebPos1Y("DebPos1Y", Range(-1,1)) = 0
		_DebPos2X("DebPos2X", Range(-1,1)) = 0
		_DebPos2Y("DebPos2Y", Range(-1,1)) = 0
	}

		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;

			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 worldSpacePos : ANY_WORLD_SPACE_POS;
				float2 uv : ANY_UV;
			};

			float _StartPoint;
			float _StartDirection;
			float _EndPoint;
			float _EndDirection;

			float _DebPos1X;
			float _DebPos1Y;
			float _DebPos2X;
			float _DebPos2Y;

			v2f vert(appdata in_v, uint vid : SV_VertexID)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(in_v.vertex);
				o.worldSpacePos = mul(unity_ObjectToWorld, in_v.vertex);
				o.uv = in_v.uv;  

				return o; 
			}

			// na podstawie https://www.shadertoy.com/view/lsV3Wd
			// F(x,y)
			float F ( in float2 coords, float4 coef )
			{
				float A = coef[0];
				float B = coef[1];
				float C = coef[2];
				float D = coef[3];

				float T = coords.x;
				return
					(A * (1.0 - T) * (1.0 - T) * (1.0 - T)) +
					(B * 3.0 * (1.0 - T) * (1.0 - T) * T) +
					(C * 3.0 * (1.0 - T) * T * T) +
					(D * T * T * T) - coords.y;
			}


			float2 grad( in float2 coords, float4 coef )
			{
				float A = coef[0];
				float B = coef[1];
				float C = coef[2];
				float D = coef[3];

				float T = coords.x;
				float T2 = T * T;
				float S = (1.0 - T);
				float S2 = S * S;
				
				float DF =
					(B-A) * S2 +
					(C-B) * 2.0 * S * T +
					(D-C) * T2;
				
				return float2 (
					3.0*DF,
					-1.0
				);
			}

			float sdfBezier(float2 coords, float4 coef) {
				float v = F(coords, coef);
				float2 g = grad(coords, coef); // to jest tylko do kontroli grubości linii
				return abs(v)/ length(g);
			}

			float2 pointRangeToUv(float range) {
				if (range < 0.25) {
					return float2(0, range * 4);
				}
				else if (range < 0.5) {
					return float2((range-0.25)*4, 1);
				}
				else if (range < 0.75) {
					return float2(1, 1- (range-0.5)*4);
				}
				else {
					return float2(1- (range-0.75)*4,0);
				}
			}


			float4 frag1(v2f input) : SV_Target
			{
				float2 p1Uv = pointRangeToUv(_StartPoint);
				float p1Angle = _StartDirection * 3.14159 * 2;
				float2 p1Vec = float2(cos(p1Angle), sin(p1Angle));
				if (length(p1Uv - input.uv) < 0.02) {
					return float4(1, 0, 0, 1);
				}

				float2 tP1Vec = normalize(input.uv - p1Uv);
				float tP1Dist = length(input.uv - p1Uv);
				if (length(tP1Vec - p1Vec) < (0.01 / tP1Dist) && tP1Dist < 0.1) {
					return float4(1, 1, 0, 1);
				}

				float2 p2Uv = pointRangeToUv(_EndPoint);
				float p2Angle = _EndDirection* 3.14159 * 2;
				float2 p2Vec = float2(cos(p2Angle), sin(p2Angle));
				if (length(p2Uv - input.uv) < 0.02) {
					return float4(0, 0, 1, 1);
				}

				float2 tP2Vec = normalize(input.uv - p2Uv);
				float tP2Dist = length(input.uv - p2Uv);
				if (length(tP2Vec - p2Vec) < (0.01/tP2Dist) && tP2Dist < 0.1) {
					return float4(0, 1, 1, 1);
				}

				input.uv.y = (input.uv.y - 0.5) * 2;
				if (sdfBezier(input.uv, float4(0, 1, -0.25, 0)) < 0.04) {
					return float4(0, 1, 0, 1);
				}

				return 1;
			}

			float3x3 RSTMatrix(float2 t, float angle, float scale) {
				scale = 1 / scale;
				float3x3 tMat = {
					1, 0, t.x,
					0, 1, t.y,
					0, 0, 1
				};

				float3x3 rMat = {
					cos(angle), sin(angle), 0,
					-sin(angle), cos(angle), 0,
					0, 0, 1
				};

				float3x3 sMat = {
					scale, 0, 0,
					0, scale, 0,
					0,0,1
				};

				return mul(mul(rMat, sMat), tMat);
				//return mul(mul(tMat, rMat), sMat);
			}

			float distanceToLine(float3 lineCoeffs, float2 p) {
				return abs(lineCoeffs[0] * p.x + lineCoeffs[1] * p.y + lineCoeffs[2])
					/ sqrt(pow(lineCoeffs[0], 2) + pow(lineCoeffs[1], 2));
			}

			struct curveDistanceResult {
				float distanceToLine;
				float distanceFromStart;
				float wholeCurveLength;
			};

			curveDistanceResult make_curveDistanceResult( float distanceToLine, float distanceFromStart, float wholeCurveLength ) {
				curveDistanceResult o;
				o.distanceToLine = distanceToLine;
				o.distanceFromStart = distanceFromStart;
				o.wholeCurveLength = wholeCurveLength;
				return o;
			}

			float2 rayBoxIntersectionPoint(float2 p, float angle) {
				float PI = 3.14159;
				// from -PI , PI to 0; 2PI
				if (angle < 0) {
					angle = PI +  -1*angle;
				}
				// we will check intersection with two lines - one horizontal and one vertical
				float xToCheck = 0;
				float yToCheck = 0;

				if (angle < PI / 2) {
					xToCheck = 1;
					yToCheck = 1;
				}else if (angle < PI ) {
					xToCheck = 0;
					yToCheck = 1;
				}else if (angle < PI * 1.5 ) {
					xToCheck = 0;
					yToCheck = 0;
				}else {
					xToCheck = 1;
					yToCheck = 0;
				}

				float alpha = tan(angle);
				// as in y = alpha*x + c
				float c = p.y - p.x*alpha;

				float2 horizontalPoint;
				float2 verticalPoint;

				if (xToCheck == 0) {
					verticalPoint = float2(0, c);
				}
				else {
					verticalPoint = float2(1, alpha + c);
				}

				if (yToCheck == 0) {
					horizontalPoint = float2(-c / alpha, 0);
				}
				else {
					horizontalPoint = float2( (1-c) / alpha, 1);
				}

				if (length(p - verticalPoint) < length(p - horizontalPoint)) {
					return verticalPoint;
				}
				else {
					return horizontalPoint;
				}
			}

			curveDistanceResult distanceToMyLine(float2 uv, float2 p1, float2 p1Vec, float2 p2, float2 p2Vec) {
				float arcLength = length(p1 - p2); //very crude approximation
				float2 iPoint = rayBoxIntersectionPoint(p2, atan2(p2Vec.y, p2Vec.x));
				float lineLength = length(iPoint - p2);
				// we have to compute distances to four 

				float2 delta = (p2 - p1);
				float2 nDelta = normalize(delta);
				float angle = atan2(nDelta.y, nDelta.x);
				float scale = length(delta);

				float3x3 rstMat = RSTMatrix(-p1, angle, scale);
				float2 nuv = mul( rstMat, float3(uv,1) ).xy;
				float2 cp1Vec = mul( (float2x2)rstMat,(p1Vec));
				float2 cp2Vec = mul( (float2x2)rstMat,(p2Vec));

				if (nuv.x > 1 ) {
					float2 dvB = p2Vec;
					float2 dvA = uv - p2;
					float2 dr = dot(dvA, normalize(dvB))*normalize(dvB);
					float2 rr = p2 + dr;

					float distanceToLine = length(uv - rr) *1.5; //to make distance constant with one from bezier
					float distanceToEndOfLine = length(rr - iPoint); 

					return make_curveDistanceResult(
						distanceToLine, arcLength + (lineLength - distanceToEndOfLine), arcLength + lineLength);
				}

				float yb = (1 / 3.0)* cp1Vec.y / cp1Vec.x;
				float yc = (-1 / 3.0)* (cp2Vec.y / cp2Vec.x);

				float distanceToBezier = sdfBezier(nuv, float4(0, yb, yc, 0));
				return make_curveDistanceResult( distanceToBezier, arcLength*nuv.x , arcLength + lineLength);
			}


			// length of curve is computed from arc length
			// f(t) = 3*B(1-t)^2*t + 3*C*(1-t)*t^2  becouse for me A=D=0
			// f`(t) = 3 (C (2 - 3 x) x + B (1 - 4 x + 3 x^2))
			// arc length is integral from a to b of sqrt( 1 + [f't]^2)
			// [f`(t)]^2 = 81 B^2 x^4 - 216 B^2 x^3 + 198 B^2 x^2 - 72 B^2 x + 9 B^2 - 162 B C x^4 + 324 B C x^3 - 198 B C x^2 + 36 B C x + 81 C^2 x^4 - 108 C^2 x^3 + 36 C^2 x^2
			// i'l not compute this now... TODO later

			float4 frag(v2f input) : SV_Target{
				float2 uv = input.uv;

				uv = (uv - 0.5) * 5;
				float2 p1Uv = pointRangeToUv(_StartPoint);
				//p1Uv = float2(0.4913, 1);

				float p1Angle = _StartDirection * 3.14159 * 2;
				float2 p1Vec = float2(cos(p1Angle), sin(p1Angle));
				//p1Vec = float2(0, 1);

				float2 tP1Vec = normalize(uv - p1Uv);
				float tP1Dist = length(uv - p1Uv);

				float2 p2Uv = pointRangeToUv(_EndPoint);
				p2Uv = float2(_DebPos2X, _DebPos2Y);

				float p2Angle = _EndDirection* 3.14159 * 2;
				float2 p2Vec = float2(cos(p2Angle), sin(p2Angle));
				//p2Vec = float2(0, 1);

				float2 tP2Vec = normalize(uv - p2Uv);
				float tP2Dist = length(uv - p2Uv);

				if (length(p1Uv - uv) < 0.02) {
					return float4(1, 0, 0, 1);
				}
				if (length(tP1Vec - p1Vec) < (0.01 / tP1Dist) && tP1Dist < 0.1) {
					return float4(1, 1, 0, 1);
				}
				if (length(p2Uv - uv) < 0.02) {
					return float4(0, 0, 1, 1);
				}
				if (length(tP2Vec - p2Vec) < (0.01/tP2Dist) && tP2Dist < 0.1) {
					return float4(0, 1, 1, 1);
				}

				float distance = distanceToMyLine(uv, p1Uv, p1Vec, p2Uv, p2Vec).distanceToLine;
				if (distance < 0.01) {
					return 0;
				}

				if (min(min(abs(uv.x), abs(uv.y)), min(abs(1 - uv.x), abs(1 - uv.y))) < 0.01) {
					return 0.2;
				}

				return 1;
			}

			float4 frag4(v2f input) : SV_Target
			{
				float p1Angle = _StartDirection * 3.14159 * 2;
				float2 p1Vec = float2(cos(p1Angle), sin(p1Angle));

				float p2Angle = _EndDirection* 3.14159 * 2;
				float2 p2Vec = float2(cos(p2Angle), sin(p2Angle));

				float2 dp1 = float2(_DebPos1X, _DebPos1Y);
				float2 dp2 = float2(_DebPos2X, _DebPos2Y);

				float2 uv = input.uv;
				if (length(dp1 - uv) < 0.01) {
					return float4(1, 0, 0, 1);
				}
				if (length(dp2 - uv) < 0.01) {
					return float4(0, 1, 0, 1);
				}

				float2 delta = (dp2 - dp1);
				float2 nDelta = normalize(delta);
				float angle = atan2(nDelta.y, nDelta.x);
				float scale = length(dp2 - dp1);

				float3x3 rstMat = RSTMatrix(-dp1, angle, scale);
				float2 nuv = mul( rstMat, float3(uv,1) ).xy;
				p1Vec = mul(rstMat, float3(p1Vec,1));
				p2Vec = mul(rstMat, float3(p2Vec,1));

				if (nuv.x > 1) {
					if (distanceToLine(float3(p2Vec.y, -p2Vec.x, -p2Vec.y), nuv) < 0.01) {
						return float4(1, 0.1, 0.7, 1);
					}
				}

				float yb = (1 / 3.0)* p1Vec.y / p1Vec.x;
				float yc = (-1 / 3.0)* (p2Vec.y / p2Vec.x);

				if (sdfBezier(nuv, float4(0, yb, yc, 0)) < 0.01) {
					return float4(0, 1, 0, 1);
				}

				if (length(nuv - p1Vec*0.2) < 0.01) {
					return float4(0.5, 0, 0.1, 1);
				}
				if (length( nuv - (float2(1,0) + p2Vec*0.2)) < 0.02) {
					return float4(0.1, 0, 0.9, 1);
				}

				if (nuv.x > 0 && nuv.x < 1 && nuv.y > 0 && nuv.y < 1) {
					return 0;
				}

				return 1;
			}
			ENDCG
		}
	}
}
