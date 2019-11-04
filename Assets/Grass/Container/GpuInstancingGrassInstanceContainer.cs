using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Assets.Grass.Instancing;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using SplatId = System.Int32;

namespace Assets.Grass.Container
{
    class GpuInstancingGrassInstanceContainer : IGrassInstanceContainer
    {
        private readonly Dictionary<SplatId, GpuInstancingEntitySplat> _entityPacks =
            new Dictionary<SplatId, GpuInstancingEntitySplat>();

        private SplatId _lastSplatId = 0;

        public void AddSplat(GpuInstancingEntitySplat splat)
        {
            var splatId = _lastSplatId++;
            splat.SetSplatId(splatId);
            _entityPacks[splatId] = splat;
        }

        public void Draw()
        {
            foreach (var entity in _entityPacks.Values)
            {
                foreach (var pack in entity.GrassPacks)
                {
                    Graphics.DrawMeshInstanced(entity.Mesh, 0, entity.Material, pack.MaticesArray, pack.InstancesCount,
                        pack.MyBlock.Block,
                        pack.CastShadows, false, 0, null);
                }
            }
        }


        public void SetGlobalColor(string name, Color value)
        {
            ForeachObject(aGrassPack => aGrassPack.MyBlock.AddSingleVectorArray(name, value));
        }

        public void SetGlobalUniform(ShaderUniformName name, float value)
        {
            ForeachObject(aGrassPack => aGrassPack.MyBlock.AddGlobalUniform(name.ToString(), value));
        }

        public void SetGlobalUniform(ShaderUniformName name, Vector4 value)
        {
            ForeachObject(aGrassPack => aGrassPack.MyBlock.AddGlobalUniform(name.ToString(), value));
        }

        private void ForeachObject(Action<InstancesPack> action)
        {
            foreach (var entity in _entityPacks.Values)
            {
                foreach (var pack in entity.GrassPacks)
                {
                    action(pack);
                }
            }
        }

        public void RemoveSplat(int splatId)
        {
            _entityPacks.Remove(splatId);
        }
    }

    public class MyMaterialPropertyBlock
    {
        private int _arraySize;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        public MyMaterialPropertyBlock(int arraySize)
        {
            _arraySize = arraySize;
        }

        public void AddVectorArrayToBlock(IUniformArray uniformArray, int elementsToSkipCount, int elementsToTakeCount)
        {
            Preconditions.Assert(uniformArray.Count == _arraySize,
                string.Format("Cant add vector of length {0} to block, as block length is {1}", uniformArray.Count,
                    _arraySize));
            uniformArray.AddToBlock(_block, elementsToSkipCount, elementsToTakeCount);
        }

        public void AddSingleVectorArray(string name, Vector4 value)
        {
            var newValueArray = Enumerable.Repeat(value, _arraySize).ToArray();
            _block.SetVectorArray(name, newValueArray);
        }

        public MaterialPropertyBlock Block
        {
            get { return _block; }
        }

        public void AddGlobalUniform(string name, float value)
        {
            var newValueArray = Enumerable.Repeat(value, _arraySize).ToArray();
            _block.SetFloatArray(name, newValueArray);
        }

        public void AddGlobalUniform(string name, Vector4 value)
        {
            var newValueArray = Enumerable.Repeat(value, _arraySize).ToArray();
            _block.SetVectorArray(name, newValueArray);
        }
    }

    public class UniformArray<T> : IUniformArray
    {
        private ShaderUniform<T>[] _uniformsArray;

        public UniformArray(ShaderUniform<T>[] uniformsArray)
        {
            _uniformsArray = uniformsArray;
        }

        public void AddToBlock(MaterialPropertyBlock block, int elementsToSkipCount, int elementsToTakeCount)
        {
            var values = _uniformsArray.Skip(elementsToSkipCount).Take(elementsToTakeCount).Select(c => c.Get())
                .ToArray();
            var name = _uniformsArray[0].Name;
            if (typeof(T) == typeof(float))
            {
                block.SetFloatArray(name, values.Cast<float>().ToArray());
            }
            else if (typeof(T) == typeof(Vector4) || typeof(T) == typeof(Color))
            {
                block.SetVectorArray(name, values.Cast<Vector4>().ToArray());
            }
        }

        public int Count
        {
            get { return _uniformsArray.Length; }
        }
    }

    public interface IUniformArray
    {
        void AddToBlock(MaterialPropertyBlock block, int elementsToSkipCount, int elementsToTakeCount);
        int Count { get; }
    }

    public class InstancesPack
    {
        public InstancesPack(Matrix4x4[] maticesArray, MyMaterialPropertyBlock propertiesBlock)
        {
            Preconditions.Assert(maticesArray.Length <= MyConstants.MaxInstancesPerPack,
                String.Format("In grass pack there can be at max {0} elements, but is {1}",
                    MyConstants.MaxInstancesPerPack, maticesArray.Length));

            CastShadows = ShadowCastingMode.Off;
            MaticesArray = maticesArray;
            InstancesCount = maticesArray.Length;
            MyBlock = propertiesBlock;
        }

        public ShadowCastingMode CastShadows { get; private set; }
        public Matrix4x4[] MaticesArray { get; private set; }
        public int InstancesCount { get; private set; }

        public MyMaterialPropertyBlock MyBlock { get; private set; }
    }

    internal class SplatInfo
    {
        private Mesh _mesh;
        private Material _material;

        public SplatInfo(Mesh mesh, Material material)
        {
            this._mesh = mesh;
            this._material = material;
        }

        public Mesh Mesh
        {
            get { return _mesh; }
        }

        public Material Material
        {
            get { return _material; }
        }
    }
}