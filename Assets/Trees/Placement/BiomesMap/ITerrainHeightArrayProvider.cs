using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.TerrainDescription;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.Placement.BiomesMap
{
    public interface ITerrainHeightArrayProvider
    {
        TerrainHeightArrayWithUvBase RetriveTerrainHeightInfo(MyRectangle queryArea,
            TerrainCardinalResolution resolution);
    }

    public class DebugTerrainHeightArrayProvider : ITerrainHeightArrayProvider
    {
        private readonly MySimpleArray<float> _heightArray;
        private readonly MyRectangle _uvBase;

        public DebugTerrainHeightArrayProvider(Texture2D texture2D, MyRectangle uvBase)
        {
            _heightArray = HeightmapUtils.EncodedHeightToArray(texture2D);
            _uvBase = uvBase;
        }

        public TerrainHeightArrayWithUvBase RetriveTerrainHeightInfo(
            MyRectangle queryArea,
            TerrainCardinalResolution resolution)
        {
            return new TerrainHeightArrayWithUvBase()
            {
                UvBase = _uvBase,
                HeightArray = _heightArray
            };
        }
    }

    public class TerrainHeightArrayWithUvBase
    {
        public MySimpleArray<float> HeightArray;
        public MyRectangle UvBase;
    }
}