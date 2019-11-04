// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
Shader "Custom/NPR/NoiseContour" {
     Properties
     {
			_MainTex ("", any) = "" {}
			_BufferTex("BufferTex", 2D) = "" {}
			_Param1("Param1", Range(0,1000)) = 0
			_Param2("Param2", Range(0,1000)) = 0
			_NormalsSensivity("NormalsSensivity", Range(0,10)) = 0.3
			_DepthSensivity("DepthSensivity", Range(0,10)) = 1.5
     }
	SubShader{
	// Na bazie kodu https://www.shadertoy.com/view/MscSzf
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
			}; 

			struct pixelSample {
				float3 normal;
				float depth;
			};

			float4x4 _ClipToWorld;

			sampler2D _CameraDepthTexture;
			sampler2D _CameraDepthNormalsTexture;
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			float _NormalsSensivity;
			float _DepthSensivity;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				return o;
			}

			float checkSame(pixelSample center, pixelSample samplef)
			{
				float2 Sensitivity = (float2(0.3, 1.5) * _MainTex_TexelSize.zw / 400);
				Sensitivity.x *= _NormalsSensivity;
				Sensitivity.y *= _DepthSensivity;
				// sensitivity.x odpowiada za normals
				// sensitivity.y za depth


				float3 centerNormal = center.normal;
				float centerDepth = center.depth;
				float3 sampleNormal = samplef.normal;
				float sampleDepth = samplef.depth;
				
				float3 diffNormal = abs(centerNormal - sampleNormal) * Sensitivity.x;
				bool isSameNormal = (diffNormal.x + diffNormal.y ) < 0.1;
				float diffDepth = abs(centerDepth - sampleDepth) * Sensitivity.y;
				bool isSameDepth = diffDepth < 0.1;
				
				return (isSameDepth && isSameNormal) ? 1.0 : 0.0;
			}

			pixelSample samplePixel(float2 uv){
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
				depth = LinearEyeDepth(depth);

				float3 normalValues;
				float inDepth = depth;
				DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv), inDepth, normalValues);

				pixelSample sample;
				sample.normal = normalValues;
				sample.depth = round(depth/20*255)/255;;
				return sample;
			}

			float4 frag(v2f i) : COLOR {
				int2 srcSize = _MainTex_TexelSize.zw;
				float2 uv = i.projPos.xy;

				pixelSample sample0 = samplePixel( uv);
				pixelSample sample1 = samplePixel( uv + (float2(1.0, 1.0) / srcSize));
				pixelSample sample2 = samplePixel( uv + (float2(-1.0, -1.0) / srcSize));
				pixelSample sample3 = samplePixel( uv + (float2(-1.0, 1.0) / srcSize));
				pixelSample sample4 = samplePixel( uv + (float2(1.0, -1.0) / srcSize));
				
				float edge = checkSame(sample1, sample2) * checkSame(sample3, sample4);
				
				return float4(edge,uv.x,uv.y,1); 
			}
			ENDCG
		}

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

			float4x4 _ClipToWorld;

			sampler2D _CameraDepthTexture;
			sampler2D _CameraDepthNormalsTexture;
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			sampler2D _BufferTex;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);

				float4 clip = float4(o.pos.xy, 0.0, 1.0);
				o.worldDirection = mul(_ClipToWorld, clip) - _WorldSpaceCameraPos;

				return o;
			}

			float my_triangle(float x)
			{
				return abs(1.0 - fmod(abs(x), 2.0)) * 2.0 - 1.0;
			}

			float4 frag(v2f i) : COLOR {
				int2 srcSize = _MainTex_TexelSize.zw;
				float2 uv = frac(i.projPos.xy);
				float2 originalUv = uv;

				//float3 edge = tex2D(_BufferTex, i.projPos.xy).rgb;
				
				float time = _Time[0];
				// kod ktory generuje trzesienie ekranu
				uv += float2( 
					my_triangle(uv.y * rand(time )) * rand(time* 1.9) * 0.005,
					my_triangle(uv.x * rand(time *3.4)) * rand(time* 2.1) * 0.005) *  0.3;

				float noiseFactor = 0.005;
				float noise = snoise2D(uv*30) * noiseFactor;
				//float noise = snoise2D(i.pos.xy/30) * noiseFactor;

				float ErrorPeriod = 3;
				float ErrorRange = 0.003;
				float2 uvs[3];
				uvs[0] = uv + float2(ErrorRange * sin(ErrorPeriod * uv.y + 0.0) + noise, ErrorRange * sin(ErrorPeriod * uv.x + 0.0) + noise);
				uvs[1] = uv + float2(ErrorRange * sin(ErrorPeriod * uv.y + 1.047) + noise, ErrorRange * sin(ErrorPeriod * uv.x + 3.142) + noise);
				uvs[2] = uv + float2(ErrorRange * sin(ErrorPeriod * uv.y + 2.094) + noise, ErrorRange * sin(ErrorPeriod * uv.x + 1.571) + noise);

			    float edge = tex2D(_BufferTex, uvs[0]).r * tex2D(_BufferTex, uvs[1]).r * tex2D(_BufferTex, uvs[2]).r;
				
				float3 color = tex2D(_MainTex, originalUv);
				return float4(color*edge, 1);
			}
			ENDCG
		}
	}
}