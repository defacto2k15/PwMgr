using Assets.Caching;
using Assets.ComputeShaders;
using Assets.FinalExecution;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Ring2;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.TextureRendering;
using Assets.Utils.Textures;
using UnityEngine;

namespace Assets.ETerrain.TestUtils
{
    public class TerrainShapeDbUnderTest
    {
        private TerrainShapeDb _shapeDb;

        public TerrainShapeDbUnderTest(bool useTextureSavingToDisk = false, bool useCornerMerging = false,
            string terrainDetailFilePath = "C:\\unityCache\\", bool useTextureLoadingFromDisk = false)
        {
            CommonExecutorUTProxy commonExecutorUtProxy = new CommonExecutorUTProxy();
            ComputeShaderContainerGameObject containerGameObject = GameObject.FindObjectOfType<ComputeShaderContainerGameObject>();

            var globalHeightTexture = CreateGlobalHeightTexture(commonExecutorUtProxy);

            UTTextureRendererProxy textureRendererProxy = new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(containerGameObject), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(400, 400)
                }));

            UnityThreadComputeShaderExecutorObject computeShaderExecutorObject = new UnityThreadComputeShaderExecutorObject();
            var terrainDetailGenerator = Ring1DebugObjectV2.CreateTerrainDetailGenerator(
                globalHeightTexture, textureRendererProxy, commonExecutorUtProxy, computeShaderExecutorObject,
                containerGameObject);

            TerrainDetailCornerMerger merger = null;
            LateAssignFactory<BaseTerrainDetailProvider> detailProviderFactory = new LateAssignFactory<BaseTerrainDetailProvider>();
            if (useCornerMerging)
            {
                merger = new TerrainDetailCornerMerger(detailProviderFactory, new TerrainDetailAlignmentCalculator(240),textureRendererProxy,new TextureConcieverUTProxy(),
                    new CommonExecutorUTProxy(), new TerrainDetailCornerMergerConfiguration() );
            }

            var terrainDetailProvider = Ring1DebugObjectV2.CreateTerrainDetailProvider(terrainDetailGenerator, merger);
            _shapeDb = FETerrainShapeDbInitialization.CreateTerrainShapeDb(terrainDetailProvider, commonExecutorUtProxy
                , new TerrainDetailAlignmentCalculator(240), useCornerMerging, useTextureSavingToDisk, useTextureLoadingFromDisk
                , new TerrainDetailFileManager(terrainDetailFilePath, commonExecutorUtProxy));

            var baseProvider = new FromTerrainDbBaseTerrainDetailProvider(_shapeDb);
            detailProviderFactory.Assign(baseProvider);
            terrainDetailGenerator.SetBaseTerrainDetailProvider(baseProvider);
        }

        private static RenderTexture CreateGlobalHeightTexture(CommonExecutorUTProxy commonExecutorUtProxy)
        {
            var rgbaMainTexture = SavingFileManager.LoadPngTextureFromFile( @"C:\mgr\PwMgrProject\precomputedResources\allTerrainF1.png" , 3600,
                3600,
                TextureFormat.ARGB32, true, false);
            TerrainTextureFormatTransformator transformator =
                new TerrainTextureFormatTransformator(commonExecutorUtProxy);
            var mirroredImage = transformator.MirrorHeightTexture(new TextureWithSize()
            {
                Size = new IntVector2(3600, 3600),
                Texture = rgbaMainTexture
            });
            var globalHeightTexture = transformator.EncodedHeightTextureToPlain(new TextureWithSize()
            {
                Size = new IntVector2(3600, 3600),
                Texture = mirroredImage
            });
            return globalHeightTexture;
        }

        public TerrainShapeDb ShapeDb => _shapeDb;
    }
}