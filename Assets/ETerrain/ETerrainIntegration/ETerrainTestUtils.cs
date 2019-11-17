using System.Collections.Generic;
using Assets.ComputeShaders;
using Assets.ETerrain.SectorFilling;
using Assets.FinalExecution;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.Db;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration
{
    public static class ETerrainTestUtils
    {

        public static Texture CreateDummyHeightTextureDependentOnPixelPosition(SegmentInformation segmentInformation, float offset, float stepMultiplier)
        {
            var tex = new Texture2D(240, 240, TextureFormat.RFloat, false);
            float[] rawTextureData = new float[tex.width * tex.height];
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    rawTextureData[x + y * tex.width] = offset + ((x + y * 4) / 400f) * stepMultiplier;
                }
            }

            tex.LoadRawTextureData(CastUtils.ConvertFloatArrayToByte(rawTextureData));

            tex.Apply();
            return tex;
        }

        public static Texture CreateDummyHeightTextureDependentOnWorldSpacePosition(MyRectangle worldSpaceRectangle, float repreatLength)
        {
            var tex = new Texture2D(240, 240, TextureFormat.RFloat, false);
            float[] rawTextureData = new float[tex.width * tex.height];
            for (int x = 0; x < tex.width; x++)
            {
                for (int y = 0; y < tex.height; y++)
                {
                    var uvInTexture = new Vector2(x / 240f, y / 240f);
                    var worldSpacePosition = worldSpaceRectangle.SampleByUv(uvInTexture);
                    rawTextureData[x + y * tex.width] = Mathf.Repeat(worldSpacePosition.x + worldSpacePosition.y * 4, repreatLength) / repreatLength;
                }
            }

            tex.LoadRawTextureData(CastUtils.ConvertFloatArrayToByte(rawTextureData));

            tex.Apply();
            return tex;
        }


        public static UltraUpdatableContainer InitializeFinalElements(FEConfiguration configuration, ComputeShaderContainerGameObject containerGameObject,
            GameInitializationFields gameInitializationFields, Dictionary<int, Ring2RegionsDbGeneratorConfiguration> ring2RegionsDatabasesConfiguration = null
            , bool initializeLegacyDesignBodySpotUpdater = false)
        {
            TaskUtils.SetGlobalMultithreading(configuration.Multithreading);
            return TaskUtils.ExecuteFunctionWithOverridenMultithreading(true, () =>
            {
                var servicesProfileInfo = new GlobalServicesProfileInfo();
                gameInitializationFields.SetField(servicesProfileInfo);
                var ultraUpdatableContainer = new UltraUpdatableContainer(
                    configuration.SchedulerConfiguration,
                    servicesProfileInfo,
                    configuration.UpdatableContainerConfiguration);

                configuration.TerrainShapeDbConfiguration.UseTextureSavingToDisk = true;

                gameInitializationFields.SetField(containerGameObject);
                gameInitializationFields.SetField(configuration.Repositioner);
                gameInitializationFields.SetField(configuration.HeightDenormalizer);

                var initializingHelper =
                    new FEInitializingHelper(gameInitializationFields, ultraUpdatableContainer, configuration);
                initializingHelper.InitializeUTService(new TextureConcieverUTProxy());
                initializingHelper.InitializeUTService(new UnityThreadComputeShaderExecutorObject());
                initializingHelper.InitializeUTService(new CommonExecutorUTProxy());
                initializingHelper.CreatePathProximityTextureDb();

                if (initializeLegacyDesignBodySpotUpdater)
                {
                    initializingHelper.InitializeDesignBodySpotUpdater();
                }

                initializingHelper.InitializeUTRendererProxy();
                initializingHelper.InitializeUTService(new MeshGeneratorUTProxy(new MeshGeneratorService()));
                if (ring2RegionsDatabasesConfiguration != null)
                {
                    initializingHelper.InitializeComplexRing2RegionsDatabase(ring2RegionsDatabasesConfiguration);
                }
                else
                {
                    initializingHelper.InitializeMonoliticRing2RegionsDatabase();
                }

                //var finalTerrainInitialization = new FinalTerrainInitialization(_ultraUpdatableContainer, _gameInitializationFields, _configuration, FeGRingConfiguration);
                //finalTerrainInitialization.Start();

                initializingHelper.InitializeGlobalInstancingContainer();
                //var finalVegetation = new FinalVegetation(_gameInitializationFields, _ultraUpdatableContainer, VegetationConfiguration);
                //finalVegetation.Start();
                return ultraUpdatableContainer;
            });
        }
    }
}