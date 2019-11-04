    Shader "Custom/Diagram/CartoonLighting"{
        Properties {
            _MainTex ("Texture", 2D) = "white" {}
			_BaseColor("BaseColor", Color) = (1.0,0.0,0.0,0.0)
			_DebugScalar("DebugScalar", Range(0,0.3)) = 0.0
			_Alpha("Alpha", Range(0,1)) = 1.0
		}
			SubShader{
			Tags {"Queue" = "Transparent" "RenderType"="Transparent" }
			CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0

					float _DebugScalar;
			float _Alpha;

        struct Input {
            float2 uv_MainTex;
        };
        
        sampler2D _MainTex;
		float4 _BaseColor;
        
        void surf (Input IN, inout SurfaceOutputStandard o) {
			o.Albedo =  _BaseColor;
			o.Metallic = 0;
			o.Smoothness = 0;
			o.Alpha = _Alpha;
        }
        ENDCG
        }
        Fallback "Diffuse"
    }