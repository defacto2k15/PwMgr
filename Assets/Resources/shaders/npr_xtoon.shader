Shader "Custom/NPR/XToon" {
    Properties {
		_ArrayElementSelector("ArrayElementSelector", Range(-1,1)) = 0
		_ArrayLodSelector("ArrayLodSelector", Range(-1,1)) = 0
		_ControlTex("ControlTex", 2D)  = "white" {}
    }
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "common.txt"
						
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 norm : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 nrm : TEXCOORD1;
				float distanceToCamera : ANY;
				float3  viewDir : ANY2;
			};

			float _ArrayElementSelector;

			sampler2D _ControlTex;
			float4 _ControlTex_ST;

			float4 _LightColor0;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv * _ControlTex_ST.xy + _ControlTex_ST.zw;

				/// #### Kwantyzacja normalnych
				//v.norm = round(v.norm * 4) / 4;

				o.nrm = mul(float4(v.norm, 0.0), unity_WorldToObject).xyz;
				o.distanceToCamera = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, v.vertex));

				float4x4 modelMatrix = unity_ObjectToWorld;
				float4x4 modelMatrixInverse = unity_WorldToObject; 
				o.viewDir = normalize(_WorldSpaceCameraPos - mul(modelMatrix,v.vertex).xyz);

				return o;
			}

			float3 XToon(fixed intensity){
				fixed lightCoord = 1-(intensity+1)/2;
			}

			float intensityToXCoord(float intensity){
				return 1-(intensity+1)/2;
			}

			float cameraDistanceToYCoord(float distance){
				return 1-invLerpClamp(1, 10, distance);
			}

			float silhouetteSmoothnessToYCoord(float smoothness, float3 normalDir, float3 viewDir){
				float3 normalDirection = normalize(normalDir);
				float3 viewDirection = normalize(viewDir);
				float silhouetteFactor = abs(dot(viewDirection, normalDirection));
				return saturate(pow(saturate(1- silhouetteFactor), smoothness));
			}

			float2 silhouetteToBothCoords(float oldX, float smoothness, float3 normalDir, float3 viewDir){
				float3 normalDirection = normalize(normalDir);
				float3 viewDirection = normalize(viewDir);
				float silhouetteFactor = abs(dot(viewDirection, normalDirection));
				if(silhouetteFactor > 0.3 ){
					return float2(oldX, saturate(pow(saturate(1- silhouetteFactor), smoothness)));
				}else{
					return float2(1,1);
				}
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed intensity =  dot(_WorldSpaceLightPos0, normalize(i.nrm));

				fixed xcoord = intensityToXCoord(intensity);
				//fixed ycoord = 1;

				// ### 1
				//fixed ycoord = cameraDistanceToYCoord(i.distanceToCamera);

				// ### 2
				//fixed ycoord = silhouetteSmoothnessToYCoord(_ArrayElementSelector, i.nrm, i.viewDir);

				//fixed3 color = tex2D(_ControlTex, fixed2(xcoord, ycoord));

				// ### 3
				fixed3 color = tex2D(_ControlTex, silhouetteToBothCoords(xcoord, _ArrayElementSelector, i.nrm, i.viewDir));

				return fixed4(color,1);
			}
			ENDCG
		}
	}
}