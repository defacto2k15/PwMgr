Shader "Custom/Debug/NPR/ShowingMRTPP"
{
	Properties
	{
		_TexBuffer0("_TexBuffer0", 2D) = "green"{}
		_TexBuffer1("_TexBuffer1", 2D) = "green"{}
		_TexBuffer2("_TexBuffer2", 2D) = "green"{}
		_TexBuffer3("_TexBuffer3", 2D) = "green"{}
		_TextureSelector("TextureSelector", Int) = 0
	}

	CGINCLUDE
#include "UnityCG.cginc"

	sampler2D _TexBuffer0;
	sampler2D _TexBuffer1;
	sampler2D _TexBuffer2;
	sampler2D _TexBuffer3;
	int _TextureSelector;

	//// Simple combiner
	half4 frag_combine(v2f_img i) : SV_Target
	{
		float4 color = 0;
		if (_TextureSelector == 0) {
			color = tex2D(_TexBuffer0, i.uv);
		}else if (_TextureSelector == 1) {
			color = tex2D(_TexBuffer1, i.uv);
		}else if (_TextureSelector == 2) {
			color = tex2D(_TexBuffer2, i.uv);
		}else if (_TextureSelector == 3) {
			color = tex2D(_TexBuffer3, i.uv);
		}
		else {
			color = 0.12;
		}
		return color;
	}

		ENDCG

		SubShader
	{
		Cull Off ZWrite Off ZTest Always
			Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag_combine
			ENDCG
		}
	}
}
