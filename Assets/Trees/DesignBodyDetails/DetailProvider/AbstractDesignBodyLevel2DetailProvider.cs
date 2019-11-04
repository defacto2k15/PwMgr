using Assets.ShaderUtils;
using Assets.Trees.DesignBodyDetails.MyRandom;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails.DetailProvider
{
    public abstract class AbstractDesignBodyLevel2DetailProvider
    {
        public virtual void AddDetailsFor(DesignBodyLevel2Detail currentLevel2Detail,
            DesignBodyLevel1Detail level1Detail, MyRandomProvider randomGenerator)
        {
            currentLevel2Detail.MergeWith(GetDetailsFor(level1Detail, randomGenerator));
        }

        protected virtual DesignBodyLevel2Detail GetDetailsFor(DesignBodyLevel1Detail level1Detail,
            MyRandomProvider randomGenerator)
        {
            return new DesignBodyLevel2Detail(Vector3.zero, Quaternion.identity, Vector3.one, new UniformsPack());
        }
    }
}