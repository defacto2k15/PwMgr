Shader "Custom/NPR/Sample/HaoLines" {
    Properties {
		_MainTex("MainTex", 2D)  = "white" {}
		_RidgeTreshold("RidgeTreshold", Range(-2,2)) = 0
		_ValleyTreshold("ValleyTreshold", Range(-2,2)) = 0

		_TextureKParam("TextureKParam", Range(0,1)) = 1
		_TextureScale("TextureScale", Range(0,10)) = 1
		_EdgeSize("EdgeSize", Range(0,0.2)) = 0.01
		_LineClosingFactor("LineClosingFactor", Range(0,3)) = 1
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
			#pragma geometry geom
			#pragma target 5.0
						
			struct appdata
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float3 nrm : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 nrm : TEXCOORD1; 
			};

			struct pixelIn
			{
				float4 pos : SV_POSITION;
				float3 nrm : TEXCOORD1; 
				int triangleType : ANY;
				half2 lineStatus : ANY1;
				float2 barycentricCoords : ANY2;
			};

			struct AdjacencyInfo{
				float3 pos[3];
				float3 nrm[3];
			};

			StructuredBuffer<AdjacencyInfo> _AdjacencyBuffer;

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4 _LightColor0;

			float _EdgeSize;
			float _TextureKParam;
			float _TextureScale;

			float _RidgeTreshold;
			float _ValleyTreshold;

			float _LineClosingFactor;
			
			appdata vert (appdata v)
			{
				return v;
			}

			float3 getNormal( float3 v1, float3 v2, float3 v3){
				return normalize(cross(normalize(v2-v1), normalize(v3-v1)));
			}

			appdata initialize_appdata(float3 position, float3 normal, float2 uv){
				v2f v;
				v.pos = float4(position,1);
				v.nrm = normal;
				v.uv = uv; 
				return v;
			}
			
			void addTriangle(inout TriangleStream<v2f> outStream, v2f v1, v2f v2, v2f v3, float uv){
				v1.uv = uv;
				v2.uv = uv;
				v3.uv = uv;

				outStream.Append(v1);
				outStream.Append(v2);
				outStream.Append(v3);

				outStream.RestartStrip();
			}

			float signedAngle( float3 vA, float3 vB, float3 vN){
				float d1 = acos(dot(vA, vB));
				float3 c1 = cross(vA, vB);
				if( dot(vN, c1) < 0 ){
					d1 = -d1;
				}
				return d1;
			}

			[maxvertexcount(21)]
			void geom( triangle appdata input[3], uint pid : SV_PrimitiveID, inout TriangleStream<pixelIn> outStream )
			{
				AdjacencyInfo adjacent = _AdjacencyBuffer[pid];
				
				float edgeSize = _EdgeSize;

				float3 normalTrian = getNormal(input[0].pos.xyz, input[1].pos.xyz, input[2].pos.xyz);
				float3 viewDirect = normalize( (input[0].pos.xyz + input[1].pos.xyz + input[2].pos.xyz)/3 - UNITY_MATRIX_IT_MV[3].xyz);

				half2 lineStatus[3];
				for(int i = 0; i < 3; i++){
					lineStatus[i] = 0;
				}

				//[branch]
				//if(  ){
					[loop]
					for( int i = 0; i < 3; i+=1){
						int auxIndex = (i+1)%3;
						if(input[auxIndex].pos.z < -99999){
							continue;
						}
						float3 auxNormal = getNormal(input[i].pos.xyz, adjacent.pos[i].xyz, input[auxIndex].pos.xyz);
						float3 auxDirect = normalize( (input[i].pos.xyz + adjacent.pos[i].xyz + input[auxIndex].pos.xyz)/3 - UNITY_MATRIX_IT_MV[3].xyz);
						
						float d1 = signedAngle(normalTrian, auxNormal, normalize(input[i].pos - input[auxIndex].pos));
						bool silhouetteFlag = (dot(normalTrian, viewDirect) < 0) && (dot(auxNormal, auxDirect) >= 0.0f);
						bool ridgeFlag = d1 < _RidgeTreshold;
						bool valleyFlag = d1 > _ValleyTreshold;

						if(ridgeFlag){
							lineStatus[i][0] = 1;
							lineStatus[auxIndex][0] = 1;
						}

						if(valleyFlag){
							lineStatus[i][1] = 1;
							lineStatus[auxIndex][1] = 1;
						}


						if(  valleyFlag || ridgeFlag){
							// we have a silhouette edge!
							//transform position to screen space
							// polorzenie wierzcholka 1 w screen-sapce
							float4 transPos1 = UnityObjectToClipPos(input[i].pos);
							float o1 = transPos1.w;
							transPos1 = transPos1/transPos1.w;

							// polorzenie wierzcholka 2 w screen-sapce
							float4 transPos2 = UnityObjectToClipPos(input[auxIndex].pos);
							float o2 = transPos2.w;
							transPos2 = transPos2/transPos2.w;

							// calculate edge direction in screen space
							float2 edgeDirection = normalize(transPos1.xy - transPos2.xy);

							//extrude vector in screen space
							float4 extrudeDirection = float4(normalize( float2(-edgeDirection.y, edgeDirection.x)), 0, 0);
							float4 normExtrude1 = UnityObjectToClipPos(input[i].pos + adjacent.nrm[i]);
							normExtrude1 = normExtrude1 / normExtrude1.w;
							normExtrude1 = normExtrude1 - transPos1;
							normExtrude1 = float4(normalize(normExtrude1.xy),0.0f ,0.0f);

							float4 normExtrude2 = UnityObjectToClipPos(input[auxIndex].pos + adjacent.nrm[auxIndex] );
							normExtrude2 = normExtrude2 / normExtrude2.w;
							normExtrude2 = normExtrude2 - transPos2;
							normExtrude2 = float4(normalize(normExtrude2.xy),0.0f ,0.0f);

							//Scale the extrude directions with the edge size.
							normExtrude1 = normExtrude1 * edgeSize;
							normExtrude2 = normExtrude2 * edgeSize;
							extrudeDirection = extrudeDirection * edgeSize;

							//Calculate the extruded vertices .
							float4 normVertex1 = transPos1 + normExtrude1;
							float4 extruVertex1 = transPos1 + extrudeDirection;
							float4 normVertex2 = transPos2 + normExtrude2;
							float4 extruVertex2 = transPos2 + extrudeDirection;

							pixelIn outVert[6];
							outVert[0].pos = float4(normVertex1.xyz*o1, o1); 
							outVert[1].pos = float4(extruVertex1.xyz*o1, o1); //(v0 + e)
							outVert[2].pos = float4(transPos1.xyz*o1, o1); // v0
							outVert[3].pos = float4(normVertex2.xyz*o2, o2);  
							outVert[4].pos = float4(extruVertex2.xyz*o2, o2); // v1 + e
							outVert[5].pos = float4(transPos2.xyz*o2, o2); //v1

							/////// UVS For extruded vertices

							for(int k = 0; k < 6; k++){
								outVert[k].nrm = float3(1,0,0); //todo
								outVert[k].lineStatus = 0;
								outVert[k].barycentricCoords = 0;

								outVert[k].pos.z *= _LineClosingFactor; // TODO do innych shaderow tego uzyj
								if(valleyFlag){
									outVert[k].triangleType = 1;
								}else{
									outVert[k].triangleType = 2;
								}
							}

#ifdef FINS							
							outStream.Append(outVert[0]);
							outStream.Append(outVert[1]);
							outStream.Append(outVert[2]);
							outStream.Append(outVert[4]);
							outStream.Append(outVert[5]);
							outStream.Append(outVert[3]);

							outStream.RestartStrip();
#endif							
						}

					}
				//}

				pixelIn v[3];
				v[0].barycentricCoords = float2(1,0);
				v[1].barycentricCoords = float2(0,1);
				v[2].barycentricCoords = float2(0,0);
				for(int i = 0; i < 3; i++){
					v[i].pos = UnityObjectToClipPos(input[i].pos);
					v[i].nrm =normalize(mul(float4(input[i].nrm, 0.0), unity_WorldToObject).xyz);
					v[i].triangleType = 0;
					v[i].lineStatus = lineStatus[i]; 
					
					outStream.Append(v[i]); 
				}
				outStream.RestartStrip();
			}

			fixed4 frag (pixelIn i) : SV_Target
			{
				const float PI = 3.141592653589793238462;

				float3 barys;
				barys.xy = i.barycentricCoords;
				barys.z = 1 - barys.x - barys.y;
				float3 deltas = fwidth(barys);
				barys = step(deltas, barys);
				float minBary = min(barys.x, min(barys.y, barys.z));

				fixed4 color = 1;
				if(i.triangleType == 1 ){
					color = float4(1,0,0,1);
				}else if (i.triangleType == 2){
					color = float4(0,1,0,1);
				}

				float margin = 0.3;
				float margin2 = 0.1;

				if(minBary <= 0){
					if(i.lineStatus[0] > (1-margin2)){ // todo remove arrows!
						color.b = 0;
					}
					if(i.lineStatus[1] > (1-margin2)){
						color.g = 0;
					}
				}

				return color;
			}
			ENDCG
		}
	}
}
