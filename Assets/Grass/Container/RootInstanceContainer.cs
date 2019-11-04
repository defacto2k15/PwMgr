using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ShaderUtils;
using UnityEngine;

namespace Assets.Grass.Container
{
    class RootInstanceContainer : IGrassInstanceContainer
    {
        public Dictionary<ContainerType, IGrassInstanceContainer> Containers =
            new Dictionary<ContainerType, IGrassInstanceContainer>();

        public RootInstanceContainer(GameObjectGrassInstanceContainer gameObjectContainer,
            GpuInstancingGrassInstanceContainer gpuInstancingGrassContainer)
        {
            Containers.Add(ContainerType.GameObject, gameObjectContainer);
            Containers.Add(ContainerType.Instancing, gpuInstancingGrassContainer);
        }


        public void Draw()
        {
            foreach (var @container in Containers.Values)
            {
                @container.Draw();
            }
        }

        public void SetGlobalColor(string name, Color value)
        {
            foreach (var @container in Containers.Values)
            {
                @container.SetGlobalColor(name, value);
            }
        }

        public void SetGlobalUniform(ShaderUniformName name, float value)
        {
            foreach (var @container in Containers.Values)
            {
                @container.SetGlobalUniform(name, value);
            }
        }

        public void SetGlobalUniform(ShaderUniformName name, Vector4 value)
        {
            foreach (var @container in Containers.Values)
            {
                @container.SetGlobalUniform(name, value);
            }
        }
    }
}