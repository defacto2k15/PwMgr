using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.FinalExecution;
using Assets.Habitat;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.MeshGeneration;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Heightmaps.TextureUtils;
using Assets.PreComputation.Configurations;
using Assets.Roads.Files;
using Assets.Roads.Pathfinding;
using Assets.Scheduling;
using Assets.TerrainMat;
using Assets.TerrainMat.BiomeGen;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.PreComputation
{
    public class RootPrecomputer
    {
        private GameInitializationFields _gameInitializationFields = new GameInitializationFields();

        private PrecomputationConfiguration _precomputationConfiguration =
            new PrecomputationConfiguration(new FEConfiguration(new FilePathsConfiguration()),
                new FilePathsConfiguration());

        private FilePathsConfiguration _filePathsConfiguration = new FilePathsConfiguration();

        public void Compute(ComputeShaderContainerGameObject computeShaderContainer)
        {
            TaskUtils.SetGlobalMultithreading(false);
            _gameInitializationFields.SetField(computeShaderContainer);

            var msw = new MyStopWatch();

            var queries = RecordingTerrainShapeDb.LoadQueriesFromFile(@"C:\inz\allQueries.json");
            Debug.Log("H8: " + StringUtils.ToString(queries.Select(c => c.QueryArea)));

            //msw.StartSegment("TerrainDbInitialization");
            InitializeTerrainDb(true);

            //TestHeightNormalization();
            //return;

            msw.StartSegment("PathPrecomputation");
            var pathPrecomputation = new PathPrecomputation(_gameInitializationFields, _precomputationConfiguration,
                _filePathsConfiguration);
            //pathPrecomputation.Compute();
            pathPrecomputation.Load();

            msw.StartSegment("HabitatMapDbPrecomputation");
            var habitatMapDbPrecomputation = new HabitatMapDbPrecomputation(_gameInitializationFields,
                _precomputationConfiguration, _filePathsConfiguration);
            //habitatMapDbPrecomputation.Compute();
            habitatMapDbPrecomputation.Load();

            msw.StartSegment("Ring1Precomputation");
            var ring1Precomputation = new Ring1Precomputation(_gameInitializationFields, _precomputationConfiguration,
                _filePathsConfiguration);
            ring1Precomputation.Compute();
            //ring1Precomputation.Load();

            //msw.StartSegment("VegetationDatabasePrecomputation");
            //var vegetationDatabasePrecomputation =
            //    new VegetationDatabasePrecomputation(_gameInitializationFields, _precomputationConfiguration, _filePathsConfiguration);
            //vegetationDatabasePrecomputation.Compute();

            //msw.StartSegment("Grass2billboardsPrecomputation");
            //var grass2BillboardsPrecomputation = new Grass2BillboardsPrecomputer(_gameInitializationFields, _filePathsConfiguration);
            ////grass2BillboardsPrecomputation.Compute();

            //Debug.Log("L8 Precomputation time: "+msw.CollectResults());
        }

        private void InitializeTerrainDb(bool useTerrainDetailFileCache = false)
        {
            var ultraUpdatableContainer = new UltraUpdatableContainer(new MyUtSchedulerConfiguration(),
                new GlobalServicesProfileInfo(), new UltraUpdatableContainerConfiguration());

            var feConfiguration = new FEConfiguration(new FilePathsConfiguration());
            var feGRingConfiguration = new FeGRingConfiguration();
            feGRingConfiguration.FeConfiguration = feConfiguration;

            feConfiguration.TerrainDetailProviderConfiguration.UseTextureSavingToDisk = useTerrainDetailFileCache;
            feConfiguration.TerrainDetailProviderConfiguration.UseTextureLoadingFromDisk= useTerrainDetailFileCache;
            feConfiguration.EngraveRoadsInTerrain = false;

            TaskUtils.SetGlobalMultithreading(feConfiguration.Multithreading);
            TaskUtils.SetMultithreadingOverride(true);

            _gameInitializationFields.SetField(feConfiguration.Repositioner);
            _gameInitializationFields.SetField(feConfiguration.HeightDenormalizer);

            var initializingHelper =
                new FEInitializingHelper(_gameInitializationFields, ultraUpdatableContainer, feConfiguration);
            initializingHelper.InitializeUTService(new TextureConcieverUTProxy());
            initializingHelper.InitializeUTService(new UnityThreadComputeShaderExecutorObject());
            initializingHelper.InitializeUTService(new CommonExecutorUTProxy());

            initializingHelper.InitializeUTRendererProxy();


            var finalTerrainShapeDb = new FETerrainShapeDbInitialization(ultraUpdatableContainer,
                _gameInitializationFields, feConfiguration, _filePathsConfiguration);
            finalTerrainShapeDb.Start();
        }

        public void FillTerrainDetailsCache(ComputeShaderContainerGameObject computeShaderContainer)
        {
            TaskUtils.SetGlobalMultithreading(false);
            _gameInitializationFields.SetField(computeShaderContainer);

            InitializeTerrainDb(true);

            var db = _gameInitializationFields.Retrive<ITerrainShapeDb>();
            var queries = RecordingTerrainShapeDb.LoadQueriesFromFile(@"C:\inz\allQueries.json");
            Debug.Log("AXXX + " + queries.Count);
            foreach (var aQuery in queries.Skip(3).Take(10))
            {
                var xx = db.Query(aQuery).Wait(1000);
            }
        }

        private void TestHeightNormalization()
        {
            var db = _gameInitializationFields.Retrive<TerrainShapeDbProxy>();

            var translator = GeoCoordsToUnityTranslator.DefaultTranslator;
            //var unityPosition = translator.TranslateToUnity(new GeoCoordinate(49.613, 19.5510));
            var unityPosition = translator.TranslateToUnity(new GeoCoordinate(49.573343, 19.528953));

            var queryArea = new MyRectangle(unityPosition.x - 10, unityPosition.y - 10, 20, 20);

            var uvdHeight = db.Query(new TerrainDescriptionQuery()
            {
                QueryArea = queryArea,
                RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                {
                    new TerrainDescriptionQueryElementDetail()
                    {
                        Resolution = TerrainCardinalResolution.MAX_RESOLUTION,
                        Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                    }
                }
            }).Result.GetElementOfType(TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY);

            var uvBase = uvdHeight.UvBase;
            var plainHeightTexture = uvdHeight.TokenizedElement.DetailElement.Texture;

            var transformator = new TerrainTextureFormatTransformator(new CommonExecutorUTProxy());
            var encodedTexture = transformator.PlainToEncodedHeightTextureAsync(plainHeightTexture).Result;

            var pixelPoint =
                RectangleUtils.CalculateSubPosition(new MyRectangle(0, 0, 241, 241), uvBase.Center);

            var intCenterPoint = new IntVector2(Mathf.RoundToInt(pixelPoint.x), Mathf.RoundToInt(pixelPoint.y));

            var color = encodedTexture.GetPixel(intCenterPoint.X, intCenterPoint.Y);
            var height = HeightColorTransform.DecodeHeight(color);
            Debug.Log("Normalized Height is: " + height);
        }
    }
}