Shader "Custom/NPR/PostProcessingFilter" {
     Properties
     {
			_MainTex ("", any) = "" {}
			_KernelTex("_KernelTex", 2D) = "white" {}
			_Kernel4Tex("_Kernel4Tex", 2D) = "white" {}
			_TfmTex("TfmTex", 2D) = "white" {}

			_Sigma("Sigma", Range(0,2)) = 0
			_Radius("Radius", Range(0,5)) = 1
			_QParam("QParam", Range(0,5)) = 0
			_ChangeComparision("ChangeComparision", Range(0,1)) = 0
			_Alpha("Alpha", Range(0,4)) = 1
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

			float4x4 _ClipToWorld;

			float _Sigma;
			float _Radius;
			float _QParam;
			float _ChangeComparision;
			float _Alpha;

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.projPos = ComputeScreenPos (o.pos);
				COMPUTE_EYEDEPTH(o.projPos.z);

				float4 clip = float4(o.pos.xy, 0.0, 1.0);
				o.worldDirection = mul(_ClipToWorld, clip) - _WorldSpaceCameraPos;

				return o;
			}

			sampler2D _CameraDepthTexture;
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;

			sampler2D _KernelTex;
			sampler2D _Kernel4Tex;
			sampler2D _TfmTex;

// KOD Z // by Jan Eric Kyprianidis <www.kyprianidis.com>
			float4 frag(v2f i) : COLOR {
				const float PI = 3.14159;
				float3 originalColor = tex2D(_MainTex, i.projPos.xy).rgb;
				float2 uv = i.projPos.xy;

				int2 srcSize = _MainTex_TexelSize.zw;
				float3 outColor = 1;
#ifdef GAUSSIAN_BLUR
				float twoSigma2 = 2 * _Sigma * _Sigma;
				int halfWidth = int(ceil(2*_Sigma));

				float3 sum = 0;
				float norm = 0;
				if(halfWidth > 0 ){
					for(int i = -halfWidth; i<= halfWidth; ++i){
						for(int j = -halfWidth; j <= halfWidth; j++){
							float d = length(float2(i,j));
							float kernel = exp(-d*d / twoSigma2);
							float3 c = tex2D(_MainTex, uv + float2(i,j)/srcSize).rgb;
							sum += kernel * c;
							norm += kernel;
						}
					}
				}
				outColor = sum/norm;
#endif

#ifdef TFM
				float3 g = originalColor;
				// Paragraf 3.1 Roznica ze w jednym jest plus, a w drogim minus
				// E - g.x
				// G - g.y
				// F - g.z

				float lambda1 = 0.5 * (g.y + g.x + 
					sqrt(g.y*g.y - 2.0*g.x*g.y + g.x*g.x + 4.0*g.z*g.z));
				float lambda2 = 0.5 * (g.y + g.x -
					sqrt(g.y*g.y - 2.0*g.x*g.y + g.x*g.x + 4.0*g.z*g.z));

				float2 v = float2(lambda1 - g.x, -g.z); // gx to E gz to F
				float2 t;
				if (length(v) > 0.0) { 
					t = normalize(v);
				} else {
					t = float2(0.0, 1.0);
				}

				float phi = atan2(t.y, t.x); //todo

				// A to anisotropy!
				float A = (lambda1 + lambda2 > 0.0)?
					(lambda1 - lambda2) / (lambda1 + lambda2) : 0.0;
				outColor = A;
#endif

#ifdef KUWAHARA
				int radius = _Radius;
				float n = float((radius + 1) * (radius + 1));

				float3 m[4];
				float3 s[4];
				for (int k = 0; k < 4; ++k) {
					m[k] = (0.0);
					s[k] = (0.0);
				}

				for (int j = -radius; j <= 0; ++j)  {
					for (int i = -radius; i <= 0; ++i)  {
						float3 c = tex2D(_MainTex, uv + float2(i,j) / srcSize).rgb;
						m[0] += c;
						s[0] += c * c;
					}
				}

				for (int j = -radius; j <= 0; ++j)  {
					for (int i = 0; i <= radius; ++i)  {
						float3 c = tex2D(_MainTex, uv + float2(i,j) / srcSize).rgb;
						m[1] += c;
						s[1] += c * c;
					}
				}

				for (int j = 0; j <= radius; ++j)  {
					for (int i = 0; i <= radius; ++i)  {
						float3 c = tex2D(_MainTex, uv + float2(i,j) / srcSize).rgb;
						m[2] += c;
						s[2] += c * c;
					}
				}

				for (int j = 0; j <= radius; ++j)  {
					for (int i = -radius; i <= 0; ++i)  {
						float3 c = tex2D(_MainTex, uv + float2(i,j) / srcSize).rgb;
						m[3] += c;
						s[3] += c * c;
					}
				}


				float min_sigma2 = 1e+2;
				for (int k = 0; k < 4; ++k) {
					m[k] /= n;
					s[k] = abs(s[k] / n - m[k] * m[k]);

					float sigma2 = s[k].r + s[k].g + s[k].b;
					if (sigma2 < min_sigma2) {
						min_sigma2 = sigma2;
						outColor = float4(m[k], 1.0);
					}
				}			
#endif

#ifdef GENERALIZED_KUWAHARA
				int N = 8;
				int radius = _Radius;
				float q = _QParam;

				float4 m[8]; //averages
				float3 s[8]; // std dev
				for (int k = 0; k < N; ++k) {
					m[k] = (0.0);
					s[k] = (0.0);
				}

				float piN = 2.0 * PI / float(N);
				float2x2 X = float2x2(cos(piN), sin(piN), -sin(piN), cos(piN));


				for ( int j = -radius; j <= radius; ++j ) {
					for ( int i = -radius; i <= radius; ++i ) {
						float2 v = 0.5 * float2(i,j) / float(radius);
						if (dot(v,v) <= 0.25) { // to robi KOLO!!!
							float4 c_fix = tex2D(_MainTex, uv + float2(i,j) / srcSize);
							float3 c = c_fix.rgb; 
							for (int k = 0; k < N; ++k) {
								float w = tex2D(_KernelTex, float2(0.5, 0.5) + v).r; // kernelTex jest wypelnione tylko w 1/8. Tak powinno byc!
								// Chodzi o to, ze obliczamy jaki wpsyw na k-ty sektor ma dany punkt. Jak petla sie powtarza, to mul(v,X) sprawia, ze uv sie zmieniaja
								// i wskazuja na nastepny sektor!
								m[k] += float4(c * w, w);
								s[k] += c * c * w;

								v = mul(v,X);
							}
						}
					}
				}

				float4 o = (0.0);
				for (int k = 0; k < N; ++k) {
					m[k].rgb /= m[k].w;
					s[k] = abs(s[k] / m[k].w - m[k].rgb * m[k].rgb);

					float sigma2 = s[k].r + s[k].g + s[k].b;
					float w = 1.0 / (1.0 + pow(255.0 * sigma2, 0.5 * q));

					o += float4(m[k].rgb * w, w);
				}
				outColor = o.rgb/o.w;

#endif

#ifdef  AKF_V1
				int N = 8;
				int radius = _Radius;
				float q = _QParam;
				// alpha should be 1
				// when infinity, matrix S converges to identity
				float alpha = _Alpha;


				float4 m[8];
				float3 s[8];
				for (int k = 0; k < N; ++k) {
					m[k] = (0.0);
					s[k] = (0.0);
				}

				float piN = 2.0 * PI / float(N);
				float2x2 X = float2x2(cos(piN), sin(piN), -sin(piN), cos(piN));

				float4 t = tex2D(_TfmTex, uv);
				// dwa elementy z poczatku rozdzialu 3.2
				// t.w to A - anisotropy!
				float a = radius * clamp((alpha + t.w) / alpha, 0.1, 2.0); 
				float b = radius * clamp(alpha / (alpha + t.w), 0.1, 2.0);

				// t.z to phi - arg t  (eigenvector)
				float cos_phi = cos(t.z);
				float sin_phi = sin(t.z);

				float2x2 R = float2x2(cos_phi, -sin_phi, sin_phi, cos_phi);
				float2x2 S = float2x2(0.5/a, 0.0, 0.0, 0.5/b);
				float2x2 SR = mul(S,R);

				int max_x = int(sqrt(a*a * cos_phi*cos_phi +
									  b*b * sin_phi*sin_phi));
				max_x = min(3, max_x);					  
				int max_y = int(sqrt(a*a * sin_phi*sin_phi +
									  b*b * cos_phi*cos_phi));
				max_y = min(3, max_y);					  
				// iterujemy po elipsie?	


				[unroll(6)]
				for (int j = -max_y; j <= max_y; ++j) {
					[unroll(6)]
					for (int i = -max_x; i <= max_x; ++i) {
						float2 v = mul(SR , float2(i,j));
						if (dot(v,v) <= 0.25) {
						float4 c_fix = tex2D(_MainTex, uv + float2(i,j) / srcSize);
						float3 c = c_fix.rgb;
						for (int k = 0; k < N; ++k) {
							float w = tex2D(_KernelTex, float2(0.5, 0.5) + v).x;

							m[k] += float4(c * w, w);
							s[k] += c * c * w;

							v = mul(v,X);
							}
						}
					}
				}

				float4 o = (0.0);
				for (int k = 0; k < N; ++k) {
					m[k].rgb /= m[k].w;
					s[k] = abs(s[k] / m[k].w - m[k].rgb * m[k].rgb);

					float sigma2 = s[k].r + s[k].g + s[k].b;
					float w = 1.0 / (1.0 + pow(255.0 * sigma2, 0.5 * q));

					o += float4(m[k].rgb * w, w);
				}
				outColor = o.rgb/o.w;
#endif



#ifndef AKF_V2
				int N = 8;
				int radius = _Radius;
				float q = _QParam;
				// alpha should be 1
				// when infinity, matrix S converges to identity
				float alpha = _Alpha;

				float4 m[8];
				float3 s[8];
				for (int k = 0; k < N; ++k) {
					m[k] = (0.0);
					s[k] =(0.0);
				}

				float4 t = tex2D(_TfmTex, uv);
				float a = radius * clamp((alpha + t.w) / alpha, 0.1, 2.0); 
				float b = radius * clamp(alpha / (alpha + t.w), 0.1, 2.0);

				float cos_phi = cos(t.z);
				float sin_phi = sin(t.z);

				float2x2 R = float2x2(cos_phi, -sin_phi, sin_phi, cos_phi);
				float2x2 S = float2x2(0.5/a, 0.0, 0.0, 0.5/b);
				float2x2 SR = mul(S,R);

				int max_x = int(sqrt(a*a * cos_phi*cos_phi +
									  b*b * sin_phi*sin_phi));
				max_x = min(3, max_x);					  
				int max_y = int(sqrt(a*a * sin_phi*sin_phi +
									  b*b * cos_phi*cos_phi));
				max_y = min(3, max_y);					  

				{
					float3 c = tex2D(_MainTex, uv).rgb;
					float w = tex2D(_Kernel4Tex, float2(0.5, 0.5)).x;
					for (int k = 0; k < N; ++k) {
						m[k] +=  float4(c * w, w);
						s[k] += c * c * w;
					}
				}

				[unroll(6)]
				for (int j = 0; j <= max_y; ++j)  {
					[unroll(6)]
					for (int i = -max_x; i <= max_x; ++i) {
						if ((j !=0) || (i > 0)) {
							float2 v = mul(SR,float2(i,j));

							if (dot(v,v) <= 0.25) {
								float3 c0 = tex2D(_MainTex,uv + float2(i,j)/srcSize).rgb;
								float3 c1 = tex2D(_MainTex,uv - float2(i,j)/srcSize).rgb;

								float3 cc0 = c0 * c0;
								float3 cc1 = c1 * c1;

								float4 w0123 = tex2D(_Kernel4Tex, float2(0.5, 0.5) + v);
								for (int k = 0; k < 4; ++k) {
									m[k] += float4(c0 * w0123[k], w0123[k]);
									s[k] += cc0 * w0123[k];
								}
								for (int k = 4; k < 8; ++k) {
									m[k] += float4(c1 * w0123[k-4], w0123[k-4]);
									s[k] += cc1 * w0123[k-4];
								}

								float4 w4567 = tex2D(_Kernel4Tex, float2(0.5, 0.5) - v);
								for (int k = 4; k < 8; ++k) {
									m[k] += float4(c0 * w4567[k-4], w4567[k-4]);
									s[k] += cc0 * w4567[k-4];
								}
								for (int k = 0; k < 4; ++k) {
									m[k] += float4(c1 * w4567[k], w4567[k]);
									s[k] += cc1 * w4567[k];
								}
							}
						}
					}
				}

				float4 o =(0.0);
				for (int k = 0; k < N; ++k) {
					m[k].rgb /= m[k].w;
					s[k] = abs(s[k] / m[k].w - m[k].rgb * m[k].rgb);

					float sigma2 = s[k].r + s[k].g + s[k].b;
					float w = 1.0 / (1.0 + pow(255.0 * sigma2, 0.5 * q));

					o += float4(m[k].rgb * w, w);
				}
				outColor = o.rgb/o.w;
#endif
				return float4(lerp(outColor, originalColor, _ChangeComparision),1);
			}
			ENDCG
		}
	}
}