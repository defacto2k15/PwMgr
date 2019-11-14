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
using Assets.Repositioning;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass2.Growing
{
    public class GrassGroupsGrower
    {
        private readonly GrassGroupsPlanter _grassGroupsPlanter;
        private readonly IGrassIntensityMapProvider _grassIntensityMapProvider;
        private readonly List<GrassType> _supportedGrassTypes;
        private Repositioner _providerGenerationRepositioner; //TODO why we must use repositioner?

        public GrassGroupsGrower(GrassGroupsPlanter grassGroupsPlanter,
            IGrassIntensityMapProvider grassIntensityMapProvider, List<GrassType> supportedGrassTypes,  Repositioner providerGenerationRepositioner)
        {
            _grassGroupsPlanter = grassGroupsPlanter;
            _grassIntensityMapProvider = grassIntensityMapProvider;
            _supportedGrassTypes = supportedGrassTypes;
            _providerGenerationRepositioner = providerGenerationRepositioner;
        }

        public async Task<GrassBandInfo> GrowGrassBandAsync(MyRectangle growingArea)
        {
            var retrived = (await _grassIntensityMapProvider.ProvideMapsAtAsync(_providerGenerationRepositioner.Move(growingArea)));
            List<GrassTypeWithUvedIntensity> grassTypeIntensities = retrived.CoordedPart.Part
                .Select(c => new GrassTypeWithUvedIntensity()
                {
                    Type = c.GrassType,
                    Figure = new IntensityFieldFigureWithUv()
                    {
                        FieldFigure = c.IntensityFigure,
                        Uv = retrived.Uv
                    }
                })
                .Where(c=> _supportedGrassTypes.Contains(c.Type))
                .ToList();

            var groupIds = new List<GrassGroupId>();
            foreach (var intensityData in grassTypeIntensities)
            {
                var intensityProvider =
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