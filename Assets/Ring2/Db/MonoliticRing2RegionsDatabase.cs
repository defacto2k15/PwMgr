using System.Collections.Generic;
using System.Linq;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.Geometries;
using Assets.TerrainMat;
using Assets.Utils;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.Ring2.Db
{
    public interface IRing2RegionsDatabase
    {
        List<Ring2Region> QueryRegions(int lodValue, MyRectangle queryArea);
    }

    public class MonoliticRing2RegionsDatabase : IRing2RegionsDatabase
    {
        private Quadtree<Ring2Region> _regionsTree = new Quadtree<Ring2Region>();

        public MonoliticRing2RegionsDatabase(Quadtree<Ring2Region> regionsTree = null)
        {
            if (regionsTree == null)
            {
                regionsTree = new Quadtree<Ring2Region>();
            }
            _regionsTree = regionsTree;
        }

        public List<Ring2Region> QueryRegions(int lodValue, MyRectangle queryArea)
        {
            var foundRegions = _regionsTree.Query(MyNetTopologySuiteUtils.ToEnvelope(queryArea)).ToList();

            var intersectingRegions = foundRegions
                .Where(c => c.Space.Intersects(MyNetTopologySuiteUtils.ToEnvelope(queryArea)))
                .OrderByDescending(c => c.Magnitude)
                .ToList();
            return intersectingRegions;
        }
    }

    public class ComplexRing2RegionsDatabase : IRing2RegionsDatabase
    {
        private Dictionary<int, MonoliticRing2RegionsDatabase> _dbsDict;

        public ComplexRing2RegionsDatabase(Dictionary<int, MonoliticRing2RegionsDatabase> dbsDict)
        {
            _dbsDict = dbsDict;
        }

        public List<Ring2Region> QueryRegions(int lodValue, MyRectangle queryArea)
        {
            return _dbsDict[lodValue].QueryRegions(lodValue, queryArea);
        }
    }
}