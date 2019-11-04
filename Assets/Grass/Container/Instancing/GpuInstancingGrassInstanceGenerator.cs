using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Grass.Container;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass.Instancing
{
    class GpuInstancingGrassInstanceGenerator
    {
        private GpuGrassInstancesTemplate Generate(GrassEntitiesWithMaterials grassEntitiesWithMaterials)
        {
            //var ab = grassEntitiesWithMaterials.Entities.Select(c => c.InitialBendingValue).ToArray(); //todo
            List<ShaderUniform<float>> floatUniforms = new List<ShaderUniform<float>>();
            List<ShaderUniform<Vector4>> vector4Uniforms = new List<ShaderUniform<Vector4>>();

            foreach (var entity in grassEntitiesWithMaterials.Entities)
            {
                floatUniforms.AddRange(entity.GetFloatUniforms());
                vector4Uniforms.AddRange(entity.GetVector4Uniforms());
            }

            List<IUniformArray> uniformArrays =
                vector4Uniforms.GroupBy(c => c.Name).Select(c => new UniformArray<Vector4>(c.ToArray()))
                    .Cast<IUniformArray>().Union(
                        floatUniforms.GroupBy(c => c.Name).Select(c => new UniformArray<float>(c.ToArray()))
                            .Cast<IUniformArray>()).ToList();

            return new GpuGrassInstancesTemplate(
                grassEntitiesWithMaterials.Entities.Select(c => c.LocalToWorldMatrix).ToArray(), uniformArrays);
        }

        public GpuInstancingEntitySplat GenerateEntitySplats(GrassEntitiesWithMaterials grassEntitiesWithMaterials,
            GpuInstancingGrassInstanceContainer container)
        {
            GpuGrassInstancesTemplate grassInstancesTemplate = Generate(grassEntitiesWithMaterials);

            var maticesArray = grassInstancesTemplate.TransformMatices;
            var uniformArrays = grassInstancesTemplate.UniformArrays;

            var newGrassPacks = new List<InstancesPack>();

            for (var i = 0; i < Math.Ceiling((float) maticesArray.Length / MyConstants.MaxInstancesPerPack); i++)
            {
                var elementsToSkipCount = i * MyConstants.MaxInstancesPerPack;
                var elementsToTakeCount = Math.Min(MyConstants.MaxInstancesPerPack,
                    maticesArray.Length - i * MyConstants.MaxInstancesPerPack);
                var packMaticesArray = maticesArray.Skip(elementsToSkipCount).Take(elementsToTakeCount).ToArray();
                MyMaterialPropertyBlock block = new MyMaterialPropertyBlock(elementsToTakeCount);
                foreach (var aUniformArray in uniformArrays)
                {
                    aUniformArray.AddToBlock(block.Block, elementsToSkipCount, elementsToTakeCount);
                }
                newGrassPacks.Add(new InstancesPack(packMaticesArray, block));
            }

            var splatInfo = new SplatInfo(grassEntitiesWithMaterials.Mesh, grassEntitiesWithMaterials.Material);
            var newGpuEntitySplat = new GpuInstancingEntitySplat(splatInfo, newGrassPacks, container);
            return newGpuEntitySplat;
        }
    }


    class GpuGrassInstancesTemplate
    {
        private readonly Matrix4x4[] _transformMatices;
        private readonly List<IUniformArray> _uniformArrays;

        public GpuGrassInstancesTemplate(Matrix4x4[] transformMatices, List<IUniformArray> uniformArrays)
        {
            _transformMatices = transformMatices;
            _uniformArrays = uniformArrays;
        }

        public Matrix4x4[] TransformMatices
        {
            get { return _transformMatices; }
        }

        public List<IUniformArray> UniformArrays
        {
            get { return _uniformArrays; }
        }
    }
}