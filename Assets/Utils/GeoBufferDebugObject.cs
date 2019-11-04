using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Habitat;
using Assets.TerrainMat;
using GeoAPI.Operation.Buffer;
using NetTopologySuite.Operation.Buffer;
using UnityEngine;

namespace Assets.Utils
{
    public class GeoBufferDebugObject : MonoBehaviour
    {
        public void Start()
        {
            var fileManager = new HabitatMapFileManager();
            var map = fileManager.LoadHabitatMap(@"C:\inz\habitating2\");

            var firstHabitat = map.QueryAll().First();

            var geo = firstHabitat.Geometry;

            var bufObj = new BufferOp(geo, new BufferParameters(0, EndCapStyle.Square));

            var bufferedGeo = bufObj.GetResultGeometry(40);

            HabitatMapOsmLoaderDebugObject.CreateDebugHabitatField(firstHabitat, 0.01f);
            var ngo = new GameObject("MEGA");
            HabitatMapOsmLoaderDebugObject.CreateDebugHabitatField(new HabitatField()
            {
                Geometry = bufferedGeo,
                Type = HabitatType.Fell
            }, 0.01f, ngo);
        }
    }
}