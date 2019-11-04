using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Assets.Utils;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer
{
    public class GpuInstanceRenderingDataElementPackDelta
    {
        private Dictionary<int, GpuInstanceRenderingDataElementDelta> _addedElements =
            new Dictionary<int, GpuInstanceRenderingDataElementDelta>();

        private Dictionary<int, GpuInstanceRenderingDataElementDelta> _modifiedElements =
            new Dictionary<int, GpuInstanceRenderingDataElementDelta>();

        private List<int> _removedElements = new List<int>();
        private bool _anythingThere = false;

        public List<int> GetActiveBlockKeys()
        {
            return _addedElements.Keys.Union(_modifiedElements.Keys).ToList();
        }

        public void AddRemovedBlock(Int32 id)
        {
            if (!_addedElements.ContainsKey(id))
            {
                _addedElements.Remove(id);
            }
            if (!_modifiedElements.ContainsKey(id))
            {
                _modifiedElements.Remove(id);
            }
            _removedElements.Add(id);
            _anythingThere = true;
        }

        public GpuInstanceRenderingDataElementDelta GetDeltaPackFor(Int32 key)
        {
            if (_modifiedElements.ContainsKey(key))
            {
                return _modifiedElements[key];
            }
            var newDelta = new GpuInstanceRenderingDataElementDelta();
            _modifiedElements.Add(key, newDelta);
            _anythingThere = true;
            return newDelta;
        }

        public GpuInstanceRenderingDataElementDelta GetDeltaPackForNew(Int32 newBlockId)
        {
            Preconditions.Assert(!_addedElements.ContainsKey(newBlockId),
                "Block of id " + newBlockId + " is arleady added");
            if (_modifiedElements.ContainsKey(newBlockId))
            {
                _modifiedElements.Remove(newBlockId);
            }
            var newDelta = new GpuInstanceRenderingDataElementDelta();
            _addedElements[newBlockId] = newDelta;
            _anythingThere = true;
            return newDelta;
        }

        public void ResetDelta()
        {
            //UnityEngine.Debug.Log("T92 Resetting delta. Added elements: "+_addedElements.Count+" _modified "+_modifiedElements.Count+" removed "+_removedElements.Count);
            if (_anythingThere)
            {
                _addedElements.Clear();
                _modifiedElements.Clear();
                _removedElements.Clear();
            }
            _anythingThere = false;
        }

        public Dictionary<int, GpuInstanceRenderingDataElementDelta> AddedElements
        {
            get { return _addedElements; }
        }

        public Dictionary<int, GpuInstanceRenderingDataElementDelta> ModifiedElements
        {
            get { return _modifiedElements; }
        }

        public List<int> RemovedElements
        {
            get { return _removedElements; }
        }

        public bool AnythingThere => _anythingThere;
    }
}