using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Files;
using Assets.Roads.Pathfinding;
using Assets.TerrainMat.BiomeGen;
using Assets.TerrainMat.Stain;
using Assets.Utils;
using Assets.Utils.MT;
using GeoAPI.Geometries;
using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.TerrainMat
{
    public class HabitatToStainBiomeDebugObject : MonoBehaviour
    {
        public GameObject RenderTextureGameObject;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            BiomeInstancesContainer container = new BiomeInstancesContainer(new BiomesContainerConfiguration()
            {
                Center = new Vector2(0.5f, 0.5f),
                DefaultType = BiomeType.Forest,
                HighQualityQueryDistance = 999999.1f
            });

            //UnityCoordsPositions2D territoryUsedToCreateStainTexture =
            //    new UnityCoordsPositions2D(62 * 720, 67 * 720, 8 * 720, 8 * 720);

            GeoCoordsToUnityTranslator translator = GeoCoordsToUnityTranslator.DefaultTranslator;
            //Debug.Log("T1: "+translator.TranslateToGeo(new Vector2(62*720, 67*720)));
            //Debug.Log("T1: "+translator.TranslateToGeo(new Vector2(62*720 + (720/4), 67*720+(720/4))));

            //UnityCoordsPositions2D territoryUsedToCreateStainTexture =
            //    new UnityCoordsPositions2D(62 * 720, 67 * 720, 1 * 720/4, 1 * 720/4);

            var geoPos1 = new GeoCoordinate(49.600f, 19.543f);
            var lifePos1 = translator.TranslateToUnity(geoPos1);
            var geoPos2 = new GeoCoordinate(49.610f, 19.552f);
            var lifePos2 = translator.TranslateToUnity(geoPos2);
            MyRectangle territoryUsedToCreateStainTexture =
                new MyRectangle(lifePos1.x, lifePos1.y, lifePos2.x - lifePos1.x, lifePos2.y - lifePos1.y);

            HabitatMapDbProxy habitatMap = new HabitatMapDbProxy(new HabitatMapDb(
                new HabitatMapDb.HabitatMapDbInitializationInfo()
                {
                    RootSerializationPath = @"C:\inz\habitating2\"
                }));
            HabitatToStainBiomeConventer conventer =
                new HabitatToStainBiomeConventer(habitatMap, CreateDebugConversionConfiguration());

            conventer.FromHabitatGenerateBiomes(territoryUsedToCreateStainTexture).Result
                .ForEach(c => container.AddBiome(c));

            var arrayGenerator2 = new StainTerrainArrayFromBiomesGenerator(container,
                BiomeGenerationDebugObject.DebugSimpleGenerator(),
                new StainSpaceToUnitySpaceTranslator(territoryUsedToCreateStainTexture),
                new StainTerrainArrayFromBiomesGeneratorConfiguration()
                {
                    TexturesSideLength = 32
                });

            var resourceGenerator = new ComputationStainTerrainResourceGenerator(
                new StainTerrainResourceComposer(
                    new StainTerrainResourceCreatorUTProxy(new StainTerrainResourceCreator())),
                new StainTerrainArrayMelder(),
                arrayGenerator2);
            StainTerrainResource terrainResource = resourceGenerator.GenerateTerrainTextureDataAsync().Result;

            var newMaterial = new Material(Shader.Find("Custom/TerrainTextureTest2"));
            BiomeGenerationDebugObject.ConfigureMaterial(terrainResource, newMaterial);
            RenderTextureGameObject.GetComponent<MeshRenderer>().material = newMaterial;
        }

        public static HabitatToStainBiomeConversionConfiguration CreateDebugConversionConfiguration()
        {
            return new HabitatToStainBiomeConversionConfiguration()
            {
                ConversionSpecifications = new Dictionary<HabitatType, BiomeFromHabitatSpecification>()
                {
                    {
                        HabitatType.Forest, new BiomeFromHabitatSpecification()
                        {
                            Type = BiomeType.Forest,
                            Priority = 2
                        }
                    },
                    {
                        HabitatType.NotSpecified, new BiomeFromHabitatSpecification()
                        {
                            Type = BiomeType.Sand,
                            Priority = 1
                        }
                    },
                    {
                        HabitatType.Meadow, new BiomeFromHabitatSpecification()
                        {
                            Type = BiomeType.Grass,
                            Priority = 3
                        }
                    },
                    {
                        HabitatType.Grassland, new BiomeFromHabitatSpecification()
                        {
                            Type = BiomeType.Grass,
                            Priority = 4
                        }
                    },
                    {
                        HabitatType.Fell, new BiomeFromHabitatSpecification()
                        {
                            Type = BiomeType.Grass,
                            Priority = 5
                        }
                    },
                }
            };
        }
    }

    public class HabitatToStainBiomeConventer
    {
        private HabitatMapDbProxy _habitatMap;
        private HabitatToStainBiomeConversionConfiguration _conversionConfiguration;
        private uint _initialInstanceId;

        public HabitatToStainBiomeConventer(HabitatMapDbProxy habitatMap,
            HabitatToStainBiomeConversionConfiguration conversionConfiguration)
        {
            _habitatMap = habitatMap;
            _conversionConfiguration = conversionConfiguration;
        }

        public async Task<List<BiomeInstanceInfo>> FromHabitatGenerateBiomes(MyRectangle conversionArea)
        {
            var outList = new List<BiomeInstanceInfo>();
            _initialInstanceId = 10000;
            var allHabitats = await _habitatMap.Query(conversionArea);

            int i = 0;
            foreach (var habitat in allHabitats.QueryAll())
            {
                Preconditions.Assert(_conversionConfiguration.ConversionSpecifications.ContainsKey(habitat.Field.Type),
                    "No conversion for type " + habitat.Field.Type);
                var conversionInfo = _conversionConfiguration.ConversionSpecifications[habitat.Field.Type];
                outList.Add(
                    new PolygonBiomeInstanceInfo(conversionInfo.Type, habitat.Field.Geometry as IPolygon,
                        new BiomeInstanceId((uint) (_initialInstanceId + i)), conversionInfo.Priority));
                i++;
            }

            var usedHabitats = allHabitats.QueryAll().Select(c => c.Field.Type).Distinct();
            Debug.Log("t76: " + StringUtils.ToString(usedHabitats) + " count: " + allHabitats.QueryAll().Count);
            return outList;
        }
    }

    public class HabitatToStainBiomeConversionConfiguration
    {
        public Dictionary<HabitatType, BiomeFromHabitatSpecification> ConversionSpecifications;
    }

    public class BiomeFromHabitatSpecification
    {
        public BiomeType Type;
        public int Priority;
    }
}