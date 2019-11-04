using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Grass;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Transfer;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;
using Assets.Utils;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread
{
    public class GpuInstancingBlocksContainer
    {
        private List<GpuInstancingBlock> _instancingBlocks = new List<GpuInstancingBlock>();

        private Dictionary<int, List<GpuInstanceId>> _instanceIdsDictionary =
            new Dictionary<int, List<GpuInstanceId>>();

        private GpuInstancingUniformsArrayTemplate _uniformsArrayTemplate;

        public GpuInstancingBlocksContainer(GpuInstancingUniformsArrayTemplate uniformsArrayTemplate)
        {
            _uniformsArrayTemplate = uniformsArrayTemplate;
        }

        public IEnumerable<GpuInstancingBlock> ActiveBlocks
        {
            get { return _instancingBlocks.Where(c => c != null); }
        }

        private void HousekeepingUpdate()
        {
            var maxPercentUsedAfterMerge = 0.8f;
            var smallBlockPercentageUsage = 0.5f;

            var smallBlocks =
                _instanceIdsDictionary
                    .Where(c => ((float) c.Value.Count) / MyConstants.MaxInstancesPerPack < smallBlockPercentageUsage)
                    .OrderByDescending(c => c.Value.Count)
                    .ToList();
            if (smallBlocks.Count > 1)
            {
                var smallest = smallBlocks[0];
                var bigger = smallBlocks[1];
                var countAfterMerge = smallest.Value.Count + bigger.Value.Count;
                var usedPercentage = ((float) countAfterMerge) / MyConstants.MaxInstancesPerPack;
                if (usedPercentage < maxPercentUsedAfterMerge)
                {
                    MergeBlocks(smallest, bigger);
                }
            }
        }

        private void MergeBlocks(KeyValuePair<int, List<GpuInstanceId>> small,
            KeyValuePair<int, List<GpuInstanceId>> big)
        {
            for (int i = 0; i < small.Value.Count; i++)
            {
                var id = small.Value[i];
                id.FillFrom(big.Key, big.Value.Count);
                big.Value.Add(id);
                _instancingBlocks[small.Key].MoveToOtherBlock(i, _instancingBlocks[big.Key]);
            }
            _instanceIdsDictionary.Remove(small.Key);
            _instancingBlocks[small.Key] = null;

            Precondition(big.Key);
        }

        public void Update(GpuInstancingBlocksContainerChangeOrder changeOrder,
            GpuInstanceRenderingBoxData renderingData)
        {
            if (!changeOrder.AnythingThere)
            {
                return;
            }
            var addingOrders = changeOrder.AddingOrders;
            var removingOrders = new Queue<GpuInstanceId>(changeOrder.RemovingOrders.Where(c => !c.IsEmpty).ToList());
            var modifyingOrders = changeOrder.ModifyingOrders;
            //if (removingOrders.Count != changeOrder.RemovingOrders.Count)
            //{
            //    Debug.Log("TD55. TODO. There was removal order of not yet fully initialized gpuInstanceId. RepairIt");
            //}

            foreach (var modifyingOrder in modifyingOrders)
            {
                var blockIdx = modifyingOrder.InstanceId.GoodBlockIndex;
                var cellIndex = modifyingOrder.InstanceId.CellId;
                _instancingBlocks[blockIdx].ModifyInstance(cellIndex, modifyingOrder.Matrix, modifyingOrder.UniformsPack);
            }

            foreach (var addingOrder in addingOrders)
            {
                if (removingOrders.Any())
                {
                    var removedOne = removingOrders.Dequeue();
                    _instancingBlocks[removedOne.GoodBlockIndex].FillCell(removedOne.CellId, addingOrder.Matrix,
                        addingOrder.UniformsPack);
                    addingOrder.GpuInstanceId.FillFrom(removedOne);
                    _instanceIdsDictionary[removedOne.GoodBlockIndex][removedOne.CellId] = addingOrder.GpuInstanceId;
                    Precondition(removedOne.GoodBlockIndex);
                }
                else
                {
                    var goodBlockIndex = ProvideFreeBlockIndex();
                    var goodBlock = _instancingBlocks[goodBlockIndex];
                    var cellId = goodBlock.AddInstance(addingOrder.Matrix, addingOrder.UniformsPack);

                    addingOrder.GpuInstanceId.FillFrom(goodBlockIndex, cellId);
                    if (!_instanceIdsDictionary.ContainsKey(goodBlockIndex))
                    {
                        _instanceIdsDictionary.Add(goodBlockIndex, new List<GpuInstanceId>());
                    }
                    _instanceIdsDictionary[goodBlockIndex].Add(addingOrder.GpuInstanceId);
                    Precondition(goodBlockIndex);
                    Preconditions.Assert(_instanceIdsDictionary[goodBlockIndex].Count < 1024, "E48 too big idsDictionary. Has "+
                        _instanceIdsDictionary[goodBlockIndex].Count+" elements.");
                }
            }

            foreach (var removedId in removingOrders)
            {
                var blockIdx = removedId.GoodBlockIndex;
                var removedCellId = removedId.CellId;
                if (_instanceIdsDictionary[blockIdx].Count > 1)
                {
                    var lastGoodInBlock = _instanceIdsDictionary[blockIdx].Count - 1;
                    var idBeingMoved = _instanceIdsDictionary[blockIdx][lastGoodInBlock];
                    idBeingMoved.FillFrom(removedId);
                    _instanceIdsDictionary[blockIdx][removedCellId] = idBeingMoved;
                    _instanceIdsDictionary[blockIdx].RemoveAt(lastGoodInBlock);
                    _instancingBlocks[blockIdx].MoveAndRemove(removedCellId, lastGoodInBlock);
                    Precondition(blockIdx);
                }
                else
                {
                    _instanceIdsDictionary[blockIdx].RemoveAt(removedCellId);
                    _instancingBlocks[blockIdx].RemoveLast();
                }
            }
            removingOrders.Clear();

            HousekeepingUpdate();
            renderingData.StartUpdating();
            foreach (var block in _instancingBlocks.Where(c => c != null))
            {
                block.Update(renderingData.GetDataRecieverFor(block));
            }
            renderingData.StopUpdating();
        }

        private void Precondition(int index)
        {
            //Preconditions.Assert(_instanceIdsDictionary[index].Count == _instancingBlocks[index].UsedCellsCount, "E556 not equal");
        }

        private Int32 ProvideFreeBlockIndex()
        {
            int i = 0;
            foreach (var block in _instancingBlocks)
            {
                if (block == null)
                {
                    var newCreatedBlock = new GpuInstancingBlock(_uniformsArrayTemplate, i);
                    _instancingBlocks[i] = newCreatedBlock;
                    return i;
                }
                if (block.HasFreeSpace())
                {
                    return i;
                }
                i++;
            }
            var newBlock = new GpuInstancingBlock(_uniformsArrayTemplate, _instancingBlocks.Count);
            _instancingBlocks.Add(newBlock);
            return _instancingBlocks.Count - 1;
        }
    }
}