using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.Utils;
using Assets.Utils.ArrayUtils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging
{
    public class TerrainDetailCornerMergerDEO : MonoBehaviour
    {
        private TerrainDetailCornerMerger _merger;
        private MockBaseTerrainDetailProvider _mockProvider = new MockBaseTerrainDetailProvider();


        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            TextureRendererServiceConfiguration textureRendererServiceConfiguration = new TextureRendererServiceConfiguration();
            textureRendererServiceConfiguration.StepSize = new Vector2(10,10);

            UTTextureRendererProxy utTextureRenderer = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(Object.FindObjectOfType<ComputeShaderContainerGameObject>()), textureRendererServiceConfiguration ));

            TextureConcieverUTProxy utTextureConciever = new TextureConcieverUTProxy();

            LateAssignFactory<BaseTerrainDetailProvider> terrainDetailProviderFactory = new LateAssignFactory<BaseTerrainDetailProvider>(() => _mockProvider);
            _merger = new TerrainDetailCornerMerger(terrainDetailProviderFactory, new TerrainDetailAlignmentCalculator(240), utTextureRenderer, utTextureConciever );

            //TaskUtils.DebuggerAwareWait(Test1());
            //TaskUtils.DebuggerAwareWait(Test2());
            //TaskUtils.DebuggerAwareWait(Test3());
            //TaskUtils.DebuggerAwareWait(Test4());
            //TaskUtils.DebuggerAwareWait(Test5());
            //TaskUtils.DebuggerAwareWait(Test6());
            //TaskUtils.DebuggerAwareWait(Test7());
            TaskUtils.DebuggerAwareWait(Test8());
        }

        public async Task Test1()
        {
            CreateTerrainObject(CreateBaseTexture(0.4f, DebugTerrainCharacter.Volcano), new Vector2(0,0));
        }

        public async Task Test2() // merging with textures that have equal heights
        {
            _mockProvider.MockProvidingFunction =
                CreateSampleMockProvidingFunction((queryArea) => CreateBaseTexture(0.4f, DebugTerrainCharacter.Flat));
            var outTexture = await _merger.MergeHeightDetailCorners(
                new MyRectangle(240 * 4, 240 * 4, 240, 240), TerrainCardinalResolution.MID_RESOLUTION, CreateBaseTexture(0.4f, DebugTerrainCharacter.Flat));
            CreateTerrainObject(outTexture, new Vector2(0,0));
        }

        public async Task Test3() // merging that results in bottom-right corner merge
        {
            var ourBaseTexture = CreateBaseTexture(0.4f, DebugTerrainCharacter.Flat);
            var bottomBaseTexture = CreateBaseTexture(0.1f, DebugTerrainCharacter.Flat);

            _mockProvider.MockProvidingFunction = CreateSampleMockProvidingFunction((queryArea) =>
            {
                if (Math.Abs(queryArea.X - 90 * 4) < 0.0001f && Math.Abs(queryArea.Y - 90 * 3) < 0.0001f)
                {
                    return bottomBaseTexture;
                }
                else
                {
                    return ourBaseTexture;
                }
            });
            var outTexture = await _merger.MergeHeightDetailCorners( 
                new MyRectangle(90 * 4, 90 * 4, 90, 90), TerrainCardinalResolution.MAX_RESOLUTION, CreateBaseTexture(0.4f, DebugTerrainCharacter.Flat));
            CreateTerrainObject(outTexture, new Vector2(0,0));
            CreateTerrainObject(bottomBaseTexture, new Vector2(0,-90));
        }

        public async Task Test4() // BottomLeftMerging but only in one terrain
        {
            var ourBaseTexture = CreateBaseTexture(0.8f, DebugTerrainCharacter.Flat);

            var gridPositionDetailTextures = new Dictionary<IntVector2, TextureWithSize>
            {
                {new IntVector2(0, 0), CreateBaseTexture(0.1f, DebugTerrainCharacter.Flat)},
                {new IntVector2(1, 0), CreateBaseTexture(0.3f, DebugTerrainCharacter.Flat)},
                {new IntVector2(0, 1), CreateBaseTexture(0.5f, DebugTerrainCharacter.Flat)},
                {new IntVector2(1, 1), ourBaseTexture}
            };

            _mockProvider.MockProvidingFunction = CreateSampleMockProvidingFunction((queryArea) =>
            {
                foreach (var pair in gridPositionDetailTextures)
                {
                    var realPosition = new Vector2(pair.Key.X * 90f, pair.Key.Y * 90f);
                    if (Math.Abs(queryArea.X - realPosition.x) < 0.0001f && Math.Abs(queryArea.Y - realPosition.y) < 0.0001f)
                    {
                        return pair.Value;
                    }
                }
                Preconditions.Fail("E911 Failed to return detail in "+queryArea);
                return null;
            });
            var mergedTexture = await _merger.MergeHeightDetailCorners( new MyRectangle(90, 90, 90, 90), TerrainCardinalResolution.MAX_RESOLUTION, ourBaseTexture);
            gridPositionDetailTextures[new IntVector2(1, 1)] = mergedTexture;

            foreach (var pair in gridPositionDetailTextures)
            {
                CreateTerrainObject(pair.Value, pair.Key*90f);
            }
        }

        public async Task Test5() // Merging all corners of one detail
        {
            var ourBaseTexture = CreateBaseTexture(0.5f, DebugTerrainCharacter.Flat);

            var gridPositionDetailTextures = new Dictionary<IntVector2, TextureWithSize>
            {
                {new IntVector2(0, 0), CreateBaseTexture(0.1f, DebugTerrainCharacter.Flat)},
                {new IntVector2(1, 0), CreateBaseTexture(0.2f, DebugTerrainCharacter.Flat)},
                {new IntVector2(2, 0), CreateBaseTexture(0.3f, DebugTerrainCharacter.Flat)},
                {new IntVector2(0, 1), CreateBaseTexture(0.4f, DebugTerrainCharacter.Flat)},
                {new IntVector2(1, 1), ourBaseTexture},
                {new IntVector2(2, 1), CreateBaseTexture(0.6f, DebugTerrainCharacter.Flat)},
                {new IntVector2(0, 2), CreateBaseTexture(0.7f, DebugTerrainCharacter.Flat)},
                {new IntVector2(1, 2), CreateBaseTexture(0.8f, DebugTerrainCharacter.Flat)},
                {new IntVector2(2, 2), CreateBaseTexture(0.9f, DebugTerrainCharacter.Flat)},
            };

            _mockProvider.MockProvidingFunction =
                CreateGridBasedMockProvidingFunction(gridPositionDetailTextures, null);
            var mergedTexture = await _merger.MergeHeightDetailCorners( new MyRectangle(90, 90, 90, 90), TerrainCardinalResolution.MAX_RESOLUTION, ourBaseTexture);
            gridPositionDetailTextures[new IntVector2(1, 1)] = mergedTexture;

            foreach (var pair in gridPositionDetailTextures)
            {
                CreateTerrainObject(pair.Value, pair.Key*90f);
            }
        }

        public async Task Test6() // Merging corners of all details
        {
            var ourBaseTexture = CreateBaseTexture(0.5f, DebugTerrainCharacter.Flat);
            var defaultTexture = CreateBaseTexture(0f, DebugTerrainCharacter.Flat);

            var gridPositionDetailTextures = new Dictionary<IntVector2, TextureWithSize>
            {
                {new IntVector2(0, 0), CreateBaseTexture(0.1f, DebugTerrainCharacter.Flat)},
                {new IntVector2(1, 0), CreateBaseTexture(0.2f, DebugTerrainCharacter.Flat)},
                {new IntVector2(2, 0), CreateBaseTexture(0.3f, DebugTerrainCharacter.Flat)},
                {new IntVector2(0, 1), CreateBaseTexture(0.4f, DebugTerrainCharacter.Flat)},
                {new IntVector2(1, 1), ourBaseTexture},
                {new IntVector2(2, 1), CreateBaseTexture(0.6f, DebugTerrainCharacter.Flat)},
                {new IntVector2(0, 2), CreateBaseTexture(0.7f, DebugTerrainCharacter.Flat)},
                {new IntVector2(1, 2), CreateBaseTexture(0.8f, DebugTerrainCharacter.Flat)},
                {new IntVector2(2, 2), CreateBaseTexture(0.9f, DebugTerrainCharacter.Flat)},
            };

            _mockProvider.MockProvidingFunction = CreateGridBasedMockProvidingFunction(gridPositionDetailTextures, defaultTexture);

            var mergedDetailTextures = new Dictionary<IntVector2, TextureWithSize>();
            foreach (var pair in gridPositionDetailTextures)
            {
                mergedDetailTextures[pair.Key] =  await 
                    _merger.MergeHeightDetailCorners( new MyRectangle(pair.Key.X*90f, pair.Key.Y*90f, 90, 90), TerrainCardinalResolution.MAX_RESOLUTION, pair.Value);
            }

            foreach (var pair in mergedDetailTextures)
            {
                CreateTerrainObject(pair.Value, pair.Key*90f);
            }
        }


        public async Task Test7() // Merging corners of all details but with noise
        {
            var ourBaseTexture = CreateBaseTexture(0.5f, DebugTerrainCharacter.Noise);
            var defaultTexture = CreateBaseTexture(0f, DebugTerrainCharacter.Flat);

            var gridPositionDetailTextures = new Dictionary<IntVector2, TextureWithSize>
            {
                {new IntVector2(0, 0), CreateBaseTexture(0.1f, DebugTerrainCharacter.Noise)},
                {new IntVector2(1, 0), CreateBaseTexture(0.2f, DebugTerrainCharacter.Noise)},
                {new IntVector2(2, 0), CreateBaseTexture(0.3f, DebugTerrainCharacter.Noise)},
                {new IntVector2(0, 1), CreateBaseTexture(0.4f, DebugTerrainCharacter.Noise)},
                {new IntVector2(1, 1), ourBaseTexture},
                {new IntVector2(2, 1), CreateBaseTexture(0.6f, DebugTerrainCharacter.Noise)},
                {new IntVector2(0, 2), CreateBaseTexture(0.7f, DebugTerrainCharacter.Noise)},
                {new IntVector2(1, 2), CreateBaseTexture(0.8f, DebugTerrainCharacter.Noise)},
                {new IntVector2(2, 2), CreateBaseTexture(0.9f, DebugTerrainCharacter.Noise)},
            };

            _mockProvider.MockProvidingFunction = CreateGridBasedMockProvidingFunction(gridPositionDetailTextures, defaultTexture);

            var mergedDetailTextures = new Dictionary<IntVector2, TextureWithSize>();
            foreach (var pair in gridPositionDetailTextures)
            {
                mergedDetailTextures[pair.Key] =  await 
                    _merger.MergeHeightDetailCorners( new MyRectangle(pair.Key.X*90f, pair.Key.Y*90f, 90, 90), TerrainCardinalResolution.MAX_RESOLUTION, pair.Value);
            }

            foreach (var pair in mergedDetailTextures)
            {
                CreateTerrainObject(pair.Value, pair.Key*90f);
            }
        }

        public async Task Test8() // Merging corners of all details where some are arleady merged
        {
            var ourBaseTexture = CreateBaseTexture(0.5f, DebugTerrainCharacter.Flat);
            var defaultDetail = new TextureWithMergingStatus()
            {
                MergeStatus = CornersMergeStatus.NOT_MERGED,
                Texture = CreateBaseTexture(0f, DebugTerrainCharacter.Flat)
            };

            var gridPositionDetailTextures = new Dictionary<IntVector2, TextureWithMergingStatus>
            {
                {new IntVector2(0, 0), new TextureWithMergingStatus() {MergeStatus = CornersMergeStatus.MERGED, Texture = CreateBaseTexture(0.1f, DebugTerrainCharacter.Flat)}},
                {new IntVector2(1, 0), new TextureWithMergingStatus() {MergeStatus = CornersMergeStatus.MERGED, Texture = CreateBaseTexture(0.1f, DebugTerrainCharacter.Flat)}},
                {new IntVector2(2, 0), new TextureWithMergingStatus() {MergeStatus = CornersMergeStatus.NOT_MERGED, Texture = CreateBaseTexture(0.3f, DebugTerrainCharacter.Flat)}},
                {new IntVector2(0, 1), new TextureWithMergingStatus() {MergeStatus = CornersMergeStatus.MERGED, Texture = CreateBaseTexture(0.1f, DebugTerrainCharacter.Flat)}},
                {new IntVector2(1, 1), new TextureWithMergingStatus() {MergeStatus = CornersMergeStatus.NOT_MERGED, Texture =  ourBaseTexture}},
                {new IntVector2(2, 1), new TextureWithMergingStatus() {MergeStatus = CornersMergeStatus.NOT_MERGED, Texture = CreateBaseTexture(0.6f, DebugTerrainCharacter.Flat)}},
                {new IntVector2(0, 2), new TextureWithMergingStatus() {MergeStatus = CornersMergeStatus.MERGED, Texture = CreateBaseTexture(0.1f, DebugTerrainCharacter.Flat)}},
                {new IntVector2(1, 2), new TextureWithMergingStatus() {MergeStatus = CornersMergeStatus.NOT_MERGED, Texture = CreateBaseTexture(0.8f, DebugTerrainCharacter.Flat)}},
                {new IntVector2(2, 2), new TextureWithMergingStatus() {MergeStatus = CornersMergeStatus.NOT_MERGED, Texture = CreateBaseTexture(0.9f, DebugTerrainCharacter.Flat)}},
            };

            _mockProvider.MockProvidingFunction =
                CreateGridBasedMockProvidingFunctionWithMergeStatus(gridPositionDetailTextures, defaultDetail);

            var mergedDetailTextures = new Dictionary<IntVector2, TextureWithSize>();
            foreach (var pair in gridPositionDetailTextures)
            {
                if (pair.Value.MergeStatus == CornersMergeStatus.NOT_MERGED)
                {
                    mergedDetailTextures[pair.Key] = await
                        _merger.MergeHeightDetailCorners(new MyRectangle(pair.Key.X * 90f, pair.Key.Y * 90f, 90, 90),
                            TerrainCardinalResolution.MAX_RESOLUTION, pair.Value.Texture);
                }
                else
                {
                    mergedDetailTextures[pair.Key] = pair.Value.Texture;
                }
            }

            foreach (var pair in mergedDetailTextures)
            {
                CreateTerrainObject(pair.Value, pair.Key*90f);
            }
        }

        private Func<TerrainDescriptionElementTypeEnum, MyRectangle, TerrainCardinalResolution, RequiredCornersMergeStatus, TerrainDetailElementOutput> 
             CreateGridBasedMockProvidingFunction( Dictionary<IntVector2, TextureWithSize> terrainDetailForGridElements, TextureWithSize defaultDetail)
        {
            var copyOfDictionary = terrainDetailForGridElements.ToDictionary(e => e.Key, e => e.Value);
            return CreateSampleMockProvidingFunction((queryArea) =>
            {
                foreach (var pair in copyOfDictionary)
                {
                    var realPosition = new Vector2(pair.Key.X * 90f, pair.Key.Y * 90f);
                    if (Math.Abs(queryArea.X - realPosition.x) < 0.0001f && Math.Abs(queryArea.Y - realPosition.y) < 0.0001f)
                    {
                        return pair.Value;
                    }
                }
                if (defaultDetail == null)
                {
                    Preconditions.Fail("E911 Failed to return detail in "+queryArea);
                }
                return defaultDetail;
            });
        }

        private Func<TerrainDescriptionElementTypeEnum, MyRectangle, TerrainCardinalResolution, RequiredCornersMergeStatus, TerrainDetailElementOutput> 
            CreateSampleMockProvidingFunction (Func<MyRectangle, TextureWithSize> simpleFunc)
        {
            return (type, queryArea, resolution, cornersMergeStatus) => new TerrainDetailElementOutput()
            {
                TokenizedElement = new TokenizedTerrainDetailElement()
                {
                    DetailElement = new TerrainDetailElement()
                    {
                        CornersMergeStatus = CornersMergeStatus.NOT_MERGED,
                        DetailArea = queryArea,
                        Resolution = resolution,
                        Texture = simpleFunc(queryArea)
                    },
                    Token = new TerrainDetailElementToken(queryArea,resolution,type, CornersMergeStatus.NOT_MERGED)
                },
                UvBase = new MyRectangle(0,0,1,1)
            };
        }

        private class TextureWithMergingStatus
        {
            public TextureWithSize Texture;
            public CornersMergeStatus MergeStatus;
        }

        private Func<TerrainDescriptionElementTypeEnum, MyRectangle, TerrainCardinalResolution, RequiredCornersMergeStatus, TerrainDetailElementOutput>
            CreateGridBasedMockProvidingFunctionWithMergeStatus( Dictionary<IntVector2,TextureWithMergingStatus> terrainDetailForGridElements, TextureWithMergingStatus defaultDetail)
        {
            var copyOfDictionary = terrainDetailForGridElements.ToDictionary(e => e.Key, e => e.Value);

            return (type, queryArea, resolution, cornersMergeStatus) =>
            {
                TextureWithMergingStatus outTextureWithMergeStatus = null;
                foreach (var pair in copyOfDictionary)
                {
                    var realPosition = new Vector2(pair.Key.X * 90f, pair.Key.Y * 90f);
                    if (Math.Abs(queryArea.X - realPosition.x) < 0.0001f && Math.Abs(queryArea.Y - realPosition.y) < 0.0001f)
                    {
                        outTextureWithMergeStatus = pair.Value;
                    }
                }
                if (outTextureWithMergeStatus == null)
                {
                    if (defaultDetail == null)
                    {
                        Preconditions.Fail("E911 Failed to return detail in " + queryArea);
                    }
                    else
                    {
                        outTextureWithMergeStatus = defaultDetail;
                    }
                }

                return new TerrainDetailElementOutput()
                {
                    TokenizedElement = new TokenizedTerrainDetailElement()
                    {
                        DetailElement = new TerrainDetailElement()
                        {
                            CornersMergeStatus = outTextureWithMergeStatus.MergeStatus,
                            DetailArea = queryArea,
                            Resolution = resolution,
                            Texture = outTextureWithMergeStatus.Texture
                        },
                        Token = new TerrainDetailElementToken(queryArea, resolution, type, CornersMergeStatus.NOT_MERGED)
                    },
                    UvBase = new MyRectangle(0, 0, 1, 1)
                };
            };
        }

        private TextureWithSize CreateBaseTexture(float height, DebugTerrainCharacter character)
        {
            if (character == DebugTerrainCharacter.Noise)
            {
                var texture = new RenderTexture(241, 241, 0, RenderTextureFormat.RFloat);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Point;
                
                var material = new Material(Shader.Find("Custom/Tool/FillHeightTextureWithRandomValues"));
                material.SetTexture("_HeightTexture", texture);
                Graphics.Blit(texture, (RenderTexture) texture, material);
                return new TextureWithSize()
                {
                    Texture = texture,
                    Size = new IntVector2(241, 241)
                };
            }
            else
            {
                float[,] heightArray;

                if (character == DebugTerrainCharacter.Flat)
                {
                    heightArray = MyArrayUtils.CreateFilled(241, 241, height);
                }
                else //if (character == DebugTerrainCharacter.Volcano)
                {
                    heightArray = new float[241, 241];
                    float minHeight = height * 0.2f;
                    float maxHeight = height;
                    for (int x = 0; x < 241; x++)
                    {
                        for (int y = 0; y < 241; y++)
                        {
                            heightArray[x, y] = Mathf.Lerp(minHeight, maxHeight,
                                1 - Vector2.Distance(new Vector2(x, y), new Vector2(120, 120)) / 200f);
                        }
                    }
                }
                var heightTex = HeightmapUtils.CreateTextureFromHeightmap(new HeightmapArray(heightArray));

                var transformator = new TerrainTextureFormatTransformator(new CommonExecutorUTProxy());
                var plainHeightTexture = transformator.EncodedHeightTextureToPlain(new TextureWithSize()
                {
                    Size = new IntVector2(241, 241),
                    Texture = heightTex
                });
                plainHeightTexture.wrapMode = TextureWrapMode.Clamp;
                return new TextureWithSize()
                {
                    Size = new IntVector2(241, 241),
                    Texture = plainHeightTexture
                };
            }
        }

        private enum DebugTerrainCharacter
        {
            Flat, Volcano, Noise
        }

        public void CreateTerrainObject(TextureWithSize textureWithSize, Vector2 startPosition)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.GetComponent<MeshFilter>().mesh = PlaneGenerator.CreateFlatPlaneMesh(241, 241);

            var material = new Material(Shader.Find("Custom/Debug/SimpleTerrain"));
            material.SetTexture("_HeightmapTex", textureWithSize.Texture);
            material.SetFloat("_HeightmapTexWidth", 241);

            go.GetComponent<MeshRenderer>().material = material;
            go.transform.localPosition = new Vector3(startPosition.x, 0, startPosition.y);
            go.transform.localScale = new Vector3(90,10,90);
            go.name = $"TerrainAt"+startPosition;
        }
    }

    public class MockBaseTerrainDetailProvider : BaseTerrainDetailProvider
    {
        private Func<TerrainDescriptionElementTypeEnum, MyRectangle, TerrainCardinalResolution,
            RequiredCornersMergeStatus, TerrainDetailElementOutput> _mockProvidingFunction;

        public Func<TerrainDescriptionElementTypeEnum, MyRectangle, TerrainCardinalResolution, RequiredCornersMergeStatus, TerrainDetailElementOutput> MockProvidingFunction
        {
            set { _mockProvidingFunction = value; }
        }

        public override Task<TerrainDetailElementOutput> RetriveTerrainDetailAsync(
            TerrainDescriptionElementTypeEnum type, MyRectangle queryArea, TerrainCardinalResolution resolution, RequiredCornersMergeStatus cornersMergeStatus)
        {
            return TaskUtils.MyFromResult(_mockProvidingFunction(type, queryArea, resolution, cornersMergeStatus));
        }

        public override Task RemoveTerrainDetailAsync(TerrainDetailElementToken token)
        {
            return TaskUtils.EmptyCompleted();
        }
    }
}