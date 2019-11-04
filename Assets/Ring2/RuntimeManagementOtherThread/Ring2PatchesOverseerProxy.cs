using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Ring2.RuntimeManagement;
using Assets.Utils;
using Assets.Utils.MT;

namespace Assets.Ring2.RuntimeManagementOtherThread
{
    public class Ring2PatchesOverseerProxy
    {
        private Thread _runtimeManagementThread;
        private SingleThreadSynchronizationContext _synchronizationContext;

        private DictionaryWithIdGeneration<OverseedPatchId> _overseedPatchIds =
            new DictionaryWithIdGeneration<OverseedPatchId>();

        private Ring2PatchesOverseer _overseer;

        public Ring2PatchesOverseerProxy(Ring2PatchesOverseer overseer)
        {
            _overseer = overseer;
        }

        public List<Ring2PatchDiametersWithId> Create(List<MyRectangle> patchesToCreateDiameters)
        {
            var oPatchIds = patchesToCreateDiameters.Select(p => new
            {
                rectangle = p,
                patchId = new OverseedPatchId()
            }).Select(p => new
            {
                rectangle = p.rectangle,
                patchId = p.patchId,
                outId = _overseedPatchIds.AddNew(p.patchId)
            }).ToList();

            var ring2PatchesOverseerOrder = new Ring2PatchesOverseerOrder()
            {
                CreationOrder = oPatchIds.Select(c => new Ring2PatchesCreationOrderElement()
                {
                    OutPatchId = c.patchId,
                    Rectangle = c.rectangle
                }).ToList()
            };
            AddOverseerOrder(ring2PatchesOverseerOrder);
            return oPatchIds.Select(c => new Ring2PatchDiametersWithId()
            {
                Diameters = c.rectangle,
                Id = c.outId
            }).ToList();
        }

        public void Remove(List<Ring2PatchDiametersWithId> patchesToRemove)
        {
            AddOverseerOrder(new Ring2PatchesOverseerOrder()
            {
                RemovalOrders = patchesToRemove.Select(c => _overseedPatchIds.Get(c.Id)).ToList()
            });
            patchesToRemove.ForEach(i => _overseedPatchIds.Remove(i.Id));
        }

        private void AddOverseerOrder(Ring2PatchesOverseerOrder order)
        {
            if (!TaskUtils.GetGlobalMultithreading() || TaskUtils.GetMultithreadingOverride())
            {
                _overseer.ProcessOrderAsync(order).ReportUnityExceptions();
            }
            else
            {
                _synchronizationContext.PostNew(() => _overseer.ProcessOrderAsync(order));
            }
        }


        public void StartThreading()
        {
            if (!TaskUtils.GetGlobalMultithreading())
            {
                return;
            }
            _synchronizationContext = new SingleThreadSynchronizationContext();
            _runtimeManagementThread = new Thread(() =>
            {
                try
                {
                    SingleThreadSynchronizationContext.SetSynchronizationContext(_synchronizationContext);
                    _synchronizationContext.RunMessagePump();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(
                        "Forging vegetation subject instance container proxy thread exception: " + e.ToString());
                }
            });
            _runtimeManagementThread.Name = "Ring2PatchesOverseerThread";
            _runtimeManagementThread.Start();
        }
    }
}