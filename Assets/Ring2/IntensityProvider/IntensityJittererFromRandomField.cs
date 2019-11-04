using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Random.Fields;
using Assets.Ring2.RegionSpace;
using Assets.Utils;
using UnityEngine;

namespace Assets.Ring2.IntensityProvider
{
    public class IntensityJittererFromRandomField : IFabricRing2IntensityProvider
    {
        private readonly ValuesFromRandomFieldProvider _provider;
        private readonly Vector2 _multiplier;
        private readonly IFabricRing2IntensityProvider _internalIntensityProvider;

        public IntensityJittererFromRandomField(ValuesFromRandomFieldProvider provider, Vector2 multiplier,
            IFabricRing2IntensityProvider internalIntensityProvider)
        {
            _provider = provider;
            _multiplier = multiplier;
            _internalIntensityProvider = internalIntensityProvider;
        }

        public async Task<List<float>> RetriveIntensityAsync(List<Vector2> queryPositions, IRegionSpace space)
        {
            List<float> oldIntensities = await _internalIntensityProvider.RetriveIntensityAsync(queryPositions, space);
            var retriveIntensityAsync = await CalculateNewIntensities(queryPositions, oldIntensities);
            return retriveIntensityAsync;
        }

        public async Task<List<float>> RetriveIntensityAsync(List<Vector2> queryPositions)
        {
            List<float> oldIntensities = await _internalIntensityProvider.RetriveIntensityAsync(queryPositions);
            var calculateNewIntensities = await CalculateNewIntensities(queryPositions, oldIntensities);
            return calculateNewIntensities;
        }

        private async Task<List<float>> CalculateNewIntensities(List<Vector2> queryPositions,
            List<float> oldIntensities)
        {
            List<float> jitterPayload = await _provider.ComputeValuesAsync(queryPositions);
            List<float> output = new List<float>(queryPositions.Count);
            for (int i = 0; i < queryPositions.Count; i++)
            {
                output.Add(JitterValue(oldIntensities[i], jitterPayload[i], queryPositions[i]));
            }
            return output;
        }

        private float JitterValue(float oldIntensity, float jitterInput, Vector2 position)
        {
            float jitterValue = Mathf.Lerp(_multiplier[0], _multiplier[1], jitterInput);
            return Mathf.Clamp01(jitterValue * oldIntensity);
        }

        public bool RequiresRegionToCompute()
        {
            return _internalIntensityProvider.RequiresRegionToCompute();
        }
    }
}