using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Habitat;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.IntensityProvider;
using Assets.Ring2.RegionSpace;
using Assets.Roads;
using Assets.Roads.Files;
using Assets.TerrainMat;
using Assets.Utils;
using GeoAPI.Geometries;
using GeoAPI.Operation.Buffer;
using NetTopologySuite.Index.Quadtree;
using NetTopologySuite.Operation.Buffer;
using UnityEngine;

namespace Assets.Ring2.Db
{
    public class Ring2RegionsDbGenerator
    {
        private HabitatMapDbProxy _habitatMap;
        private readonly Ring2RegionsDbGeneratorConfiguration _configuration;
        private readonly RoadDatabaseProxy _roadDatabase;

        public Ring2RegionsDbGenerator(
            HabitatMapDbProxy habitatMap,
            Ring2RegionsDbGeneratorConfiguration configuration,
            RoadDatabaseProxy roadDatabase = null)
        {
            _habitatMap = habitatMap;
            _configuration = configuration;
            _roadDatabase = roadDatabase;
        }

        public async Task<MonoliticRing2RegionsDatabase> GenerateDatabaseAsync(MyRectangle generationArea)
        {
            var habitatFields = await _habitatMap.Query(generationArea);
            var outTree = new Quadtree<Ring2Region>();

            var fromHabitatTemplates = _configuration.FromHabitatTemplates;
            foreach (var field in habitatFields.QueryAll().Select(c => c.Field))
            {
                //if (field.Geometry.Area < _configuration.MinimalRegionArea)
                //{
                //    continue;
                //}

                Preconditions.Assert(fromHabitatTemplates.ContainsKey(field.Type), "no for type: " + field.Type);
                var template = fromHabitatTemplates[field.Type];
                var newGeometry = AddBuffer(field.Geometry, template.BufferLength);

                var newRegion = new Ring2Region(
                    RegionSpaceUtils.Create(newGeometry),
                    new Ring2Substance(template.Fabrics),
                    template.Magnitude
                );
                outTree.Insert(newRegion.RegionEnvelope, newRegion);
            }

            if (_roadDatabase != null && _configuration.GenerateRoadHabitats)
            {
                var pathTemplate = _configuration.FromPathsTemplate;
                var paths = await _roadDatabase.Query(_configuration.Ring2RoadsQueryArea);
                foreach (var aPath in paths)
                {
                    var area = RegionSpaceUtils.ToFatLineString(_configuration.PathWidth,
                        aPath.PathNodes.Select(c => new Vector2(c.x, c.y - 3.0f)));

                    var newRegion = new Ring2Region(
                        area,
                        new Ring2Substance(pathTemplate.Fabrics),
                        pathTemplate.Magnitude
                    );

                    outTree.Insert(newRegion.RegionEnvelope, newRegion);
                }
            }

            return new MonoliticRing2RegionsDatabase(outTree);
        }

        private IGeometry AddBuffer(IGeometry inputGeo, float bufferLength)
        {
            var op = new BufferOp(inputGeo, new BufferParameters(0, EndCapStyle.Square));
            return op.GetResultGeometry(bufferLength);
        }
    }

    public class Ring2RegionFromHabitatTemplate
    {
        public List<Ring2Fabric> Fabrics;
        public float Magnitude;
        public float BufferLength;
    }

    public class Ring2RegionsDbGeneratorConfiguration
    {
        public Dictionary<HabitatType, Ring2RegionFromHabitatTemplate> FromHabitatTemplates;
        public Ring2RegionFromHabitatTemplate FromPathsTemplate;
        public MyRectangle Ring2RoadsQueryArea;
        public float PathWidth = 1.5f;
        public bool GenerateRoadHabitats = true;
        public float MinimalRegionArea = float.MaxValue;
    }
}