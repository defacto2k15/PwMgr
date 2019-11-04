using System.Collections.Generic;
using Assets.Heightmaps.submaps;
using Assets.Heightmaps.TerrainObjectCreator;

namespace Assets.Heightmaps
{
    class MyTotalHeightmap
    {
        public void LoadHeightmap(List<Submap> submaps)
        {
            Ring0TerrainObjectCreator creator = new Ring0TerrainObjectCreator();
            creator.CreatingRing0TerrainObjects(submaps);
        }
    }
}