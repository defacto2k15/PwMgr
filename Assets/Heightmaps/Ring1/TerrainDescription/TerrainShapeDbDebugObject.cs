using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ComputeShaders;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.MeshGeneration;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public class TerrainShapeDbDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ContainerGameObject;
        private UTTextureRendererProxy _utTextureRendererProxy;
        private CommonExecutorUTProxy _commonExecutorUtProxy;

        private TerrainCardinalResolution _terrainResolution = TerrainCardinalResolution.MID_RESOLUTION;

        private ConcurrentBag<GeneratedTerrainElements> _generatedElements =
            new ConcurrentBag<GeneratedTerrainElements>();

        private UnityThreadComputeShaderExecutorObject _computeShaderExecutorObject;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(true);
            _computeShaderExecutorObject = new UnityThreadComputeShaderExecutorObject();
            _commonExecutorUtProxy = new CommonExecutorUTProxy();
            _utTextureRendererProxy = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(ContainerGameObject), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(500, 500)
                }));

            var rgbaMainTexture = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\n49_e019_1arc_v3.png", 3600,
                3600,
                TextureFormat.ARGB32, true, false);


            TerrainTextureFormatTransformator transformator =
                new TerrainTextureFormatTransformator(_commonExecutorUtProxy);
            transformator.EncodedHeightTextureToPlainAsync(new TextureWithSize()
            {
                Size = new IntVector2(3600, 3600),
                Texture = rgbaMainTexture
            }).ContinueWith(x =>
            {
                var mainTexture = x.Result;
                TerrainDetailGenerator generator = CreateTerrainDetailGenerator(new TextureWithSize()
                {
                    Size = new IntVector2(3600, 3600),
                    Texture = mainTexture
                });
                TerrainDetailProvider terrainDetailProvider = CreateTerrainDetailProvider(generator);
                var db = new TerrainShapeDb(
                    new CachedTerrainDetailProvider(
                        terrainDetailProvider,
                        () => new TerrainDetailElementsCache(_commonExecutorUtProxy,
                            new TerrainDetailElementCacheConfiguration())),
                    new TerrainDetailAlignmentCalculator(240));


                MyRectangle queryArea = null;
                if (_terrainResolution == TerrainCardinalResolution.MIN_RESOLUTION)
                {
                    queryArea = new MyRectangle(0, 0, 24 * 240, 24 * 240);
                }
                else if (_terrainResolution == TerrainCardinalResolution.MID_RESOLUTION)
                {
                    queryArea = new MyRectangle(3 * 240, 3 * 240, 3 * 240, 3 * 240);
                }
                else
                {
                    queryArea =
                        new MyRectangle(5 * 0.375f * 240, 5 * 0.375f * 240, 0.375f * 240, 0.375f * 240);
                }


                var outputOfGeneration = db.QueryAsync(new TerrainDescriptionQuery()
                {
                    QueryArea = queryArea,
                    RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                    {
                        new TerrainDescriptionQueryElementDetail()
                        {
                            Resolution = _terrainResolution.LowerResolution,
                            Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                        },
                        new TerrainDescriptionQueryElementDetail()
                        {
                            Resolution = _terrainResolution,
                            Type = TerrainDescriptionElementTypeEnum.NORMAL_ARRAY
                        },
                    }
                });

                outputOfGeneration.ContinueWith(c =>
                {
                    GeneratedTerrainElements elem = new GeneratedTerrainElements();
                    elem.HeightElement = c.Result.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);
                    elem.NormalElement = c.Result.GetElementOfType(TerrainDescriptionElementTypeEnum.NORMAL_ARRAY);
                    _generatedElements.Add(elem);
                }).ContinueWith(q =>
                    {
                        Debug.Log("Error: Executing task");
                        Debug.Log("Error descr is " + q.Exception);
                    }, TaskContinuationOptions.OnlyOnFaulted
                );
            });
        }

        public TerrainDetailGenerator CreateTerrainDetailGenerator(TextureWithSize mainTexture)
        {
            var featureAppliers =
                TerrainDetailProviderDebugUtils.CreateFeatureAppliers(_utTextureRendererProxy, ContainerGameObject,
                    _commonExecutorUtProxy, _computeShaderExecutorObject);


            TerrainDetailGeneratorConfiguration generatorConfiguration = new TerrainDetailGeneratorConfiguration()
            {
                TerrainDetailImageSideDisjointResolution = 240
            };
            TextureWithCoords fullFundationData = new TextureWithCoords(new TextureWithSize()
            {
                Texture = mainTexture.Texture,
                Size = new IntVector2(mainTexture.Size.X, mainTexture.Size.Y)
            }, new MyRectangle(0, 0, 3601 * 24, 3601 * 24));

            TerrainDetailGenerator generator =
                new TerrainDetailGenerator(generatorConfiguration, _utTextureRendererProxy, fullFundationData,
                    featureAppliers, _commonExecutorUtProxy);
            return generator;
        }

        public TerrainDetailProvider CreateTerrainDetailProvider(TerrainDetailGenerator generator)
        {
            var terrainDetailProviderConfiguration = new TerrainDetailProviderConfiguration()
            {
                UseTextureSavingToDisk = false
            };
            var terrainDetailFileManager = new TerrainDetailFileManager("C:\\unityCache\\", _commonExecutorUtProxy);

            var provider =
                new TerrainDetailProvider(terrainDetailProviderConfiguration, terrainDetailFileManager, generator, null, new TerrainDetailAlignmentCalculator(240));
            return provider;
        }

        public void Update()
        {
            _utTextureRendererProxy.Update();
            _commonExecutorUtProxy.Update();
            _computeShaderExecutorObject.Update();

            GeneratedTerrainElements generatedElement;
            if (_generatedElements.TryTake(out generatedElement))
            {
                var heightElement = generatedElement.HeightElement;
                var normalElement = generatedElement.NormalElement;

                var gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var material = new Material(Shader.Find("Custom/Terrain/Ring1"));
                material.SetTexture("_HeightmapTex", heightElement.TokenizedElement.DetailElement.Texture.Texture);
                material.SetVector("_HeightmapUv", heightElement.UvBase.ToVector4());

                material.SetTexture("_NormalmapTex", normalElement.TokenizedElement.DetailElement.Texture.Texture);
                material.SetVector("_NormalmapUv", normalElement.UvBase.ToVector4());

                gameObject.GetComponent<MeshRenderer>().material = material;
                gameObject.name = "Terrain";
                gameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);

                var unitySideLength = 10f;
                var realSideLength = 240 * 24;
                var metersPerUnit = realSideLength / unitySideLength;

                if (_terrainResolution == TerrainCardinalResolution.MIN_RESOLUTION)
                {
                    gameObject.transform.localScale = new Vector3(10, (6500 / metersPerUnit), 10);
                    gameObject.transform.localPosition = new Vector3(0, 0, 0);
                }
                else if (_terrainResolution == TerrainCardinalResolution.MID_RESOLUTION)
                {
                    gameObject.transform.localScale = new Vector3(10, (6500 / metersPerUnit) * 8, 10);
                    gameObject.transform.localPosition = new Vector3(0, 0, 0);
                }
                else
                {
                    gameObject.transform.localScale = new Vector3(10, (6500 / metersPerUnit) * 8 * 8, 10);
                    gameObject.transform.localPosition = new Vector3(0, -30, 0);
                }
                gameObject.GetComponent<MeshFilter>().mesh = PlaneGenerator.CreateFlatPlaneMesh(240, 240);
            }
        }

        private class GeneratedTerrainElements
        {
            public TerrainDetailElementOutput HeightElement { get; set; }
            public TerrainDetailElementOutput NormalElement { get; set; }
        }
    }
}