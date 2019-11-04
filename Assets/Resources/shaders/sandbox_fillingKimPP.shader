Shader "Custom/Sandbox/Filling/KimPP" {
	Properties{
		_ShadingTex("ShadingTex", 2D) = "pink"{}
		_GeoPositionTex("GeoPositionTex", 2D) = "pink"{}
		_NormalsTex("NormalsTex", 2D) = "pink"{}
		_TangentsTex("TangentsTex", 2D) = "pink"{}
		_DebugScalar("DebugScalar", Range(0,2)) = 1

		_HatchTex("HatchTex", 2D) = "pink" {}
		_QuantizationLevels("QuantizationLevels", Range(0,48)) = 0

		_UvMultiplier("UvMultiplier", Range(0,10)) = 1
		_AngleOffset("AngleOffset", Range(0,6)) = 0
		_BlendingMargin("BlendingMargin", Range(0,1)) = 0
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
			#include "UnityCG.cginc"

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : TEXCOORD0;
			};

				float4x4 _ClipToWorld;


				v2f vert(appdata_base v) {
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					o.projPos = ComputeScreenPos(o.pos);

					return o;
				}


			uint2 uv_to_intScreenCoords(float2 uv) {
				return uint2(floor(uv.x * _ScreenParams.x), floor(uv.y * _ScreenParams.y));
			}

			float2 intScreenCoords_to_uv(int2 coords) {
				return float2(coords.x / _ScreenParams.x, coords.y / _ScreenParams.y);
			}

			struct Plane {
				float3 normal;
				float3 positionOnPlane; 
			};

			Plane make_Plane(float3 normal, float3 positionOnPlane) {
				Plane p;
				p.normal = normal;
				p.positionOnPlane = positionOnPlane;
				return p;
			}

			struct Ray {
				float3 origin;
				float3 direction;
			};

			Ray make_Ray(float3 origin, float3 direction) {
				Ray ray;
				ray.origin = origin;
				ray.direction = direction;
				return ray;
			}

			struct NormalRay {
				float o;
				float t;
				float u;
				float v;
			};

			NormalRay make_NormalRay(float o, float t, float u, float v) {
				NormalRay r;
				r.o = o;
				r.t = t;
				r.u = u;
				r.v = v;
				return r;
			}

			float3 intersectionPoint(Plane plane, Ray ray) {
				float rDotn = dot(ray.direction, plane.normal);

				float s = dot(plane.normal, (plane.positionOnPlane- ray.origin)) / rDotn;
				return ray.origin + s * ray.direction;
			}


			sampler2D _ShadingTex;
			sampler2D _GeoPositionTex;
			sampler2D _NormalsTex;
			sampler2D _TangentsTex;
			sampler2D _HatchTex;

			float _DebugScalar;
			int _QuantizationLevels;
			float _UvMultiplier;
			float _AngleOffset;
			float _BlendingMargin;

#include "text_printing.hlsl"


			float2 getCenterPixelCoords(float2 uv) {
				return uv + float2(0.5 / _ScreenParams.x, 0.5 / _ScreenParams.y);
			}

			fixed4 frag(v2f input) : SV_Target
			{
				fixed4 color = 1;
				float2 uv = input.projPos.xy;
				//uv = float2(0.5, 0.4);
				float2 specuv = intScreenCoords_to_uv(int2(285, 296));
				//uv = specuv;

				//color = tex2D(_ShadingTex, uv);
				//color = tex2D(_GeoPositionTex, uv);
				//color = tex2D(_NormalsTex, uv);

				float2 ourCoord = uv_to_intScreenCoords(uv);
				float2 ourUv = intScreenCoords_to_uv(ourCoord);

				float3 ourNormal = tex2D(_NormalsTex, getCenterPixelCoords(ourUv)).xyz;
				ourNormal = normalize(ourNormal);
				float3 ourGeoPos = tex2D(_GeoPositionTex, getCenterPixelCoords(ourUv)).xyz;

				float4 ourTangents = tex2D(_TangentsTex, getCenterPixelCoords(ourUv));

				// this all is in global frame
				Ray ourRay = make_Ray(ourGeoPos, ourNormal);
				Plane ourBasePlane = make_Plane(ourNormal, ourGeoPos);
				Plane ourSecondPlane = make_Plane(ourNormal, ourGeoPos + ourNormal);
				float3 ourIntersectionPoint1 = intersectionPoint(ourBasePlane, ourRay);
				float3 ourIntersectionPoint2 = intersectionPoint(ourSecondPlane, ourRay);

				float2 other1Uv = intScreenCoords_to_uv(ourCoord + int2(1, 0));
				float3 other1Normal = normalize(tex2D(_NormalsTex, getCenterPixelCoords(other1Uv)).xyz);
				float3 other1GeoPos = tex2D(_GeoPositionTex, getCenterPixelCoords(other1Uv)).xyz;
				Ray other1Ray = make_Ray(other1GeoPos, other1Normal);
				float3 other1IntersectionPoint1 = intersectionPoint(ourBasePlane, other1Ray);
				float3 other1IntersectionPoint2 = intersectionPoint(ourSecondPlane, other1Ray);

				float2 other2Uv = intScreenCoords_to_uv(ourCoord + int2(0, 1));
				float3 other2Normal = normalize(tex2D(_NormalsTex, getCenterPixelCoords(other2Uv)).xyz);
				float3 other2GeoPos = tex2D(_GeoPositionTex, getCenterPixelCoords(other2Uv)).xyz;
				Ray other2Ray = make_Ray(other2GeoPos, other2Normal);
				float3 other2IntersectionPoint1 = intersectionPoint(ourBasePlane, other2Ray);
				float3 other2IntersectionPoint2 = intersectionPoint(ourSecondPlane, other2Ray);

				// now we will transform vertices to tangent frame
				float3 b3 = normalize(ourNormal);
				float3 b1= ourTangents.xyz;
				float3 b2 = cross(b1, b3) * ourTangents.w;

				float3 origin = ourGeoPos;

				float3x3 orientationMat = {
					b1.x, b2.x, b3.x,
					b1.y, b2.y, b3.y, 
					b1.z, b2.z, b3.z, 
				};

	

				float3x3 transformMat = transpose(orientationMat);

				float3 framedOurGeoPos = mul(transformMat, ourIntersectionPoint2 - origin );

				float3 framedOther1IntersectionPoint1 = mul(transformMat, other1IntersectionPoint1 - origin);
				float3 framedOther1IntersectionPoint2 = mul(transformMat, other1IntersectionPoint2 - origin);
				// parameters s t u v
				float4 other1Parameters = float4(
					framedOther1IntersectionPoint2.x,
					framedOther1IntersectionPoint2.y,
					framedOther1IntersectionPoint1.x,
					framedOther1IntersectionPoint1.y);
				NormalRay nr1 = make_NormalRay(
					other1Parameters.x - other1Parameters.z,
					other1Parameters.y - other1Parameters.w,
					other1Parameters.w,
					other1Parameters.z);

				float3 framedOther2IntersectionPoint1 = mul(transformMat, other2IntersectionPoint1 - origin);
				float3 framedOther2IntersectionPoint2 = mul(transformMat, other2IntersectionPoint2 - origin);
				// parameters s t u v
				float4 other2Parameters = float4(
					framedOther2IntersectionPoint2.x,
					framedOther2IntersectionPoint2.y,
					framedOther2IntersectionPoint1.x,
					framedOther2IntersectionPoint1.y);
				NormalRay nr2 = make_NormalRay(
					other2Parameters.x - other2Parameters.z,
					other2Parameters.y - other2Parameters.w,
					other2Parameters.w,
					other2Parameters.z);

				/// Solving equation 1
				float A = nr1.o*nr2.t - nr2.o*nr1.t;
				float B = nr1.o*nr2.v + nr2.t*nr1.u - nr2.o*nr1.v - nr1.t*nr2.u;
				float C = nr1.u*nr2.v - nr2.u*nr1.v;

				float ta = A;
				float tb = B;
				float tc = C;

				//C = ta;
				//B = tb;
				//A = tc;

				// sometimes delta is <0. The abs is dealing with that
				float delta = abs(B*B - 4 * A*C);
				float sqrtDelta = sqrt(delta);
				float lambda1 = (-B - sqrtDelta) / (2 * A);
				//lambda1 = 1 / lambda1;
				float lambda2 = (-B + sqrtDelta) / (2 * A);
				//lambda2 = 1 / lambda2;

				// we compute directions
				float3 pd1 = normalize(float3(nr1.u + nr1.o*lambda1, nr1.v + nr1.t*lambda1, 0));
				// additional abs
				pd1 = abs(pd1);
				float3 pd2 = normalize(float3(nr2.u + nr2.o*lambda2, nr2.v + nr2.t*lambda2, 0));

				float angle = (atan2(pd1.y, pd1.x));
				//here i added abs
				angle = abs(angle);

				float quantisizedInt = floor(angle * _QuantizationLevels);
				float inLevelPosition = (angle*_QuantizationLevels - quantisizedInt);

				float3 offsetsColors[2];
				float offsetsWeights[2];
				offsetsWeights[0] = 1;
				if (inLevelPosition > 0.5) {
					offsetsWeights[1] = (inLevelPosition - 0.5) * 2;
				}
				else {
					offsetsWeights[1] = 1 - (inLevelPosition) * 2;
				}
				
				// OFFSETS
				for (int j = 0; j < 2; j++) {
					if (j == 1) {
						if (inLevelPosition > 0.5) {
							quantisizedInt++;
							inLevelPosition = (inLevelPosition - 0.5) * 2;
						}
						else {
							quantisizedInt--;
							inLevelPosition = 1 - (inLevelPosition) * 2;
						}
					}


					float quantisizedAngles[3];
					quantisizedAngles[0] = (quantisizedInt - 1) / _QuantizationLevels;
					quantisizedAngles[1] = quantisizedInt / _QuantizationLevels;
					quantisizedAngles[2] = (quantisizedInt + 1) / _QuantizationLevels;


					float blendingWeights[3];
					blendingWeights[0] = saturate(1 - inLevelPosition / _BlendingMargin);
					blendingWeights[1] = 1;
					blendingWeights[2] = saturate(1 - (1 - inLevelPosition) / _BlendingMargin);//

					float2 hatchCoords[3];
					for (int i = 0; i < 3; i++) {
						float myangle = quantisizedAngles[i] + _AngleOffset;
						hatchCoords[i] = float2(
							cos(myangle)*uv.x - sin(myangle)*uv.y,
							sin(myangle)*uv.x + cos(myangle)*uv.y
							);
					}

					float3 colors[3];
					for (int i = 0; i < 3; i++) {
						colors[i] = tex2Dlod(_HatchTex, float4(hatchCoords[i], 1, 0)* _UvMultiplier);
					}

					offsetsColors[j] = 1;
					offsetsColors[j] -= (1 - colors[0])*blendingWeights[0];
					offsetsColors[j] -= (1 - colors[1])*blendingWeights[1];
					offsetsColors[j] -= (1 - colors[2])*blendingWeights[2];
					
					color.xyz -= (1-offsetsColors[j])*offsetsWeights[j] ;
				}


				return color;
			}
			ENDCG
		}
	}
}
