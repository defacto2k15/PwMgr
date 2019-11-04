using System;
using System.Collections.Generic;
using System.Linq;
using Assets.ShaderUtils;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread
{
    public class UnityThreadGpuInstanceRenderingDataElementPack
    {
        private readonly UniformsPack _commonUniformsPack;

        private Dictionary<int, UnityThreadGpuInstanceRenderingDataElement> _elements =
            new Dictionary<int, UnityThreadGpuInstanceRenderingDataElement>();

        public UnityThreadGpuInstanceRenderingDataElementPack(UniformsPack commonUniformsPack)
        {
            _commonUniformsPack = commonUniformsPack;
        }

        private List<UnityThreadGpuInstanceRenderingDataElement> _elementsCached =
            new List<UnityThreadGpuInstanceRenderingDataElement>();

        public List<UnityThreadGpuInstanceRenderingDataElement> Elements
        {
            get { return _elementsCached; }
        }

        public void ApplyDelta(GpuInstanceRenderingDataElementPackDelta delta)
        {
            if (delta.AnythingThere)
            {
                MM1(delta);
                MM2(delta);
                MM3(delta);
                MM4();
            }
        }

        private void MM4()
        {
            _elementsCached = _elements.Values.ToList();
        }

        private void MM3(GpuInstanceRenderingDataElementPackDelta delta)
        {
            foreach (var modified in delta.ModifiedElements)
            {
                _elements[modified.Key].ApplyDelta(modified.Value);
            }
        }

        private void MM2(GpuInstanceRenderingDataElementPackDelta delta)
        {
            foreach (var added in delta.AddedElements)
            {
                _elements[added.Key] = CreateDataElementFromDelta(added.Value, added.Key);
            }
        }

        private void MM1(GpuInstanceRenderingDataElementPackDelta delta)
        {
            foreach (var removed in delta.RemovedElements)
            {
                _elements.Remove(removed);
            }
        }

        private UnityThreadGpuInstanceRenderingDataElement CreateDataElementFromDelta(
            GpuInstanceRenderingDataElementDelta delta, Int32 key)
        {
            var dataElement = new UnityThreadGpuInstanceRenderingDataElement(key, _commonUniformsPack);
            dataElement.ApplyDelta(delta);
            return dataElement;
        }
    }
}