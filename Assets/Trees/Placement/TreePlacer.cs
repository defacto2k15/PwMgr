using UnityEngine;

namespace Assets.Trees.Placement
{
    public class TreePlacer
    {
        private TreeInstanter _instanter;

        public TreePlacer(TreeInstanter instanter)
        {
            _instanter = instanter;
        }

        public void PlaceTrees(Tree treeTemplate)
        {
            int count = 50;
            for (int x = -count; x <= count; x++)
            {
                for (int z = -count; z <= count; z++)
                {
                    _instanter.InstantiateTreePrefab(treeTemplate, new Vector3(x * 10f, 0, z * 10f),
                        Quaternion.Euler(0, 0, 0));
                }
            }
        }
    }
}