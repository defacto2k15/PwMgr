// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/NPR/SimpleTAM" {
    Properties {
		_ArrayElementSelector("ArrayElementSelector", Range(0,16)) = 0
		_ArrayLodSelector("ArrayLodSelector", Range(0,16)) = 0
		_MainTex("MainTex", 2D)  = "white" {}
		_HatchingTex("HatchingTex", 2DArray) = "white" {}
		_HatchingLevelsCount("HatchingLevelsCount", Range(0,6)) = 0
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
			};

			float _ArrayElementSelector;
			UNITY_DECLARE_TEX2DARRAY(_HatchingTex);
			int _HatchingLevelsCount;

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4 _LightColor0;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
				o.nrm = mul(float4(v.norm, 0.0), unity_WorldToObject).xyz;
				return o;
			}

			fixed3 Hatching(float2 _uv, half _intensity, int levels)
			{ // todo - smooth lod
				_intensity = saturate(_intensity);
				float lowerIntensity = floor(_intensity*(levels+1));
				float upperIntensity = ceil(_intensity*(levels+1));

				float lowerHatch = 1-UNITY_SAMPLE_TEX2DARRAY(_HatchingTex, float3(_uv, clamp((levels-1)-(lowerIntensity - 1), 0, levels-1)  )).a;
				if(lowerIntensity < 1){
					lowerHatch = 0;	
				}
				if(lowerIntensity >= levels  + 0.01){
					lowerHatch = 1;
				}

				float upperHatch =  1-UNITY_SAMPLE_TEX2DARRAY(_HatchingTex, float3(_uv, clamp((levels-1)-(upperIntensity- 1), 0, levels-1))).a; 
				if(upperIntensity >= levels  + 0.01 ){
					upperHatch = 1;
				}

				//float f = frac(_intensity * (levels+1));
				//return min(lowerHatch, 1-step(upperHatch, 1-f)); // alternatywa
				return lerp(lowerHatch, upperHatch, frac(_intensity*(levels+1)));
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = tex2D(_MainTex, i.uv);
				fixed3 diffuse = color.rgb * _LightColor0.rgb * dot(_WorldSpaceLightPos0, normalize(i.nrm));

				fixed intensity = dot(diffuse, fixed3(0.2326, 0.7152, 0.0722));
				fixed intensity2 =  dot(_WorldSpaceLightPos0, normalize(i.nrm));

				color.rgb =  Hatching(i.uv , intensity2,_HatchingLevelsCount);

				return color;
			}
			ENDCG
		}
	}
}