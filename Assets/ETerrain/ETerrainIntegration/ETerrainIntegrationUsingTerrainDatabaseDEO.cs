using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.SectorFilling;
using Assets.ETerrain.TestUtils;
using Assets.ETerrain.Tools.HeightPyramidExplorer;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Repositioning;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using UnityEditor;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
{
    public class ETerrainIntegrationUsingTerrainDatabaseDEO : MonoBehaviour
    {
        public GameObject Traveller;
        private ETerrainHeightPyramidFacade _eTerrainHeightPyramidFacade;


        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            ComputeShaderContainerGameObject containerGameObject = GameObject.FindObjectOfType<ComputeShaderContainerGameObject>();
            UTTextureRendererProxy textureRendererProxy = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(containerGameObject), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(400, 400)
                }));
            var meshGeneratorUtProxy = new MeshGeneratorUTProxy(new MeshGeneratorService());

            var startConfiguration = ETerrainHeightPyramidFacadeStartConfiguration.DefaultConfiguration;
            startConfiguration.CommonConfiguration.YScale = 1;
            startConfiguration.CommonConfiguration.InterSegmentMarginSize = 0;
            startConfiguration.InitialTravellerPosition = new Vector2(490, -21);
            startConfiguration.HeightPyramidLevels = new List<HeightPyramidLevel>() { HeightPyramidLevel.Top/*, HeightPyramidLevel.Mid, HeightPyramidLevel.Bottom*/};

            ETerrainHeightBuffersManager buffersManager = new ETerrainHeightBuffersManager();
            _eTerrainHeightPyramidFacade = new ETerrainHeightPyramidFacade(buffersManager,meshGeneratorUtProxy, textureRendererProxy, startConfiguration);

            var perLevelTemplates = _eTerrainHeightPyramidFacade.GenerateLevelTemplates();
            var levels = startConfiguration.PerLevelConfigurations.Keys.Where(c=> startConfiguration.HeightPyramidLevels.Contains(c));
            buffersManager.InitializeBuffers(levels.ToDictionary(c => c, c => new EPyramidShaderBuffersGeneratorPerRingInput()
            {
                CeilTextureResolution = startConfiguration.CommonConfiguration.CeilTextureSize.X,  //TODO i use only X, - works only for squares
                HeightMergeRanges = perLevelTemplates[c].LevelTemplate.PerRingTemplates.ToDictionary(k => k.Key, k => k.Value.HeightMergeRange),
                PyramidLevelWorldSize = startConfiguration.PerLevelConfigurations[c].PyramidLevelWorldSize.Width,  // TODO works only for square pyramids - i use width
                RingUvRanges = startConfiguration.CommonConfiguration.RingsUvRange
            }),startConfiguration.CommonConfiguration.MaxLevelsCount, startConfiguration.CommonConfiguration.MaxRingsPerLevelCount);


            var repositioner = Repositioner.Default;
            var shapeDbUnderTest = new TerrainShapeDbUnderTest(useCornerMerging:false, useTextureSavingToDisk:true, useTextureLoadingFromDisk:true);

            int offset = 0;
            //TerrainDetailElementOutput terrainDetailElementOutput = null;
            _eTerrainHeightPyramidFacade.Start(perLevelTemplates,
                new Dictionary<EGroundTextureType, OneGroundTypeLevelTextureEntitiesGenerator>
                {
                    {
                        EGroundTextureType.HeightMap, new OneGroundTypeLevelTextureEntitiesGenerator
                        {
                            LambdaSegmentFillingListenerGenerator =
                                (level, segmentModificationManager) =>
                                {
                                    return new LambdaSegmentFillingListener(
                                        c =>
                                        {
                                            var segmentLength = startConfiguration.PerLevelConfigurations[level].BiggestShapeObjectInGroupLength;
                                            var sap = c.SegmentAlignedPosition;
                                            var surfaceWorldSpaceRectangle = new MyRectangle(sap.X * segmentLength, sap.Y * segmentLength, segmentLength,
                                                segmentLength);

                                            var repositionedRectangle = repositioner.InvMove(surfaceWorldSpaceRectangle);
                                            //if (terrainDetailElementOutput == null)
                                            //{
                                            var terrainDetailElementOutput = shapeDbUnderTest.ShapeDb.QueryAsync(new TerrainDescriptionQuery
                                            {
                                                QueryArea = repositionedRectangle,
                                                RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>
                                                {
                                                    new TerrainDescriptionQueryElementDetail
                                                    {
                                                        Resolution =  TerrainCardinalResolution.MAX_RESOLUTION,//ETerrainUtils.HeightPyramidLevelToTerrainShapeDatabaseResolution(level),
                                                        RequiredMergeStatus = RequiredCornersMergeStatus.NOT_IMPORTANT,
                                                        Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                                                    }
                                                }
                                            }).Result.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);
                                            //}

                                            var segmentTexture = terrainDetailElementOutput.TokenizedElement.DetailElement.Texture.Texture;
                                            Debug.Log("Element: " + terrainDetailElementOutput.UvBase);

                                            //if ((repositionedRectangle.DownLeftPoint - new Vector2(47250, 52110)).magnitude > 20)
                                            //{
                                            //segmentTexture = CreateDummySegmentTexture2(c, level, 0.5f, 0f);

                                            //    Debug.Log("B44 "+(surfaceWorldSpaceRectangle.DownLeftPoint - new Vector2(47250, 52110)).magnitude);
                                            //}
                                            //else
                                            //{
                                            //    Debug.Log("A22");
                                            //}
                                            //var segmentTexture = CreateDummySegmentTextureByWorldPosition(surfaceWorldSpaceRectangle, level);
                                            segmentModificationManager.AddSegment(segmentTexture, c.SegmentAlignedPosition);
                                        },
                                        c => { },
                                        c => { });
                                },
                            CeilTextureGenerator = () => EGroundTextureGenerator.GenerateEmptyGroundTexture(
                                startConfiguration.CommonConfiguration.CeilTextureSize, startConfiguration.CommonConfiguration.HeightTextureFormat),
                            SegmentPlacerGenerator = ceilTexture =>
                            {
                                var modifiedCornerBuffer =
                                    EGroundTextureGenerator.GenerateModifiedCornerBuffer(startConfiguration.CommonConfiguration.SegmentTextureResolution,
                                        startConfiguration.CommonConfiguration.HeightTextureFormat);

                                return new HeightSegmentPlacer(textureRendererProxy, ceilTexture, startConfiguration.CommonConfiguration.SlotMapSize,
                                    startConfiguration.CommonConfiguration.CeilTextureSize, startConfiguration.CommonConfiguration.InterSegmentMarginSize,
                                    modifiedCornerBuffer);
                            }
                        }
                    }
                }
            );

            Traveller.transform.position = new Vector3(startConfiguration.InitialTravellerPosition.x, 0, startConfiguration.InitialTravellerPosition.y);
            //_eTerrainHeightPyramidFacade.DisableLevelShapes(HeightPyramidLevel.Top);
            //_eTerrainHeightPyramidFacade.DisableLevelShapes(HeightPyramidLevel.Mid);
            //_eTerrainHeightPyramidFacade.SetShapeRootTransform(new MyTransformTriplet(new Vector3(0, -240, 0), Quaternion.identity, new Vector3(1, 20, 1)));
        }

        public static Texture CreateDummySegmentTexture2(SegmentInformation segmentInformation, HeightPyramidLevel level, float offset, float stepMultiplier)
        {
            var tex = new Texture2D(240, 240, TextureFormat.RFloat, false);
            float[] rawTextureData = new float[tex.width * tex.height];
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    rawTextureData[x + y * tex.width] = offset+ ((x+y*4) / 400f)*stepMultiplier;
                }
            }

            tex.LoadRawTextureData(CastUtils.ConvertFloatArrayToByte(rawTextureData));

            tex.Apply();
            return tex;
        }

        public static Texture CreateDummySegmentTextureByWorldPosition(MyRectangle worldSpaceRectangle, HeightPyramidLevel level)
        {
            var tex = new Texture2D(240, 240, TextureFormat.RFloat, false);
            float[] rawTextureData = new float[tex.width * tex.height];
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    var uvInTexture = new Vector2(x/240f, y/240f);
                    var worldSpacePosition = worldSpaceRectangle.SampleByUv(uvInTexture);
                    rawTextureData[x + y * tex.width] = Mathf.Repeat(worldSpacePosition.x, 100) / 100.0f;
                }
            }

            tex.LoadRawTextureData(CastUtils.ConvertFloatArrayToByte(rawTextureData));

            tex.Apply();
            return tex;
        }

        public void Update()
        {
            var position3D = Traveller.transform.position;
            var flatPosition = new Vector2(position3D.x, position3D.z);

            _eTerrainHeightPyramidFacade.Update(flatPosition);

            if (Time.frameCount > 10)
            {
                //EditorApplication.isPaused = true;
            }
        }
    }
}
