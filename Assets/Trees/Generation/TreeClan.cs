using System.Collections.Generic;

namespace Assets.Trees.Generation
{
    public class TreeClan
    {
        private List<TreePyramid> _pyramids;

        public TreeClan(List<TreePyramid> pyramids)
        {
            _pyramids = pyramids;
        }

        public List<TreePyramid> Pyramids
        {
            get { return _pyramids; }
        }
    }
}