Shader "Custom/EVegetation/BaseTreeLocaledInstanced"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
		_Color("Color", Vector) = (1,1,1,1)
		_ScopeLength("ScopeLength", Range(0,300)) = 0
	}
		SubShader
	{
			CGPROGRAM
			#pragma vertex vert
			#pragma surface surf Lambert addshadow vertex:vert 
			#pragma target 3.0
			#pragma multi_compile_instancing
			#include "UnityCG.cginc" // for UnityObjectToWorldNormal


			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float, _Pointer)
				UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
			UNITY_INSTANCING_BUFFER_END(Props)

			int _ScopeLength;  

			#include "eterrain_EPropLocaleHeightAccessing.hlsl"
			#include "noise.hlsl"

			struct Input {
				float2 uv_MainTex;
			};

			void vert (inout appdata_base v ){
				float heightOffset =  RetriveHeight();
				float3 objectOffset = mul((float3x3)unity_WorldToObject, float3(0,heightOffset,0));
				v.vertex.xyz += objectOffset;
			} 
			
			fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
			{
				fixed4 c;
				c.rgb = s.Albedo; 
				c.a = s.Alpha;  
				return c;
			}  
           
            sampler2D _MainTex;

			void surf(in Input i, inout SurfaceOutput o) {
				float3 startColor = tex2Dlod(_MainTex, float4(i.uv_MainTex, 0, 0)).rgb;

				float3 brownPattern = float3(132,77,16) / 255.0;
				float3 greenPattern = float3(107,178,16) / 255.0;

				bool isGreenElement = false;
				if (length(brownPattern - startColor) > length(greenPattern - startColor)) {
					isGreenElement = true;
				}

				float3 finalColor;
				float4 propColor = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
				if (isGreenElement) {
					finalColor = propColor; 
				}
				else {
					finalColor = brownPattern* (0.5+((rand(propColor.r)*0.5)-0.25)); //TODO some intelligent implementation
				}

				o.Albedo = finalColor;

			}
            ENDCG
    }
}