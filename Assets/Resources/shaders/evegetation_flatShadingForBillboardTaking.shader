Shader "Custom/EVegetation/FlatShadingForBillboardTaking"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#include "UnityCG.cginc" // for UnityObjectToWorldNormal

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
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            
            sampler2D _MainTex;


#define PI 3.1415926536f
half2 encodeNormalInTwoComponents (half3 n)
{
      return (half2(atan2(n.y,n.x)/PI, n.z)+1.0)*0.5;
}

            fixed4 frag (v2f i) : SV_Target
            {
				float3 brownPattern = float3(132,77,16) / 255.0;
				float3 greenPattern = float3(107,178,16) / 255.0;
                float3 colorFromTexture = tex2D(_MainTex, i.uv);

				float colorMarker = 0;
				if (length(brownPattern - colorFromTexture) < length(greenPattern - colorFromTexture)) {
					colorMarker = 1;
				}
				float2 encodedNormal = encodeNormalInTwoComponents(i.worldNormal);

				return float4(colorMarker, encodedNormal, 1);
            }
            ENDCG
        }
    }
}