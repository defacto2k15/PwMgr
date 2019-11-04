using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Repositioning;
using Assets.Ring2;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.Db;
using Assets.Ring2.Devising;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.Painting;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Ring2.Stamping;
using Assets.ShaderUtils;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.ESurface
{
    public class ESurfaceSinglePatchDebugGO : MonoBehaviour
    {
        public Vector4 PatchArea;
        public GameObject DummyPlate1;
        public GameObject DummyPlate2;
        public bool GenerateDebugGrid;
        private ESurfacePatchProvider _provider;
        private UpdatableContainer _updatableContainer;
        private GRing2PatchesCreatorProxy _patchesCreatorProxy;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var shaderContainerGO = FindObjectOfType<ComputeShaderContainerGameObject>();

            _updatableContainer = new UpdatableContainer();
            Dictionary<int, float> intensityPatternPixelsPerUnit = new Dictionary<int, float>()
            {
                {1,1 }
            };
            int mipmapLevelToExtract = 2;
            Dictionary<int, float> plateStampPixelsPerUnit = new Dictionary<int, float>()
            {
                {1, 5 }
            };
            _provider = ESurfaceProviderInitializationHelper.ConstructProvider(
                _updatableContainer, intensityPatternPixelsPerUnit, shaderContainerGO, mipmapLevelToExtract, plateStampPixelsPerUnit);

            _patchesCreatorProxy =  new GRing2PatchesCreatorProxy(ESurfaceProviderInitializationHelper.CreateRing2PatchesCreator(_updatableContainer, intensityPatternPixelsPerUnit));

            RegeneratePatch();
        }

        public void RegeneratePatch()
        {
            if (GenerateDebugGrid)
            {
                var segmentLength = 90;
                for (int x = -3; x < 3; x++)
                {
                    for (int y = -3; y < 3; y++)
                    {
                        var inGamePosition = new MyRectangle(x*segmentLength, y*segmentLength, segmentLength, segmentLength);
                        var flatLod = new FlatLod(1, 0);
                        var detailPack = _provider.ProvideSurfaceDetail(inGamePosition, flatLod);

                        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        quad.transform.rotation = Quaternion.Euler(90,0,0);
                        quad.transform.localScale = new Vector3(segmentLength,segmentLength,1);
                        quad.transform.localPosition = new Vector3(inGamePosition.X + inGamePosition.Width / 2,1, inGamePosition.Y + inGamePosition.Height / 2);
                        var a = quad.GetComponent<MeshRenderer>();
                        var b = a.GetComponent<MeshRenderer>();
                        var c = b.material;
                        if (detailPack != null)
                        {
                            c.mainTexture = detailPack.MainTexture;
                        }
                    }
                }
            }
            else
            {
                MyRectangle inGamePosition = new MyRectangle(PatchArea.x, PatchArea.y, PatchArea.z - PatchArea.x, PatchArea.w - PatchArea.y);
                var flatLod = new FlatLod(1, 0);

                var detailPack = _provider.ProvideSurfaceDetail(inGamePosition, flatLod);
                DummyPlate1.GetComponent<MeshRenderer>().material.mainTexture = detailPack.MainTexture;
                //DummyPlate2.GetComponent<MeshRenderer>().material.SetTexture("_BumpMap", detailPack.NormalTexture);

                //var devisedPatches = _patchesCreatorProxy.CreatePatchAsync(inGamePosition.ToRectangle(), flatLod.ScalarValue).Result;
                //var onlyPatch = devisedPatches.First();

                //var repositioner = Repositioner.Default; //todo
                //var templates = new List<Ring2PlateStampTemplate>();
                //foreach (var sliceInfo in onlyPatch.SliceInfos)
                //{
                //    var propertyBlock = sliceInfo.Uniforms.ToPropertyBlockTemplate();

                //    templates.Add(new Ring2PlateStampTemplate( new MaterialTemplate(
                //            Ring2ShaderNames.RuntimeTerrainTexture, sliceInfo.Keywords, propertyBlock), repositioner.Move(onlyPatch.SliceArea), flatLod.ScalarValue));
                //}

                //var material = Ring2PlateStamper_CreateRenderMaterial(templates.First());
                //material.EnableKeyword("GENERATE_COLOR");
                //DummyPlate2.GetComponent<MeshRenderer>().material = material;
            }
        }

        private Material Ring2PlateStamper_CreateRenderMaterial(Ring2PlateStampTemplate template)
        {
            var renderMaterial = new Material(Shader.Find("Custom/Terrain/Ring2Stamper"));
            foreach (var keyword in template.MaterialTemplate.KeywordSet.Keywords)
            {
                renderMaterial.EnableKeyword(keyword);
            }
            template.MaterialTemplate.PropertyBlock.FillMaterial(renderMaterial);

            renderMaterial.SetVector("_Coords",
                new Vector4(
                    template.PlateCoords.X,
                    template.PlateCoords.Y,
                    template.PlateCoords.Width,
                    template.PlateCoords.Height
                ));
            return renderMaterial;
        }


        private GRing2PatchesCreator CreateRing2PatchesCreator()
        {
            TextureConcieverUTProxy conciever = new TextureConcieverUTProxy();
            _updatableContainer.AddUpdatableElement(conciever);

            Ring2RandomFieldFigureGenerator figureGenerator = new Ring2RandomFieldFigureGenerator(new TextureRenderer(),
                new Ring2RandomFieldFigureGeneratorConfiguration()
                {
                    PixelsPerUnit = new Vector2(1, 1)
                });
            var utFigureGenerator = new RandomFieldFigureGeneratorUTProxy(figureGenerator);
            _updatableContainer.AddUpdatableElement(utFigureGenerator);

            var randomFieldFigureRepository = new Ring2RandomFieldFigureRepository(utFigureGenerator,
                new Ring2RandomFieldFigureRepositoryConfiguration(2, new Vector2(20, 20)));

            Quadtree<Ring2Region> regionsTree = Ring2TestUtils.CreateRegionsTreeWithPath3(randomFieldFigureRepository);

            return new GRing2PatchesCreator(
                new Ring2RegionsDatabase(regionsTree),
                new GRing2RegionsToPatchTemplateConventer(),
                new Ring2PatchTemplateCombiner(),
                new Ring2PatchCreator(),
                new Ring2IntensityPatternProvider(conciever),
                new GRing2Deviser(),
                new Ring2PatchesOverseerConfiguration()
                {
                    IntensityPatternPixelsPerUnit = new Dictionary<int, float>()
                    {
                        {1, 1f}
                    }
                    //PatchSize = new Vector2(90, 90)
                }
            );
        }
    }

    public class ESurfacePatchProvider
    {
        private GRing2PatchesCreatorProxy _patchesCreator;
        private Ring2PatchStamplingOverseerFinalizer _patchStamper;
        private MipmapExtractor _mipmapExtractor;
        private readonly int _mipmapLevelToExtract;

        public ESurfacePatchProvider(GRing2PatchesCreatorProxy patchesCreator, Ring2PatchStamplingOverseerFinalizer patchStamper, MipmapExtractor mipmapExtractor, int mipmapLevelToExtract)
        {
            _patchesCreator = patchesCreator;
            _patchStamper = patchStamper;
            _mipmapExtractor = mipmapExtractor;
            _mipmapLevelToExtract = mipmapLevelToExtract;
        }

        public ESurfaceTexturesPack ProvideSurfaceDetail(MyRectangle inGamePosition, FlatLod flatLod)
        {
            var devisedPatches = _patchesCreator.CreatePatchAsync(inGamePosition.ToRectangle(), flatLod.ScalarValue).Result;
            Preconditions.Assert(devisedPatches.Count <= 1,
                $"More than one patches created: {devisedPatches.Count}, rect is {inGamePosition}");
            if (!devisedPatches.Any())
            {
                return null;
            }

            var onlyPatch = devisedPatches.First();
            var stampedSlice = _patchStamper.FinalizeGPatchCreation(onlyPatch, flatLod.ScalarValue).Result;
            if (stampedSlice != null)
            {
                if (_mipmapLevelToExtract != 0)
                {
                    var mipMappedMainTexture = _mipmapExtractor.ExtractMipmap(new TextureWithSize()
                    {
                        Size = stampedSlice.Resolution,
                        Texture = stampedSlice.ColorStamp
                    }, RenderTextureFormat.ARGB32, _mipmapLevelToExtract);
                    GameObject.Destroy(stampedSlice.ColorStamp);
                    GameObject.Destroy(stampedSlice.NormalStamp);

                    return new ESurfaceTexturesPack()
                    {
                        MainTexture = mipMappedMainTexture.Texture,
                        NormalTexture = stampedSlice.NormalStamp
                    };
                }
                else
                {
                    return new ESurfaceTexturesPack()
                    {
                        MainTexture = stampedSlice.ColorStamp,
                        NormalTexture = stampedSlice.NormalStamp
                    };
                }
            }

            return null;
        }
    }

    public class ESurfaceTexturesPack
    {
        public Texture MainTexture;
        public Texture NormalTexture;
    }

    public class MipmapExtractor
    {
        private UTTextureRendererProxy _textureRenderer;

        public MipmapExtractor(UTTextureRendererProxy textureRenderer)
        {
            _textureRenderer = textureRenderer;
        }

        public TextureWithSize ExtractMipmap(TextureWithSize inputTexture, RenderTextureFormat format, int mipmapLevelToExtract )
        {
            var pack = new UniformsPack();
            pack.SetTexture("_InputTexture", inputTexture.Texture);
            pack.SetUniform("_MipmapLevelToExtract", mipmapLevelToExtract);

            var outSize = ComputeMipmappedOutSize(inputTexture.Size, mipmapLevelToExtract);

            var newTexture = _textureRenderer.AddOrder(new TextureRenderingTemplate()
            {
                CanMultistep = true,
                Coords = new MyRectangle(0, 0, 1, 1),
                CreateTexture2D = false,
                OutTextureInfo = new ConventionalTextureInfo(outSize.X, outSize.Y, TextureFormat.ARGB32, true),
                RenderTextureFormat = format,
                RenderTextureMipMaps = true,
                ShaderName = "Custom/Tool/ExtractMipmap",
                UniformPack = pack,
            }).Result;
            return new TextureWithSize()
            {
                Texture = newTexture,
                Size = outSize
            };
        }

        private IntVector2 ComputeMipmappedOutSize(IntVector2 inputTextureSize, int mipmapLevelToExtract)
        {
            var divisor = Mathf.Pow(2f, mipmapLevelToExtract);
            return new IntVector2(Mathf.RoundToInt(inputTextureSize.X/divisor), Mathf.RoundToInt(inputTextureSize.Y/divisor));
        }
    }


    public static class ESurfaceProviderInitializationHelper
    {
        public static ESurfacePatchProvider ConstructProvider(UpdatableContainer updatableContainer, Dictionary<int, float> intensityPatternPixelsPerUnit,
            ComputeShaderContainerGameObject shaderContainerGO, int mipmapLevelToExtract, Dictionary<int, float> plateStampPixelsPerUnit)
        {
            var ring2ShaderRepository = Ring2PlateShaderRepository.Create();
            TextureConcieverUTProxy conciever = new TextureConcieverUTProxy();
            updatableContainer.AddUpdatableElement(conciever);

            var ring2PatchesPainterUtProxy = new Ring2PatchesPainterUTProxy(
                new Ring2PatchesPainter(
                    new Ring2MultishaderMaterialRepository(ring2ShaderRepository, Ring2ShaderNames.ShaderNames)));
            updatableContainer.AddUpdatableElement(ring2PatchesPainterUtProxy);

            UTRing2PlateStamperProxy stamperProxy = new UTRing2PlateStamperProxy(
                new Ring2PlateStamper(new Ring2PlateStamperConfiguration()
                {
                    PlateStampPixelsPerUnit = plateStampPixelsPerUnit
                }, shaderContainerGO));
            updatableContainer.AddUpdatableElement(stamperProxy);

            UTTextureRendererProxy textureRendererProxy = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(shaderContainerGO), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(500, 500)
                }));
            updatableContainer.AddUpdatableElement(textureRendererProxy);

            CommonExecutorUTProxy commonExecutorUtProxy = new CommonExecutorUTProxy(); //todo
            updatableContainer.AddUpdatableElement(commonExecutorUtProxy);

            Ring2PatchStamplingOverseerFinalizer patchStamperOverseerFinalizer =
                new Ring2PatchStamplingOverseerFinalizer(stamperProxy, textureRendererProxy);

            MipmapExtractor mipmapExtractor = new MipmapExtractor(textureRendererProxy);
            var patchesCreatorProxy = new GRing2PatchesCreatorProxy(CreateRing2PatchesCreator(updatableContainer, intensityPatternPixelsPerUnit));
            return new ESurfacePatchProvider(patchesCreatorProxy, patchStamperOverseerFinalizer, mipmapExtractor, mipmapLevelToExtract);
        }

        public static GRing2PatchesCreator CreateRing2PatchesCreator(UpdatableContainer updatableContainer, Dictionary<int, float> intensityPatternPixelsPerUnit )
        {
            TextureConcieverUTProxy conciever = new TextureConcieverUTProxy();
            updatableContainer.AddUpdatableElement(conciever);

            Ring2RandomFieldFigureGenerator figureGenerator = new Ring2RandomFieldFigureGenerator(new TextureRenderer(),
                new Ring2RandomFieldFigureGeneratorConfiguration()
                {
                    PixelsPerUnit = new Vector2(1, 1)
                });
            var utFigureGenerator = new RandomFieldFigureGeneratorUTProxy(figureGenerator);
            updatableContainer.AddUpdatableElement(utFigureGenerator);

            var randomFieldFigureRepository = new Ring2RandomFieldFigureRepository(utFigureGenerator,
                new Ring2RandomFieldFigureRepositoryConfiguration(2, new Vector2(20, 20)));

            Quadtree<Ring2Region> regionsTree = Ring2TestUtils.CreateRegionsTreeWithPath3(randomFieldFigureRepository);

            return new GRing2PatchesCreator(
                new Ring2RegionsDatabase(regionsTree),
                new GRing2RegionsToPatchTemplateConventer(),
                new Ring2PatchTemplateCombiner(),
                new Ring2PatchCreator(),
                new Ring2IntensityPatternProvider(conciever),
                new GRing2Deviser(),
                new Ring2PatchesOverseerConfiguration()
                {
                    IntensityPatternPixelsPerUnit = intensityPatternPixelsPerUnit
                }
            );
        }
    }
}
