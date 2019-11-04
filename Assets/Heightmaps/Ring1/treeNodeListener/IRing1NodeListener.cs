using UnityEngine;

namespace Assets.Heightmaps.Ring1.treeNodeListener
{
    public interface IRing1NodeListener
    {
        void CreatedNewNode(Ring1Node ring1Node);
        void DoNotDisplay(Ring1Node ring1Node);
        void Update(Ring1Node ring1Node, Vector3 cameraPosition);

        void EndBatch();
    }
}