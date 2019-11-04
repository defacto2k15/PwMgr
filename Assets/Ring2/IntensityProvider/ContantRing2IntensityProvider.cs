using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.RegionSpace;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Ring2.IntensityProvider
{
    public class ContantRing2IntensityProvider : IFabricRing2IntensityProvider
    {
        public Task<List<float>> RetriveIntensityAsync(List<Vector2> queryPositions)
        {
            return TaskUtils.MyFromResult(queryPositions.Select(c => 1f).ToList());
        }

        public bool RequiresRegionToCompute()
        {
            return false;
        }

        public Task<List<float>> RetriveIntensityAsync(List<Vector2> queryPositions, IRegionSpace region)
        {
            return RetriveIntensityAsync(queryPositions);
        }
    }
}