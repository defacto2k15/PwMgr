using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ETerrain.TestUtils;
using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public class TerrainShapeDbPerformanceTestDEO : MonoBehaviour
    {
        public void Start()
        {
            var db = new TerrainShapeDbUnderTest(false);
            QueryTerrain(db);
        }

        public void QueryTerrain(TerrainShapeDbUnderTest db)
        {
            var x = db.ShapeDb.QueryAsync(new TerrainDescriptionQuery()
            {
                QueryArea = new MyRectangle(90 * 10, 90 * 10, 90f, 90f),
                RequestedElementDetails = new List<TerrainDescriptionQueryElementDetail>()
                {
                    new TerrainDescriptionQueryElementDetail()
                    {
                        Resolution = TerrainCardinalResolution.MIN_RESOLUTION,
                        Type = TerrainDescriptionElementTypeEnum.HEIGHT_ARRAY
                    }
                }
            }).Result;
        }
    }
}
