using System.Collections.Generic;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer
{
    public class GpuInstanceRenderingDataElementPackPair
    {
        private GpuInstanceRenderingDataElementPackDelta _activePack = new GpuInstanceRenderingDataElementPackDelta();

        Dictionary<int, RenderingDataOfBlockReciever> _recieversDict =
            new Dictionary<int, RenderingDataOfBlockReciever>();

        Dictionary<int, bool> _usagesPerUpdateDict = new Dictionary<int, bool>();
        List<int> _addedBlocks = new List<int>();

        public GpuInstanceRenderingDataElementPackDelta ActivePack
        {
            get { return _activePack; }
        }

        public void EndOfDataRecieving()
        {
            foreach (var key in _usagesPerUpdateDict.Keys)
            {
                if (!_usagesPerUpdateDict[key])
                {
                    _usagesPerUpdateDict.Remove(key);
                    _recieversDict.Remove(key);
                }
            }
        }

        public void SynchronizePacks()
        {
            var oldDictionaryKeys = _activePack.GetActiveBlockKeys();

            foreach (var key in oldDictionaryKeys)
            {
                if (!_recieversDict.ContainsKey(key)) // block was removed
                {
                    _activePack.AddRemovedBlock(key);
                }
            }

            foreach (var pair in _recieversDict)
            {
                var blockId = pair.Key;
                if (_addedBlocks.Contains(blockId)) //adding new block
                {
                    _recieversDict[blockId].MoveData(_activePack.GetDeltaPackForNew(blockId));
                }
                else // apply modifications
                {
                    _recieversDict[blockId].MoveData(_activePack.GetDeltaPackFor(blockId));
                }
            }

            _addedBlocks.Clear();
        }

        public RenderingDataOfBlockReciever GetDataRecieverFor(GpuInstancingBlock block)
        {
            if (!_recieversDict.ContainsKey(block.Id))
            {
                _recieversDict.Add(block.Id, new RenderingDataOfBlockReciever());
                _usagesPerUpdateDict.Add(block.Id, true);
                _addedBlocks.Add(block.Id);
            }

            _usagesPerUpdateDict[block.Id] = true;
            return _recieversDict[block.Id];
        }

        public void StopRendering()
        {
            _activePack.ResetDelta();
        }
    }
}