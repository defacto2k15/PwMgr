using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Caching;
using Assets.ESurface;
using Assets.ETerrain.GroundTexture;
using Assets.ETerrain.Pyramid.Map;
using Assets.ETerrain.SectorFilling;
using Assets.FinalExecution;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.CornerMerging;
using Assets.Repositioning;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Utils;
using Assets.Utils.Services;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.ETerrain.ETerrainIntegration.deos
{
    public static class ETerrainInitializationHelper
    {
        public static OneGroundTypeLevelTextureEntitiesGenerator CreateHeightTextureEntitiesGenerator(
            ETerrainHeightPyramidFacadeStartConfiguration startConfiguration, GameInitializationFields initializationFields,
            UltraUpdatableContainer updatableContainer
            , HeightmapSegmentFillingListenersContainer heightmapListenersesContainer)
        {
            startConfiguration.CommonConfiguration.UseNormalTextures = true;
            var textureRendererProxy = initializationFields.Retrive<UTTextureRendererProxy>();
            var dbProxy = initializationFields.Retrive<TerrainShapeDbProxy>();
            var repositioner = initializationFields.Retrive<Repositioner>();

            return new OneGroundTypeLevelTextureEntitiesGenerator
            {
                CeilTextureArrayGenerator = () =>
                {
                    var outList = new List<EGroundTexture>()
                    {
                        new EGroundTexture(
                            texture: EGroundTextureGenerator.GenerateEmptyGroundTextureArray(startConfiguration.CommonConfiguration.CeilTextureSize
                                , startConfiguration.HeightPyramidLevels.Count, startConfiguration.CommonConfiguration.HeightTextureFormat),
                            textureType: EGroundTextureType.HeightMap
                        ),
                    };
                    if (startConfiguration.CommonConfiguration.UseNormalTextures)
                    {
                        outList.Add(
                            new EGroundTexture(
                                texture: EGroundTextureGenerator.GenerateEmptyGroundTextureArray(startConfiguration.CommonConfiguration.CeilTextureSize
                                    , startConfiguration.HeightPyramidLevels.Count, startConfiguration.CommonConfiguration.NormalTextureFormat),
                                textureType: EGroundTextureType.NormalTexture
                            )
                        );
                    }

                    return outList;
                },
                SegmentFillingListenerGeneratorFunc = (level, ceilTextureArrays) =>
                {
                    var usedGroundTypes = new List<EGroundTextureType>() {EGroundTextureType.HeightMap};
                    if (startConfiguration.CommonConfiguration.UseNormalTextures)
                    {
                        usedGroundTypes.Add(EGroundTextureType.NormalTexture);
                    }

                    var segmentModificationManagers = usedGroundTypes.ToDictionary(groundType => groundType,
                        groundType =>
                        {
                            var groundTexture = ceilTextureArrays.First(c => c.TextureType == groundType);

                            var segmentsPlacer = new HeightSegmentPlacer(
                                textureRendererProxy, initializationFields.Retrive<CommonExecutorUTProxy>(), groundTexture.Texture
                                , level.GetIndex(), startConfiguration.CommonConfiguration.SlotMapSize, startConfiguration.CommonConfiguration.CeilTextureSize
                                , startConfiguration.CommonConfiguration.InterSegmentMarginSize, startConfiguration.CommonConfiguration.SegmentTextureResolution
                                , startConfiguration.CommonConfiguration.MergeSegmentsInFloorTexture
                            );
                            var pyramidLevelManager = new GroundLevelTexturesManager(startConfiguration.CommonConfiguration.SlotMapSize);
                            return new SoleLevelGroundTextureSegmentModificationsManager(segmentsPlacer, pyramidLevelManager);
                        });

                    var otherThreadExecutor = new OtherThreadCompoundSegmentFillingOrdersExecutorProxy("Height-" + level.ToString(),
                        new CompoundSegmentOrdersFillingExecutor<TerrainDescriptionOutput>(
                            async (sap) =>
                            {
                                var surfaceWorldSpaceRectangle = ETerrainUtils.TerrainShapeSegmentAlignedPositionToWorldSpaceArea(level,
                                    startConfiguration.PerLevelConfigurations[level], sap);

                                var terrainDescriptionOutput = await dbProxy.Query(new TerrainDescriptionQuery()
                                {
                                    QueryArea = repositioner.InvMove(surfaceWorldSpaceRectangle),
                                    RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                                    {
                                        new TerrainDescriptionQueryElementDetail()
                                        {
                                            Resolution = ETerrainUtils.HeightPyramidLevelToTerrainShapeDatabaseResolution(level),
                                            RequiredMergeStatus = RequiredCornersMergeStatus.MERGED,
                                            Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                                        },
                                        new TerrainDescriptionQueryElementDetail()
                                        {
                                            Resolution = ETerrainUtils.HeightPyramidLevelToTerrainShapeDatabaseResolution(level),
                                            RequiredMergeStatus = RequiredCornersMergeStatus.NOT_MERGED,
                                            Type = TerrainDescriptionElementTypeEnum.NORMAL_ARRAY
                                        },
                                    }
                                });
                                return terrainDescriptionOutput;

                            },
                            async (sap, terrainDescriptionOutput) =>
                            {
                                var heightSegmentTexture = terrainDescriptionOutput.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY)
                                    .TokenizedElement.DetailElement.Texture.Texture;
                                await segmentModificationManagers[EGroundTextureType.HeightMap].AddSegmentAsync(heightSegmentTexture, sap);

                                if (startConfiguration.CommonConfiguration.UseNormalTextures)
                                {
                                    var normalSegmentTexture = terrainDescriptionOutput.GetElementOfType(TerrainDescriptionElementTypeEnum.NORMAL_ARRAY)
                                        .TokenizedElement.DetailElement.Texture.Texture;
                                    await segmentModificationManagers[EGroundTextureType.NormalTexture].AddSegmentAsync(normalSegmentTexture, sap);
                                }
                            },
                            async (terrainDescriptionOutput) =>
                            {
                                await dbProxy.DisposeTerrainDetailElement(terrainDescriptionOutput
                                    .GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY).TokenizedElement.Token);
                                if (startConfiguration.CommonConfiguration.UseNormalTextures)
                                {
                                    await dbProxy.DisposeTerrainDetailElement(terrainDescriptionOutput
                                        .GetElementOfType(TerrainDescriptionElementTypeEnum.NORMAL_ARRAY).TokenizedElement.Token);
                                }
                            }
                        ));
                    updatableContainer.AddOtherThreadProxy(otherThreadExecutor);

                    var fillingListener = new UnityThreadCompoundSegmentFillingListener(otherThreadExecutor);
                    heightmapListenersesContainer.AddListener(level, fillingListener);

                    var travellerCustodian = initializationFields.Retrive<TravellerMovementCustodian>();
                    travellerCustodian.AddLimiter(() => new MovementBlockingProcess()
                        {ProcessName = "HeightSegmentsGenerationProcess " + level, BlockCount = fillingListener.BlockingProcessesCount()});
                    //initializationFields.Retrive<InitialSegmentsGenerationInspector>().SetConditionToCheck(() => fillingListener.BlockingProcessesCount() == 0);
                    return fillingListener;
                }
            };
        }

        public static OneGroundTypeLevelTextureEntitiesGenerator CreateSurfaceTextureEntitiesGenerator(
            FEConfiguration configuration, ETerrainHeightPyramidFacadeStartConfiguration startConfiguration, GameInitializationFields gameInitializationFields
            , UltraUpdatableContainer ultraUpdatableContainer)
        {
            var repositioner = gameInitializationFields.Retrive<Repositioner>();
            var surfaceTextureFormat = RenderTextureFormat.ARGB32;
            var commonExecutor = gameInitializationFields.Retrive<CommonExecutorUTProxy>();

            var feRing2PatchConfiguration = new Ring2PatchInitializerConfiguration(configuration);

            feRing2PatchConfiguration.Ring2PlateStamperConfiguration.PlateStampPixelsPerUnit =
                feRing2PatchConfiguration.Ring2PlateStamperConfiguration.PlateStampPixelsPerUnit.ToDictionary(
                    c => c.Key,
                    c => c.Value * Mathf.Pow(2, feRing2PatchConfiguration.MipmapLevelToExtract)
                );

            var patchInitializer = new Ring2PatchInitializer(gameInitializationFields, ultraUpdatableContainer, feRing2PatchConfiguration);
            patchInitializer.Start();

            var mipmapExtractor = new MipmapExtractor(gameInitializationFields.Retrive<UTTextureRendererProxy>());
            var patchesCreatorProxy = gameInitializationFields.Retrive<GRing2PatchesCreatorProxy>();
            var patchStamperOverseerFinalizer = gameInitializationFields.Retrive<Ring2PatchStamplingOverseerFinalizer>();
            var surfacePatchProvider = new ESurfacePatchProvider(patchesCreatorProxy, patchStamperOverseerFinalizer, commonExecutor,
                mipmapExtractor, feRing2PatchConfiguration.MipmapLevelToExtract);

            var cachedSurfacePatchProvider =
                new CachedESurfacePatchProvider(surfacePatchProvider
                    , new InMemoryAssetsCache<ESurfaceTexturesPackToken, NullableESurfaceTexturesPack>(
                        FETerrainShapeDbInitialization.CreateLevel2AssetsCache<ESurfaceTexturesPackToken, NullableESurfaceTexturesPack>(
                            cachingConfiguration: new CachingConfiguration()
                            {
                                SaveAssetsToFile = true,
                                UseFileCaching = true,
                            }
                            , new InMemoryCacheConfiguration() /*{ MaxTextureMemoryUsed = 0}*/
                            , new ESurfaceTexturesPackEntityActionsPerformer(commonExecutor)
                            , new ESurfaceTexturesPackFileManager(commonExecutor, configuration.FilePathsConfiguration.SurfacePatchCachePath))));
            cachedSurfacePatchProvider.Initialize().Wait();

            var cachedSurfacePatchDbProxy = new ESurfacePatchDbProxy(cachedSurfacePatchProvider);
            ultraUpdatableContainer.AddOtherThreadProxy(cachedSurfacePatchDbProxy);

            var textureRendererProxy = gameInitializationFields.Retrive<UTTextureRendererProxy>();

            return new OneGroundTypeLevelTextureEntitiesGenerator()
            {
                CeilTextureArrayGenerator =  () =>
                {
                    return new List<EGroundTexture>()
                    {
                        new EGroundTexture( EGroundTextureGenerator.GenerateEmptyGroundTextureArray(startConfiguration.CommonConfiguration.CeilTextureSize,
                            startConfiguration.HeightPyramidLevels.Count, surfaceTextureFormat),
                        EGroundTextureType.SurfaceTexture )
                    };
                },
                SegmentFillingListenerGeneratorFunc = (level, ceilTextureArrays) =>
                {
                    var ceilTextureArray = ceilTextureArrays.First(c => c.TextureType == EGroundTextureType.SurfaceTexture);
                    var segmentsPlacer = new ESurfaceSegmentPlacer(textureRendererProxy, ceilTextureArray.Texture, level.GetIndex()
                        , startConfiguration.CommonConfiguration.SlotMapSize, startConfiguration.CommonConfiguration.CeilTextureSize);
                    var pyramidLevelManager = new GroundLevelTexturesManager(startConfiguration.CommonConfiguration.SlotMapSize);
                    var segmentModificationManager = new SoleLevelGroundTextureSegmentModificationsManager(segmentsPlacer, pyramidLevelManager);

                    var otherThreadExecutor = new OtherThreadCompoundSegmentFillingOrdersExecutorProxy("ESurface-" + level.ToString(),
                        new CompoundSegmentOrdersFillingExecutor<TokenizedESurfaceTexturesPackToken>(
                            async (sap) =>
                            {
                                var surfaceWorldSpaceRectangle = ETerrainUtils.SurfaceTextureSegmentAlignedPositionToWorldSpaceArea(level,
                                    startConfiguration.PerLevelConfigurations[level], sap);
                                var lod = ETerrainUtils.HeightPyramidLevelToSurfaceTextureFlatLod(level);
                                return await cachedSurfacePatchDbProxy.ProvideSurfaceDetail(repositioner.InvMove(surfaceWorldSpaceRectangle), lod);
                            },
                            async (sap, packAndToken) =>
                            {
                                var pack = packAndToken.Pack;
                                if (pack != null)
                                {
                                    var mainTexture = pack.MainTexture;
                                    await segmentModificationManager.AddSegmentAsync(mainTexture, sap);
                                }
                            },
                            segmentRemovalFunc: async (packAndToken) =>
                            {
                                if (packAndToken != null)
                                {
                                    var pack = packAndToken.Pack;
                                    if (pack != null)
                                    {
                                        Preconditions.Assert(packAndToken.Token != null, "Token is null. Unexpected");
                                        cachedSurfacePatchDbProxy.RemoveSurfaceDetailAsync(pack, packAndToken.Token);
                                    }
                                }
                            }
                            ));

                    ultraUpdatableContainer.AddOtherThreadProxy(otherThreadExecutor);
                    var fillingListener = new UnityThreadCompoundSegmentFillingListener(otherThreadExecutor);
                    var travellerCustodian = gameInitializationFields.Retrive<TravellerMovementCustodian>();
                    travellerCustodian.AddLimiter(() => new MovementBlockingProcess() { BlockCount = fillingListener.BlockingProcessesCount(), ProcessName = "SurfaceSegmentsGeneration " + level });
                    return fillingListener;
                }
            };
        }

    }
}
