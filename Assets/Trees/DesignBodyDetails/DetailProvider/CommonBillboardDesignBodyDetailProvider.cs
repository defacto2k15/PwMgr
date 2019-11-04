using Assets.Trees.DesignBodyDetails.MyRandom;
using UnityEngine;

namespace Assets.Trees.DesignBodyDetails.DetailProvider
{
    public class CommonBillboardDesignBodyDetailProvider : AbstractDesignBodyLevel2DetailProvider
    {
        protected override DesignBodyLevel2Detail GetDetailsFor(DesignBodyLevel1Detail level1Detail,
            MyRandomProvider random)
        {
            var heightScale = random.FloatValueRange(StringSeed.HeightScale, 2, 4);
            return new DesignBodyLevel2Detail()
            {
                Scale = new Vector3(1, heightScale, 1)
            };
        }
    }
}