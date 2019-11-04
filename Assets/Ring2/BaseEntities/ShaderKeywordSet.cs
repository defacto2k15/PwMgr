using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Ring2.BaseEntities
{
    public class ShaderKeywordSet
    {
        private List<string> _keywords;

        public ShaderKeywordSet(List<string> keywords = null)
        {
            if (keywords == null)
            {
                keywords = new List<string>();
            }
            _keywords = keywords.OrderBy(c => c).ToList();
        }

        public List<string> Keywords
        {
            get { return _keywords; }
        }

        protected bool Equals(ShaderKeywordSet other)
        {
            return Equals(_keywords, other._keywords);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ShaderKeywordSet) obj);
        }

        public override int GetHashCode()
        {
            return (_keywords != null ? _keywords.GetHashCode() : 0);
        }

        public void EnableInMaterial(Material material)
        {
            foreach (var keyword in _keywords)
            {
                material.EnableKeyword(keyword);
            }
        }

        public static ShaderKeywordSet Merge(ShaderKeywordSet set1, ShaderKeywordSet set2)
        {
            return new ShaderKeywordSet(set1.Keywords.Union(set2.Keywords).Distinct().ToList());
        }
    }
}