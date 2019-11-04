using System.Threading;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer
{
    public class GpuInstanceRenderingBoxData
    {
        private object _activePackLock = new object();
        private GpuInstanceRenderingDataElementPackPair PackPair = new GpuInstanceRenderingDataElementPackPair();

        public GpuInstanceRenderingDataElementPackDelta StartRendering()
        {
            Monitor.Enter(_activePackLock);
            return PackPair.ActivePack;
        }

        public void StopRendering()
        {
            PackPair.StopRendering();
            Monitor.Exit(_activePackLock);
        }

        public void StartUpdating()
        {
        }

        public void StopUpdating()
        {
            PackPair.EndOfDataRecieving();
            Monitor.Enter(_activePackLock);
            PackPair.SynchronizePacks();
            Monitor.Exit(_activePackLock);
        }

        public RenderingDataOfBlockReciever GetDataRecieverFor(GpuInstancingBlock block)
        {
            return PackPair.GetDataRecieverFor(block);
        }
    }
}