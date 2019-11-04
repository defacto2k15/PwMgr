using UnityEngine;

namespace Assets.Trees.Generation
{
    public class TreePyramid
    {
        private Tree _fullDetailTree;
        private Tree _simplifiedTree;
        private BillboardCollageTexture _collageTexture;

        public TreePyramid(Tree fullDetailTree, Tree simplifiedTree, BillboardCollageTexture collageTexture)
        {
            _fullDetailTree = fullDetailTree;
            _simplifiedTree = simplifiedTree;
            _collageTexture = collageTexture;
        }

        public Tree FullDetailTree
        {
            get { return _fullDetailTree; }
        }

        public Tree SimplifiedTree
        {
            get { return _simplifiedTree; }
        }

        public BillboardCollageTexture CollageTexture
        {
            get { return _collageTexture; }
        }
    }
}