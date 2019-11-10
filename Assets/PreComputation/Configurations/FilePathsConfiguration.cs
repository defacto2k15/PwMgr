using System.IO;
using UnityEngine;

namespace Assets.PreComputation.Configurations
{
    public class FilePathsConfiguration
    {
        /////////////
        public string HabitatDbFilePath => PathBase + @"habitating2\";

        ////////////// Roads
        public string OsmFilePath => PathBase + @"osm\midMap.osm";

        /////////////// VegetationDatabase
        public string VegetationDatabaseFilePath => PathBase + @"db1.json";

        public string LoadingVegetationDatabaseDictionaryPath => PathBase + @"dbs2";

        public string StainTerrainServicePath => PathBase + @"ring1\";

        public string PathsPath => PathBase + @"wrtC\";

        public string Grass2BillboardsPath => PathBase + @"billboards\";

        public string HeightmapFilePath => PathBase + @"allTerrainF1.png";

        public string TerrainDetailCachePath => PathBase + @"unityCache\";
        public string SurfacePatchCachePath => PathBase + @"surfaceCache\";
        public string ManualRing1TexturePath => PathBase + @"colorPlay\geoHand2.png";
        public string ColorPaletteFilePath => PathBase + @"colorPlay\cPal3.png";
        public string TreeCompletedClanDirectiory = "Assets/treePrefabs/completedGenerated2";

        public string PathBase
        {
            get
            {
                return Directory.GetCurrentDirectory() + @"\precomputedResources\";
            }
        }
    }
}