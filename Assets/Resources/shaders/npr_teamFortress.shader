Shader "Custom/NPR/TeamFortress" {
    Properties {
		_ArrayElementSelector("ArrayElementSelector", Range(-1,1)) = 0
		_Alpha("Alpha", Range(0,1)) = 0
		_Beta("Beta", Range(0,1)) = 0
		_Gamma("Gamma", Range(0,10)) = 0
		_MainTex("MainTex", 2D)  = "white" {}
		_NormalTex("NormalTex", 2D) = "white" {}
		_WarpControlTex("WarpControlTex", 2D) = "white"{}

		_FresnelTerm("FresnelTerm", Range(0,1)) = 0
		_SpecularMaskValue("SpecularMaskValue", Range(0,1)) = 1
		 _SpecularComponent("SpecularComponent", Range(0,1)) = 1
		 _RimComponent("_RimComponent", Range(0,1)) = 1
		 _RimMask("_RimMask", Range(0,1)) = 1
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
				float2 uv : TEXCOORD0;
				float3 nrm : TEXCOORD1;
				float distanceToCamera : ANY;
				float3  viewDir : ANY2;
				float3 localPos : ANY3;
			};

			float _ArrayElementSelector;
			float _FresnelTerm;
			float _RimComponent;
			float _SpecularMaskValue;
			float _SpecularComponent;
			float _RimMask;

			float _Alpha;
			float _Beta;
			float _Gamma;
			sampler2D _WarpControlTex;

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _NormalTex;

			float4 _LightColor0;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.localPos = v.vertex;
				o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;

				o.nrm = normalize(mul(float4(v.norm, 0.0), unity_WorldToObject).xyz);
				o.distanceToCamera = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, v.vertex));

				float4x4 modelMatrix = unity_ObjectToWorld;
				float4x4 modelMatrixInverse = unity_WorldToObject; 
				o.viewDir = normalize(_WorldSpaceCameraPos - mul(modelMatrix,v.vertex).xyz);

				return o;
			}

			float3 WorldSpaceViewDir2( in float4 v )
			{
				return normalize(_WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, v).xyz);
			}

			float ambientTerm(float3 normal){
				return saturate(0.1 * normal.y + 0.05); // na gorze jest jasniej
			}

			float3 warp( float intensity ){
				return tex2D(_WarpControlTex, float2(intensity, 0.5))*2; // uwaga na mnożnik * 2;
			}

			float3 ambientCube(float3 viewDirection){
				return 0.1;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3 normal = i.nrm; //UnpackNormal(tex2D(_NormalTex, i.uv))-1; 

				fixed4 lightColor = _LightColor0;
				fixed intensity = dot(_WorldSpaceLightPos0, normalize(normal));
				float4 albedo = tex2D(_MainTex, i.uv);

				float alpha = _Alpha; //scale
				float beta = _Beta; //bias
				float gamma = _Gamma; //exponent

				float3 outColor = albedo*( ambientTerm(normal) + lightColor*warp( pow(alpha*(intensity) + beta, gamma)));

				float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				//if(dot(normal, lightDirection)  < 0.0){ // LIGHT FROM BACK!!!!
				//	return float4(1,0,1,1);
				//}

				float3 diffuse = max(0.0, dot(normal, lightDirection));

				float3 viewDirection = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, i.pos).xyz);
				float3 specular = pow( max(0, dot( reflect(-lightDirection, normal), viewDirection)), _ArrayElementSelector);

				// view dependent lighting
				float specularMaskValue =  _SpecularMaskValue; //ks
				float fresnelTerm = _FresnelTerm; //fs
				float specularComponent = _SpecularComponent; //kspec
				float rimHighlightsFresnel = pow(1 - dot(normal, viewDirection), 4); //fr THIS IS RIM LIGHTING!
				float rimMask = _RimMask;
				float rimComponent = _RimComponent; //krim
				//k rim is constant for whole object and significantly smaller than kspec

				float vd = dot( viewDirection, reflect(-lightDirection, normal));
				float3 dependent = lightColor*specularMaskValue*
					max( 
						fresnelTerm*pow(vd, specularComponent) , // this is specular!
						rimHighlightsFresnel * rimMask * pow(vd,  rimComponent )
						)
					+ dot(normal, float3(0,1,0))*rimHighlightsFresnel * rimMask * ambientCube(viewDirection); //ambient part
				dependent = saturate(dependent);

				return fixed4(dependent+outColor, 0);
			}
			ENDCG
		}
	}
}