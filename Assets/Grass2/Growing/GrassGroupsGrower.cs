using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Grass2.GrassIntensityMap;
using Assets.Grass2.Groups;
using Assets.Grass2.IntensitySampling;
using Assets.Grass2.Planting;
using Assets.Grass2.Types;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass2.Growing
{
    public class GrassGroupsGrower
    {
        private readonly GrassGroupsPlanter _grassGroupsPlanter;
        private readonly IGrassIntensityMapProvider _grassIntensityMapProvider;

        public GrassGroupsGrower(
            GrassGroupsPlanter grassGroupsPlanter,
            IGrassIntensityMapProvider grassIntensityMapProvider)
        {
            _grassGroupsPlanter = grassGroupsPlanter;
            _grassIntensityMapProvider = grassIntensityMapProvider;
        }

        public async Task<GrassBandInfo> GrowGrassBandAsync(MyRectangle growingArea)
        {
            var retrived = (await _grassIntensityMapProvider.ProvideMapsAtAsync(growingArea));
            List<GrassTypeWithUvedIntensity> grassTypeIntensities = retrived.CoordedPart.Part
                .Select(c => new GrassTypeWithUvedIntensity()
                {
                    Type = c.GrassType,
                    Figure = new IntensityFieldFigureWithUv()
                    {
                        FieldFigure = c.IntensityFigure,
                        Uv = retrived.Uv
                    }
                }).ToList();

            var groupIds = new List<GrassGroupId>();
            foreach (var intensityData in grassTypeIntensities)
            {
                IIntensitySamplingProvider intensityProvider =
                    new IntensityFromRandomFigureProvider(intensityData.Figure);
                var newGroupId = _grassGroupsPlanter.AddGrassGroup(growingArea, intensityData.Type, intensityProvider);
                groupIds.Add(newGroupId);
            }

            return new GrassBandInfo()
            {
                GroupIds = groupIds
            };
        }

        public void RemoveGrassBand(GrassBandInfo info)
        {
            foreach (var id in info.GroupIds)
            {
                _grassGroupsPlanter.RemoveGroup(id);
            }
        }
    }
}