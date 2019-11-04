using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.TerrainMat.Stain;
using Assets.Utils;

namespace Assets.Heightmaps.GRing
{
    public interface INewGRingListenersCreator
    {
        IAsyncGRingNodeListener CreateNewListener(Ring1Node node, FlatLod lod);
    }
}