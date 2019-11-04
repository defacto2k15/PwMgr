using System.Text;
using Assets.Utils;
using UnityEngine;

namespace Assets.TerrainMat
{
    public class DebugBiomeContainerGenerator
    {
        public BiomeInstancesContainer GenerateBiomesContainer(BiomesContainerConfiguration containerConfiguration)
        {
            var container = new BiomeInstancesContainer(containerConfiguration);
            //container.AddBiome( new BiomeInfo(BiomeType.Grass, 
            //    Polygon.Create( 
            //        new Vector2(0.2f, 0.3f),
            //        new Vector2(0.2f, 0.6f),
            //        new Vector2(0.6f, 0.6f),
            //        new Vector2( 0.6f, 0.2f)
            //    ), 3));
            container.AddBiome(new PolygonBiomeInstanceInfo(BiomeType.Forest,
                MyNetTopologySuiteUtils.ToPolygon(
                    new Vector2[]
                    {
                        new Vector2(0.5f, 0.0f + 0.1f),
                        new Vector2(1.0f - 0.1f, 0.5f),
                        new Vector2(0.5f, 1.0f - 0.1f),
                        new Vector2(0.0f + 0.1f, 0.5f)
                    }), new BiomeInstanceId(2), 0));
            return container;
        }
    }
}