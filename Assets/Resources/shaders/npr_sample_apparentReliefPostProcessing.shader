Shader "Custom/NPR/Sample/ApparentReliefPostProcessing" {
     Properties
     {
			_MainTex ("MainTex", any) = "" {}
			_ApparentReliefTex ("ApparentReliefTex", any) = "" {}
			_ApparentReliefMapTexture("ApparentReliefMapTexture", 2D) = "white"{}
			_Sigma("Sigma", Range(0,2)) = 1
			_Selector("Selector", Range(0,1)) = 0
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
			#include "common.txt"

			struct v2f {
				float4 pos : SV_POSITION;
				float4 projPos : TEXCOORD0;
				float3 worldDirection : TEXCOORD1;
			}; 

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
			
			sampler2D _CameraDepthTexture;
			sampler2D _MainTex;
			sampler2D _ApparentReliefTex;
			sampler2D _ApparentReliefMapTexture;
			float4 _ApparentReliefTex_TexelSize;
			float _Sigma;
			float _Selector;

			float2 computeBluredApparentRelief(float2 uv, float sigma){
				int2 srcSize = _ApparentReliefTex_TexelSize.zw;
				float twoSigma2 = 2 * sigma * sigma;
				int halfWidth = int(ceil(2*sigma));

				float2 sum = 0;
				float norm = 0;
				[unroll(6)]
				for(int i = -halfWidth; i<= halfWidth; ++i){
					[unroll(6)]
					for(int j = -halfWidth; j <= halfWidth; j++){
						float d = length(float2(i,j));
						float kernel = exp(-d*d / twoSigma2);
						float2 c = remapNeg_2f(tex2D(_ApparentReliefTex, uv + float2(i,j)/srcSize).rg);
						sum += kernel * c;
						norm += kernel;
					}
				}
				float2 bluredRelief = sum/norm;
				return bluredRelief;
			}

			float4 computeNormalDeltas(float2 uv, float sigma){
				int2 srcSize = _ApparentReliefTex_TexelSize.zw;
				float twoSigma2 = 2 * sigma * sigma;
				int halfWidth = int(ceil(2*sigma));

				float2 sum = 0;
				float norm = 0;
				[unroll(6)]
				for(int i = -halfWidth; i<= halfWidth; ++i){
						float d = abs(i);
						float kernel = exp(-d*d / twoSigma2);
						float2 c = remapNeg_2f(tex2D(_ApparentReliefTex, uv + float2(i,0)/srcSize).ba);
						sum += kernel * c;
						norm += kernel;
				}
				float2 deltaNX = sum/norm;

				sum = 0;
				norm = 0;
				[unroll(6)]
				for(int i = -halfWidth; i<= halfWidth; ++i){
						float d = abs(i);
						float kernel = exp(-d*d / twoSigma2);
						float2 c = remapNeg_2f(tex2D(_ApparentReliefTex, uv + float2(0,i)/srcSize).ba);
						sum += kernel * c;
						norm += kernel;
				}
				float2 deltaNY = sum/norm;
				return float4(deltaNX, deltaNY);
			}

			float4 frag(v2f i) : COLOR {
				float4 reliefColor = tex2D(_ApparentReliefTex, i.projPos.xy);
				if( length(reliefColor) == 0 ){
					return float4(1,1,1,1);
				}
				reliefColor = reliefColor * 2  - 1;

				float2 uv = i.projPos.xy;
				float2 bluredRelief =  computeBluredApparentRelief(uv, _Sigma);

				float2 standardRelief = remapNeg_2f(tex2D(_ApparentReliefTex, uv).rg); 

				float4 normalDeltas = computeNormalDeltas(uv, _Sigma);
				float2 deltaN1 = normalDeltas.xz;
				float2 deltaN2 = normalDeltas.yw;

				float2 deltaNx = float2( dot(deltaN1, float2(1,0)),  dot(deltaN2, float2(1,0)));
				float2 deltaNy = float2( dot(deltaN1, float2(0,1)),  dot(deltaN2, float2(0,1)));

				float2x2 N = float2x2(dot(deltaNx, deltaNx), dot(deltaNx, deltaNy), dot(deltaNy, deltaNx), dot(deltaNy, deltaNy));

				// ROW FIRST!!!
				float2 means = float2( (N[0][0] + N[1][0])/2, (N[0][1] + N[1][1])/2);
				N[0] -= means[0];
				N[0][1] -= means[0];

				N[0][1] -= means[1];
				N[1][1] -= means[1];

				float2 X = float2(N[0][0],N[1][0]);
				float2 Y = float2(N[0][1],N[1][1]);

				means = float2( (N[0][0] + N[1][0])/2, (N[0][1] + N[1][1])/2);
				float meanX = means[0];
				float meanY = means[1];

				float covXY = (X[0] - meanX)*(Y[0] - meanY) + (X[1] - meanX)*(Y[1] - meanY);
				float covXX = (X[0] - meanX)*(X[0] - meanX) + (X[1] - meanX)*(X[1] - meanX);
				float covYY = (Y[0] - meanY)*(Y[0] - meanY) + (Y[1] - meanY)*(Y[1] - meanY);
				float2x2 C = float2x2(covXX, covXY, covXY,covYY);

				//computing eigenvalues: https://www.khanacademy.org/math/linear-algebra/alternate-bases/eigen-everything/v/linear-algebra-example-solving-for-the-eigenvalues-of-a-2x2-matrix
				float cA = C[0][0];
				float cB = C[0][1];
				float cC = C[1][0];
				float cD = C[1][1];
				float d = sqrt(cA*cA - 2*cA*cD + 4*cB*cC + cD*cD);

				float eigenValue1 = ( - d + cA + cD)/2;
				float eigenValue2 = ( d + cA + cD)/2;

				float maxAbsoluteEigenValue = max(abs(eigenValue1), abs(eigenValue2));

				float2 apparentReliefDescriptor = normalize(bluredRelief) * maxAbsoluteEigenValue;

				float3 color = tex2D(_ApparentReliefMapTexture, (remap_2f(lerp( normalize(standardRelief), normalize(bluredRelief), _Selector)))).rgb;
				color = tex2D(_ApparentReliefMapTexture, remap_2f(apparentReliefDescriptor));

				float2 standardReliefX = remapNeg_2f(remap_2f((tex2D(_ApparentReliefTex, uv).rg))); 
				//standardReliefX =tex2D(_ApparentReliefTex, uv).rg; 
				//color = tex2D(_ApparentReliefMapTexture, standardReliefX);

				//return float4(normalize(bluredRelief), 0,1);				
				return float4(color,1);
			}
			ENDCG
		}
	}
}