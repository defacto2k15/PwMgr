using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Grass2.Growing;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.RuntimeManagement;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass2
{
    public class Grass2RuntimeManager
    {
        private GrassGroupsGrower _grassGroupsGrower;
        private Grass2RuntimeManagerConfiguration _configuration;
        private Dictionary<int, GrassBandInfo> _entityToGrassBand = new Dictionary<int, GrassBandInfo>();

        public Grass2RuntimeManager(GrassGroupsGrower grassGroupsGrower,
            Grass2RuntimeManagerConfiguration configuration)
        {
            _grassGroupsGrower = grassGroupsGrower;
            _configuration = configuration;
        }

        public async Task AddInstancingOrderAsync(
            VegetationDetailLevel level,
            List<VegetationSubjectEntity> gainedEntities,
            List<VegetationSubjectEntity> lostEntities)
        {
            foreach (var entity in gainedEntities)
            {
                Preconditions.Assert(entity.Detail.SpeciesEnum == VegetationSpeciesEnum.Grass2SpotMarker,
                    $"Given entity is not of type spotMarker. It is {entity.Detail.SpeciesEnum}");
                var position = entity.Position2D;

                var generationArea = MyRectangle.CenteredAt(position, _configuration.GroupSize);
                var grassBandInfo = await _grassGroupsGrower.GrowGrassBandAsync(generationArea);

                _entityToGrassBand[entity.Id] = grassBandInfo;
            }
            foreach (var entity in lostEntities)
            {
                var id = entity.Id;
                var bandInfo = _entityToGrassBand[id];
                _entityToGrassBand.Remove(id);
                _grassGroupsGrower.RemoveGrassBand(bandInfo);
            }
        }

        public class Grass2RuntimeManagerConfiguration
        {
            public Vector2 GroupSize;
        }
    }
}