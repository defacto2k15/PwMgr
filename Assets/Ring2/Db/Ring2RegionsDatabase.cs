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
    public class Ring2RegionsDatabase
    {
        private Quadtree<Ring2Region> _regionsTree = new Quadtree<Ring2Region>();

        public Ring2RegionsDatabase(Quadtree<Ring2Region> regionsTree = null)
        {
            if (regionsTree == null)
            {
                regionsTree = new Quadtree<Ring2Region>();
            }
            _regionsTree = regionsTree;
        }

        public List<Ring2Region> QueryRegions(MyRectangle queryArea)
        {
            var foundRegions = _regionsTree.Query(MyNetTopologySuiteUtils.ToEnvelope(queryArea)).ToList();

            var intersectingRegions = foundRegions
                .Where(c => c.Space.Intersects(MyNetTopologySuiteUtils.ToEnvelope(queryArea)))
                .OrderByDescending(c => c.Magnitude)
                .ToList();
            return intersectingRegions;
        }
    }
}