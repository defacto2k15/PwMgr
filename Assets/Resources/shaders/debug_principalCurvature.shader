Shader "Custom/Debug/PrincipalCurvature"
{
	Properties 
	{
		_DummyTexture("DummyTexture", 2D) = "white"{}
		_HatchingTex("HatchingTex", 2DArray) = "white" {}
		_HatchingLevelsCount("HatchingLevelsCount", Range(0,6)) = 0
		_DebugRotation("DebugRotation",Range(0,4)) = 0
		_Tiling("Tiling", Range(0,1)) = 1
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
			#pragma target 4.6

			sampler2D _DummyTexture;
			UNITY_DECLARE_TEX2DARRAY(_HatchingTex);
			int _HatchingLevelsCount;
			float _DebugRotation;
			float _Tiling;

			struct appdata
			{
				float4 pos : POSITION;
				float3 nrm: NORMAL;
				float4 principalDirection1 : TEXCOORD1;
				float4 principalDirection2 : TEXCOORD2;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 nrm : NORMAL;
				float4 principalDirection : TEXCOORD1;
				float3 coords : ANY;
				float2 exValues : ANY1;
			};

			v2f vert (appdata i)
			{
				const float PI = 3.141592653589793238462;

				v2f o;
				//i.principalDirection = normalize( float4(i.principalDirection.xyz,0) * i.principalDirection.w);

				float4 principalDirection = i.principalDirection1 * sign(i.principalDirection1.x);//* i.principalDirection2.w;
				
				o.pos =  UnityObjectToClipPos(i.pos + normalize(principalDirection)*_Tiling);
				o.nrm = i.nrm; // object space normal
				o.principalDirection = (principalDirection); // principal direction
				o.coords = i.pos/100;
				o.exValues = float2(i.principalDirection1.w, i.principalDirection2.w);
				return o;
			}

			float2 TriPlanarUvWithRotation(float2 uv,  float2 maskedPrincipalDirection){
				maskedPrincipalDirection = normalize(maskedPrincipalDirection);

				float angle = atan(maskedPrincipalDirection.y/maskedPrincipalDirection.x);

				float sinX = sin (angle);
				float cosX = cos (angle);
				float2x2 rotationMatrix = float2x2( cosX, -sinX, sinX, cosX);

				float rotationPivot = float2(0.5, 0.5);
				return mul(rotationMatrix, uv-rotationPivot) + rotationPivot;
				//return uv;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// rotation matrix
				float textureRotationAngle = i.principalDirection.xy;
				float sinX = sin (textureRotationAngle);
				float cosX = cos (textureRotationAngle);
				float2x2 rotationMatrix = float2x2( cosX, -sinX, sinX, cosX);

				half3 blend = abs(i.nrm);
				//blend.xy = 0;
				blend /= dot(blend, 1.0);

				//Tri-planar texturing!
				fixed4 cx = tex2D(_DummyTexture, TriPlanarUvWithRotation(i.coords.yz, i.nrm.yz));
				float rv = 1;
				cx = sign(frac(TriPlanarUvWithRotation(i.coords.yz, i.nrm.yz).x*rv)-0.5);
				fixed4 cy = tex2D(_DummyTexture, TriPlanarUvWithRotation(i.coords.xz, i.nrm.xz));
				cy = sign(frac(TriPlanarUvWithRotation(i.coords.xz, i.nrm.xz).x*rv)-0.5);
				fixed4 cz = tex2D(_DummyTexture, TriPlanarUvWithRotation(i.coords.xy, i.nrm.xy)); 
				cz = sign(frac(TriPlanarUvWithRotation(i.coords.xy, i.nrm.xy).x*rv)-0.5);
				fixed4 c = cx * blend.x*0 + cy * blend.y*0 + cz * blend.z;

				float4 color;
				//float rotationPivot = float2(0.5, 0.5);
				//uv = mul(rotationMatrix, uv-rotationPivot) + rotationPivot;
				//color = tex2D(_DummyTexture, uv);
				//color = float3(i.pos.xy/100,0);
				color = c;
				//color = (1+i.principalDirection)/2;
				float alef = i.principalDirection.w;
				//color = color * (alef*100);
				//color = i.principalDirection;

				float2 rd = i.principalDirection.xy;
				rd = normalize(rd);
				float angle = atan(rd.y/rd.x);
				//color = (1 + angle/(3.14))/2 ;

				/*float v = i.exValues.xxxx*100;
				if(v > 0 ){
					color = float4(v,0,0,0);
				}else{
					color = float4(0,-v,0,0);
				}*/
				
				//color = i.principalDirection;
				if( blend.x > blend.y && blend.x > blend.z ){
					color = float4(1,0,0,0);
				}else if (blend.y > blend.x && blend.y > blend.z){
					color = float4(0,1,0,0);
				}else if (blend.z > blend.y && blend.z > blend.x){
					color = float4(0,0,1,0);
				}else{
					color = 0;
				}




				return float4(color.xyz,1);
			}

			ENDCG
		}
	} 
	FallBack "Diffuse"
}
