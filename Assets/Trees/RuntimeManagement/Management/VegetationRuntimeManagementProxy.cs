using System;
using System.Threading;
using System.Threading.Tasks;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.Management
{
    public class VegetationRuntimeManagementProxy : IOtherThreadProxy//todo add baseotherThradProxy
    {
        private VegetationRuntimeManagement _runtimeManagement;
        private Thread _runtimeManagementThread;
        private MyThreadSafeOneSpaceBox<object> _updateBox = new MyThreadSafeOneSpaceBox<object>();

        public VegetationRuntimeManagementProxy(VegetationRuntimeManagement runtimeManagement)
        {
            _runtimeManagement = runtimeManagement;
        }

        public void Start(Vector3 startCameraPosition)
        {
            _runtimeManagement.Start(startCameraPosition);
        } // todo stop method

        public void StartThreading(Action everyPostAction = null)
        {
            if (!TaskUtils.GetGlobalMultithreading())
            {
                return;
            }
            else
            {
                _runtimeManagementThread = new Thread(() =>
                {
                    try
                    {
                        while (true)
                        {
                            Vector3 cameraPosition = (Vector3) _updateBox.RetriveUpdateData();
                            _runtimeManagement.Update(cameraPosition);
                            everyPostAction?.Invoke();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("VegetationRuntimeManagementProxy thread exception: " + e.ToString());
                    }
                });
                _runtimeManagementThread.Start();
            }
        }

        public void ExecuteAction(Func<Task> actionToPreform)
        {
            throw new NotImplementedException();
        }

        public void SynchronicUpdate(Vector3 cameraPosition)
        {
            _runtimeManagement.Update(cameraPosition);
        }

        public void AddUpdate(Vector3 cameraPosition)
        {
            _updateBox.SetCurrentData(cameraPosition);
        }
    }
}