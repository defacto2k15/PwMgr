// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
Shader "Custom/PostProcessing" {
     Properties
     {
			_MainTex ("", any) = "" {}
			_Param1("Param1", Range(0,1000)) = 0
			_Param2("Param2", Range(0,1000)) = 0
			_CloudPositionOffset("_CloudPositionOffset", Vector)= (1.0, 1.0, 0.0, 0.0)
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
			#include "noise.hlsl"

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : TEXCOORD0;
				float3 worldDirection : TEXCOORD1;
			}; 
			// na bazie z https://github.com/unitycoder/UnityBuiltinShaders/blob/master/DefaultResources/Particle%20MultiplyDouble.shader 
			//oraz https://forum.unity.com/threads/effect-shader-world-pos-calculation.68639/

			float4x4 _ClipToWorld;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				COMPUTE_EYEDEPTH(o.projPos.z);

				float4 clip = float4(o.pos.xy, 0.0, 1.0);
				o.worldDirection = mul(_ClipToWorld, clip) - _WorldSpaceCameraPos;

				return o;
			}

			float _Param1;
			float _Param2;
			float4 _CloudPositionOffset;

			float4 linearFog(float4 inputColor, float depth) {
				float4 fogColor = float4(0.5, 0.5, 0.5, 1);
				float fogStart = 1000;
				float fogEnd = 2000;
				float f = saturate((fogEnd- depth) / (fogEnd = fogStart));
				return lerp( fogColor, inputColor, f);
			}


			float4 exponentFog(float4 inputColor, float depth) {
				float4 fogColor = float4(0.5, 0.5, 0.5, 1);
				float b = 0.0005;
				float f = saturate(exp(-depth*b));
				return lerp( fogColor, inputColor, f);
			}

			float4 exponentSquaredFog(float4 inputColor, float depth) {
				float4 fogColor = float4(0.5, 0.5, 0.5, 1);
				float b = 0.0003;
				float f = saturate(exp(-pow(depth*b,2)));
				return lerp( fogColor, inputColor, f);
			}

			//bazuje na http://in2gpu.com/2014/07/22/create-fog-shader/
			float4 complexLightFog(float4 inputColor, float depth) {
				float4 lightColor = inputColor;
				float4 fogColor = float4(0.5, 0.5, 0.5, 1);

				float b1 = _Param1;
				float b2 = _Param2;
				b2 = b1;

				float in_scattering = exp(-depth*b1);
				float in_extinction = exp(-depth*b2);

				return fogColor * (1 - in_scattering) +  lightColor* in_extinction;
			}

GEN_fractalNoise3D( cloudNoise, 5, snoise3D, 0, 1)

//--------------------------------------------------------------------------
// Grab all sky information for a given ray from camera
			float3 GetSky(in float3 rd, in float3 cameraPos)
			{
				float3 sunLight = normalize(float3(0.4, 0.4, 0.48));
				float3 sunColour = float3(1.0, .9, .83);
				float3 skyColor = float3(0.18, 0.22, 0.4)*2.1;

				float sunAmount =  max(dot(rd, sunLight), 0.0); // 1 gdy kąt raya i słońca się zgadza
				float v = pow(1.0 - max(rd.y, 0.0), 5.)*.5;
				float3  sky = float3(v*sunColour.x*0.4 + skyColor.r, v*sunColour.y*0.4 + skyColor.g, v*sunColour.z*0.4 + skyColor.b);
				// Wide glare effect...
				sky = sky + sunColour * pow(sunAmount, 6.5)*0.69; // tym jaśniejsze/większe słońce, im ten ostatni wskaźnik większy
				// Actual sun...
				sky = sky + sunColour * min(pow(sunAmount, 1150.0), .3)*.65; // ustawianie jasności kółka słonecznego

				//////////////// CLOUDS!!!!
				if (rd.y < 0.01) {
					return sky;
				}
				v =  (200.0 - cameraPos.y) / rd.y;
				rd.xz *= v;
				rd.xz += cameraPos.xz;
				rd.xz *= 0.01; // ten parametr ustawia jak wysoko są chmury

				float cn =  cloudNoise(float3(rd.xz / 5, 0)+_CloudPositionOffset);
				float dn =  ((cn)+1.75)*0.36;

				float f =  dn* 2; //wskaźnik 1 - jak bardzo jest zachmurzenie
				f = pow(f, 1 - f);

				// Uses the ray's y component for horizon fade of fixed colour clouds...
				float3 cloudColor = float3(.55, .55, .52)*1.1;
				cloudColor =
					lerp(
						lerp(
							lerp(sunColour, cloudColor * 3, 0.4 + sqrt(sunAmount * 10 + 0.1) /1),
							cloudColor*0.6,
							f),
						cloudColor,
						1.4 - sunAmount);

				if (f < 0.01) {
					return sky;
				}

				float cloudControlFactor = saturate(f*rd.y - .05);
				sky = lerp(sky, cloudColor, cloudControlFactor);

				return sky;
			}


			float3 nonConstantFog(float3 inputColor, float depth, float3 cameraPosition, float3 cameraToPointVector) {
				float3 originalFogColor = float3(0.5, 0.6, 0.7);
				float3 skyColor = float3(0.18, 0.22, 0.4)*1.8;
				float3 fogColor = lerp(originalFogColor, skyColor, 1);

				float heightFactor = 0.0005;
				float c = _Param1/100000;
				float b = _Param2/1;

				float fogAmount = c * exp(-cameraPosition.y*heightFactor*b) * (1 - exp(-depth*cameraToPointVector.y*heightFactor*b)) / (cameraToPointVector.y*heightFactor*b);
				return lerp(inputColor, fogColor, saturate(lerp(0, 0.5, fogAmount)));
			}


			float3 finalFog(float3 inputColor, float3 skyColor, float depth, float3 targetPosition, in float3 rd) {
				float3 originalFogColor = float3(0.5, 0.6, 0.7);
				float3 fogColor = lerp(originalFogColor, skyColor, invLerpClamp(10000, 15000, depth));

				float3 sunLight = normalize(float3(0.4, 0.4, 0.48));
				float3 sunColor = float3(1.0, .9, .83);

				float b = 0.00015;
				if (depth > 10000) {
					depth = depth + (depth - 10000) * 60000;
				}
				float f = saturate(exp(-depth*b));

				float heightFogMax = 1200;
				float heightFogMin = 750; 
				float f1 = saturate(
							(((targetPosition.y - heightFogMin) / (heightFogMax - heightFogMin))) +0.2
				);
				f1 = lerp(2 * f1, 1, pow(f,4));
				f =  saturate(min(f1,f));

				return lerp(fogColor, inputColor, f);
			}
			 
			
			sampler2D _CameraDepthTexture;
			sampler2D _MainTex;

			float4 frag(v2f i) : COLOR {
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.projPos.xy);
				depth = LinearEyeDepth(depth);

				float3 targetPointWorldspacePosition = i.worldDirection * depth + _WorldSpaceCameraPos;

				float3 col = tex2D(_MainTex, i.projPos.xy).rgb;
				
				float3 rd = normalize(i.worldDirection);
				float3 outColor;
				if ((rd.y < -0.2) && depth > 14000) {
					outColor = tex2D(_MainTex, i.projPos.xy-float2(0.01, 0.01)).rgb;
				}
				else {
					float3 skyColor =  GetSky(rd, _WorldSpaceCameraPos);
					outColor = skyColor;
					if (depth > 10000) {
						outColor = skyColor;
					}
					else {
						outColor = finalFog(col, skyColor, depth, targetPointWorldspacePosition, rd);
					}
				}

				return float4(outColor, 1);
				return depth/10000.0;
			}
			ENDCG
		}
	}
}