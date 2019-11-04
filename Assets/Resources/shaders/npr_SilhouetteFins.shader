Shader "Custom/NPR/SilhouetteFins" {
    Properties {
		_MainTex("MainTex", 2D)  = "white" {}
		_Param1("Param1", Range(-1,1)) = 0
		_TextureKParam("TextureKParam", Range(0,1)) = 1
		_TextureScale("TextureScale", Range(0,10)) = 1
		_EdgeSize("EdgeSize", Range(0,1)) = 0.01
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
				float3 uv : TEXCOORD0;
				float3 nrm : TEXCOORD1; 
			};

			struct AdjacencyInfo{
				float3 pos[3];
				float3 nrm[3];
			};

			StructuredBuffer<AdjacencyInfo> _AdjacencyBuffer;

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4 _LightColor0;

			float _Param1;
			float _EdgeSize;
			float _TextureKParam;
			float _TextureScale;
			
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

			[maxvertexcount(21)]
			void geom( triangle appdata input[3], uint pid : SV_PrimitiveID, inout TriangleStream<pixelIn> outStream )
			{
				AdjacencyInfo adjacent = _AdjacencyBuffer[pid];
				
				float edgeSize = _EdgeSize;

				pixelIn v[3];
				float special[3]; //todo remove
				special[0] = 0;
				special[1] = 0;
				special[2] = 0;

				float3 normalTrian = getNormal(input[0].pos.xyz, input[1].pos.xyz, input[2].pos.xyz);
				float3 viewDirect = normalize( (input[0].pos.xyz + input[1].pos.xyz + input[2].pos.xyz)/3 - UNITY_MATRIX_IT_MV[3].xyz);

				[branch]
				if(dot(normalTrian, viewDirect) < 0  ){
					[loop]
					for( int i = 0; i < 3; i+=1){
						int auxIndex = (i+1)%3;
						if(input[auxIndex].pos.z < -99999){
							continue;
						}
						float3 auxNormal = getNormal(input[i].pos.xyz, adjacent.pos[i].xyz, input[auxIndex].pos.xyz);
						float3 auxDirect = normalize( (input[i].pos.xyz + adjacent.pos[i].xyz + input[auxIndex].pos.xyz)/3 - UNITY_MATRIX_IT_MV[3].xyz);
					
						if(dot(auxNormal, auxDirect) >= 0.0f ){
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

							float4 normExtrude2 = UnityObjectToClipPos(input[auxIndex ].pos + adjacent.nrm[auxIndex] );
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
							half da = distance(normVertex1.xy, extruVertex1.xy);
							half db = distance(extruVertex1.xy, extruVertex2.xy);
							half dc = distance(extruVertex2.xy, normVertex2.xy);
							half d = distance(transPos1.xy, transPos2.xy);

							float2 e1 = 0;
							e1.x = transPos1.x + (d * ((d*da)/(da+db+dc)));
							e1.y = transPos1.y + (d * ((d*da)/(da+db+dc)));

							float2 e2 = 0;
							e2.x = transPos2.x - (d * ((d*dc)/(da+db+dc)));
							e2.y = transPos2.y - (d * ((d*dc)/(da+db+dc)));

							for(int k = 0; k < 6; k++){
								outVert[k].nrm = 0;
							}
							outVert[0].uv = float3(transPos1.xy, 1);
							outVert[1].uv = float3(e1, 1);
							outVert[2].uv = float3(transPos1.xy, 0);
							outVert[3].uv = float3(transPos2.xy, 1);
							outVert[4].uv = float3(e2, 1);
							outVert[5].uv = float3(transPos2.xy, 0);


							outStream.Append(outVert[0]);
							outStream.Append(outVert[1]);
							outStream.Append(outVert[2]);
							outStream.Append(outVert[4]);
							outStream.Append(outVert[5]);
							outStream.Append(outVert[3]);

							outStream.RestartStrip();
						}
					}
				}

				for(int i = 0; i < 3; i++){
					v[i].pos = UnityObjectToClipPos(input[i].pos);
					v[i].uv =  special[i];//max(special[i], special[(i+2)%3]); //input[i].uv;
					v[i].nrm = input[i].nrm; //todo

					float4 origin = UnityObjectToClipPos(float3(0,0,0));
					float4 ax = v[i].pos / v[i].pos.w;
					float4 bx = origin / origin.w;
					v[i].uv = distance(ax.xy, bx.xy);
					v[i].uv = 0;
					
					outStream.Append(v[i]); 
				}
				outStream.RestartStrip();
			}

			fixed4 frag (pixelIn i) : SV_Target
			{
				const float PI = 3.141592653589793238462;
				//fixed4 color = tex2D(_MainTex, i.texcoord);
				fixed4 color = i.uv.xxxx;// float4(_AdjacencyBuffer[0].position[2], 1);
				

				float2 coord = float2(0, i.uv.z);

				float4 center = UnityObjectToClipPos(float3(0,0,0));
				center /= center.w;

				float2 vect = i.uv.xy - center.xy;

				float angle = atan(vect.y/vect.x);
				angle = (vect.x < 0 ) ? 
					angle + PI :
						(vect.y < 0 ) ?
						angle+(2*PI) :
						angle;

				float lengthPer = _TextureKParam;
				coord.x = ((angle/(2*PI)) + (length(vect)*lengthPer)) * _TextureScale;

				color = tex2D(_MainTex, coord);
				//return float4(coord.x,0, 0, 1);
				return color;
			}
			ENDCG
		}
	}
}