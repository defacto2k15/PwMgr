using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass.Container
{
    class GpuInstancingEntitySplat : IEntitySplat
    {
        private SplatInfo _splatInfo;
        private readonly List<InstancesPack> _grassPacks;
        private int? _splatId;
        private readonly GpuInstancingGrassInstanceContainer _gpuInstancingGrassInstanceContainer;

        public GpuInstancingEntitySplat(SplatInfo splatInfo, List<InstancesPack> GrassPacks,
            GpuInstancingGrassInstanceContainer gpuInstancingGrassInstanceContainer, int? splatId = null)
        {
            _splatInfo = splatInfo;
            _grassPacks = GrassPacks;
            _gpuInstancingGrassInstanceContainer = gpuInstancingGrassInstanceContainer;
            _splatId = splatId;
        }

        public IEnumerable<InstancesPack> GrassPacks
        {
            get { return _grassPacks; }
        }

        public Mesh Mesh
        {
            get { return _splatInfo.Mesh; }
        }

        public Material Material
        {
            get { return _splatInfo.Material; }
        }

        public void Remove()
        {
            Preconditions.Assert(_splatId.HasValue, "There is no splatId value set - cant remove splat");
            _gpuInstancingGrassInstanceContainer.RemoveSplat(_splatId.Value);
        }

        public IEntitySplat Copy()
        {
            return new GpuInstancingEntitySplat(_splatInfo, _grassPacks, _gpuInstancingGrassInstanceContainer);
        }

        public void SetMesh(Mesh newMesh)
        {
            _splatInfo = new SplatInfo(newMesh, _splatInfo.Material);
        }

        public void Enable()
        {
            if (!_splatId.HasValue)
            {
                _gpuInstancingGrassInstanceContainer.AddSplat(this);
            }
        }

        public void SetSplatId(int splatId)
        {
            _splatId = splatId;
        }
    }
}