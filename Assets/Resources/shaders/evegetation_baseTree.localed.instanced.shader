﻿Shader "Custom/EVegetation/BaseTreeLocaledInstanced"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
		_ScopeLength("ScopeLength", Range(0,300)) = 0
	}
		SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc" // for UnityObjectToWorldNormal


			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(float, _Pointer)
			UNITY_INSTANCING_BUFFER_END(Props)

			int _ScopeLength;  
			#include "eterrain_EPropLocaleHeightAccessing.hlsl"

            // vertex shader inputs
            struct appdata
            {
                float4 vertex : POSITION; 
                float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION; 
				float3 worldNormal : ANY_NORMAL;
            };

            // vertex shader
            v2f vert (appdata v)
            {
                v2f o;

				float4 vertexWorldPos =  mul(unity_ObjectToWorld , v.vertex);
				vertexWorldPos.y += RetriveHeight();
				o.vertex =  mul(UNITY_MATRIX_VP, float4(vertexWorldPos.xyz, 1.0));

                o.uv = v.uv;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            
            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
				return tex2Dlod(_MainTex, float4(i.uv,0,0));
            }
            ENDCG
        }
    }
}