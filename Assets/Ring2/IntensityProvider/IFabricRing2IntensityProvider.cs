using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Ring2.BaseEntities;
using Assets.Ring2.RegionSpace;
using UnityEngine;

namespace Assets.Ring2.IntensityProvider
{
    public interface IFabricRing2IntensityProvider
    {
        Task<List<float>> RetriveIntensityAsync(List<Vector2> queryPositions, IRegionSpace space);

        Task<List<float>> RetriveIntensityAsync(List<Vector2> queryPositions);

        bool RequiresRegionToCompute();
    }
}