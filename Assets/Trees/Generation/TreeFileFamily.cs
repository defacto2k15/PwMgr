using System.Collections.Generic;
using UnityEngine;

namespace Assets.Trees.Generation
{
    public class TreeFileFamily
    {
        private readonly List<Tree> _treesInFamily;
        private readonly string _familyName;

        public TreeFileFamily(List<Tree> treesInFamily, string familyName)
        {
            _treesInFamily = treesInFamily;
            _familyName = familyName;
        }

        public List<Tree> TreesInFamily
        {
            get { return _treesInFamily; }
        }

        public string FamilyName
        {
            get { return _familyName; }
        }
    }
}