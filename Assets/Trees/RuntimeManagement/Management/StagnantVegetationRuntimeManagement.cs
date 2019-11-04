using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using UnityEngine;

namespace Assets.Trees.RuntimeManagement.Management
{
    public class StagnantVegetationRuntimeManagement
    {
        private IVegetationSubjectInstancingContainerChangeListener _vegetationSubjectsChangesListener;
        private List<VegetationSubjectEntity> _gainedEntities;
        private StagnantVegetationRuntimeManagementConfiguration _configuration;

        public StagnantVegetationRuntimeManagement(
            IVegetationSubjectInstancingContainerChangeListener vegetationSubjectsChangesListener,
            List<VegetationSubjectEntity> gainedEntities,
            StagnantVegetationRuntimeManagementConfiguration configuration)
        {
            _vegetationSubjectsChangesListener = vegetationSubjectsChangesListener;
            _gainedEntities = gainedEntities;
            _configuration = configuration;
        }

        public void Start()
        {
            MyProfiler.BeginSample("StagnantVegetationRuntimeManagement Start");
            _vegetationSubjectsChangesListener.AddInstancingOrder(_configuration.DetailLevel, _gainedEntities,
                new List<VegetationSubjectEntity>());
            MyProfiler.EndSample();
        }
    }

    public class StagnantVegetationRuntimeManagementConfiguration
    {
        public VegetationDetailLevel DetailLevel  = VegetationDetailLevel.BILLBOARD;
    }

    public class StagnantVegetationRuntimeManagementProxy : IOtherThreadProxy//todo add baseotherThradProxy
    {
        private readonly StagnantVegetationRuntimeManagement _runtimeManagement;
        private Thread _runtimeManagementThread;

        public StagnantVegetationRuntimeManagementProxy(StagnantVegetationRuntimeManagement runtimeManagement)
        {
            _runtimeManagement = runtimeManagement;
        }


        public void StartThreading(Action everyPostAction = null)
        {
            if (!TaskUtils.GetGlobalMultithreading())
            {
                 _runtimeManagement.Start();
            }
            else
            {
                _runtimeManagementThread = new Thread(() =>
                {
                    try
                    {
                        _runtimeManagement.Start();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("StagnantVegetationRuntimeManagementProxy thread exception: " + e.ToString());
                    }
                });
                _runtimeManagementThread.Start();
            }
        }

        public void ExecuteAction(Func<Task> actionToPreform)
        {
            throw new NotImplementedException();
        }
    }
}
