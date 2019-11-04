using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Assets.Ring2.Devising;
using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Threading;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;
using Assets.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using InstancesPortionId = System.Int32;
using InstancesElementId = System.Int32;
using CellId = System.Int32;
using Debug = UnityEngine.Debug;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing
{
    public class GpuInstancingVegetationSubjectContainer
    {
        private GpuInstancerCommonData _commonData;
        private ThreadedGpuInstancingBlocksContainer _threadedBlocksContainer;
        private UnityThreadGpuInstanceRenderingDataElementPack _dataElementPack;

        public GpuInstancingVegetationSubjectContainer(GpuInstancerCommonData commonData,
            GpuInstancingUniformsArrayTemplate uniformsArrayTemplate)
        {
            _commonData = commonData;
            _threadedBlocksContainer = new ThreadedGpuInstancingBlocksContainer(uniformsArrayTemplate);
            _dataElementPack = new UnityThreadGpuInstanceRenderingDataElementPack(_commonData.UniformsPack);
        }

        public CyclicJobWithWait RetriveCyclicJob()
        {
            return _threadedBlocksContainer.RetriveJob();
        }

        public void DrawFrame()
        {
            var delta = _threadedBlocksContainer.RenderingData.StartRendering();

            _dataElementPack.ApplyDelta(delta);

            foreach (var element in _dataElementPack.Elements)
            {
                if (element.UsedCellsCount > 0)
                {
                    Graphics.DrawMeshInstanced(_commonData.Mesh, _commonData.SubmeshIndex, _commonData.Material,
                        element.MaticesArray,
                        element.UsedCellsCount, element.Block, _commonData.CastShadows);
                }
            }

            _threadedBlocksContainer.RenderingData.StopRendering();
        }

        public GpuInstanceId AddInstance(Matrix4x4 matrix, UniformsPack uniformsPack)
        {
            return _threadedBlocksContainer.AddInstance(matrix, uniformsPack);
        }

        public void ModifyInstance(GpuInstanceId instanceId, Matrix4x4 matrix, UniformsPack uniformsPack)
        {
            _threadedBlocksContainer.ModifyInstance(instanceId, matrix, uniformsPack);
        }

        public void RemoveInstance(GpuInstanceId removedIdx)
        {
            _threadedBlocksContainer.RemoveInstance(removedIdx);
        }

        public void FinishUpdateBatch()
        {
            _threadedBlocksContainer.FinishUpdateBatch();
        }
    }
}