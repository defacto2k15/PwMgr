﻿Shader "Custom/Sandbox/SuggestiveContoursAlg2PP" {
	Properties
	{
			_MainTex("", any) = "" {}
			_IntensityDifferenceTreshold("_IntensityDifferenceTreshold", Range(0,0.5)) = 0.05
			_DarkerCountFactor("_DarkerCountFactor", Range(0,1)) = 0.36
			_FilterRadius("FilterRadius",Int) = 2
	}
		SubShader{
			Tags { "RenderType" = "Opaque" }
			Pass {
				Fog { Mode Off }
				Cull Off
				CGPROGRAM
				#pragma target 5.0
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

				sampler2D _MainTex;

				int2 uv_to_intScreenCoords(float2 uv) {
					return int2(floor(uv.x * _ScreenParams.x), floor(uv.y * _ScreenParams.y));
				}

				float2 intScreenCoords_to_uv(int2 coords) {
					return float2(coords.x / _ScreenParams.x, coords.y / _ScreenParams.y);
				}

				float _IntensityDifferenceTreshold;
				float _DarkerCountFactor;
				int _FilterRadius;

				float4 frag(v2f i) : COLOR {

					float color = 1;
					float2 uv = i.projPos.xy;
					int2 centerCoords = uv_to_intScreenCoords(uv);
					float centerIntensity = tex2D(_MainTex, intScreenCoords_to_uv(centerCoords)).r;

					int darkerCount = 0;
					float max_intensity = centerIntensity;
					int radius = _FilterRadius;

					for (int x = -radius; x <= radius; x++) {
						for (int y = -radius; y <= radius; y++) {
							int2 currentCoords = centerCoords + int2(x, y);
							float currentIntensity = tex2D(_MainTex, intScreenCoords_to_uv(currentCoords)).r;
							if (currentIntensity < centerIntensity) {
								darkerCount++;
							}
							max_intensity = max(currentIntensity, max_intensity);
						}
					}

					//if ((max_intensity - centerIntensity) > 0.01 * radius) {
					//	if (((float)darkerCount) / (radius*radius) < (1.0 - (1.0 / radius))) {
					//		color = 0;
					//	}
					//}

					if ((max_intensity - centerIntensity) > _IntensityDifferenceTreshold*radius) {
						if (((float)darkerCount) / pow(radius*2 + 1, 2) < _DarkerCountFactor) {
							color = 0;
						}
					}
					//color = centerIntensity;
					return color;
				}
				ENDCG
			}
	}
}
