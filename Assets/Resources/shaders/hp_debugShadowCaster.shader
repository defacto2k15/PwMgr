Shader "Custom/Hp/DebugShadowCaster"
{
	SubShader
	{
		// very simple lighting pass, that only does non-textured ambient
		Pass
		{
			Tags {"LightMode" = "ForwardBase"}
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "AutoLight.cginc"

			struct v2f
			{
                float2 uv : TEXCOORD0;
				float4 _ShadowCoord : TEXCOORD1;
                fixed3 diff : COLOR0;
                fixed3 ambient : COLOR1;
                float4 pos : SV_POSITION;
			};
			v2f vert(appdata_base v)
			{
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0.rgb;
                o.ambient = ShadeSH9(half4(worldNormal,1));
                // compute shadows data
				o._ShadowCoord = ComputeScreenPos(o.pos);
                return o;
			}
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = float4(0.8,0.7,0.87,1);
                // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
                fixed shadow = SHADOW_ATTENUATION(i);
                // darken light's illumination with shadow, keep ambient intact
                fixed3 lighting = i.diff * shadow + i.ambient;
                col.rgb *= lighting;
				
				return col;
			}
			ENDCG
		}
        // shadow casting support
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}