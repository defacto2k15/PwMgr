using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Grass2.Types;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Pathfinding;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Spatial;
using Assets.Utils.Textures;
using OsmSharp.Math.Geo;
using UnityEngine;

namespace Assets.Grass2.IntenstityDb
{
    public class Grass2IntensityDbDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            var queryingArea = new MyRectangle(526 * 90, 582 * 90, 90, 90);


            HabitatToGrassIntensityMapGenerator habitatToGrassIntensityMapGenerator =
                new HabitatToGrassIntensityMapGenerator(ComputeShaderContainer,
                    new UnityThreadComputeShaderExecutorObject(), new CommonExecutorUTProxy(),
                    new HabitatToGrassIntensityMapGenerator.HabitatToGrassIntensityMapGeneratorConfiguration()
                    {
                        GrassTypeToSourceHabitats = new Dictionary<GrassType, List<HabitatType>>()
                        {
                            {GrassType.Debug1, new List<HabitatType>() {HabitatType.Forest}},
                            {GrassType.Debug2, new List<HabitatType>() {HabitatType.Meadow, HabitatType.Fell}},
                        },
                        OutputPixelsPerUnit = 1
                    });

            HabitatMapDbProxy habitatDbProxy = new HabitatMapDbProxy(new HabitatMapDb(
                new HabitatMapDb.HabitatMapDbInitializationInfo()
                {
                    RootSerializationPath = @"C:\inz\habitating2\"
                }));

            var mapsGenerator = new Grass2IntensityMapGenerator(
                habitatToGrassIntensityMapGenerator,
                new HabitatTexturesGenerator(habitatDbProxy,
                    new HabitatTexturesGenerator.HabitatTexturesGeneratorConfiguration()
                    {
                        HabitatMargin = 5,
                        HabitatSamplingUnit = 3
                    }, new TextureConcieverUTProxy()),
                new Grass2IntensityMapGenerator.Grass2IntensityMapGeneratorConfiguration()
                {
                    HabitatSamplingUnit = 3
                }, null);

            var db = new SpatialDb<List<Grass2TypeWithIntensity>>(mapsGenerator,
                new SpatialDbConfiguration()
                {
                    QueryingCellSize = new Vector2(90, 90)
                });

            var retrivedMap = db.ProvidePartsAt(queryingArea).Result.CoordedPart.Part;
            Debug.Log("Ret: " + retrivedMap.Count);

            retrivedMap.Select(c => new Grass2TypeWithIntensity()
            {
                GrassType = c.GrassType,
                IntensityFigure = c.IntensityFigure
            }).Select((c, i) =>
            {
                HabitatToGrassIntensityMapGeneratorDebugObject.CreateDebugObject(c, i);
                return 0;
            }).ToList();
        }
    }
}