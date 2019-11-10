using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scheduling;
using Assets.Utils.MT;
using Assets.Utils.Services;
using UnityEngine;

namespace Assets.Utils.UTUpdating
{
    public class UpdatableContainer : MonoBehaviour
    {
        public List<IUpdatable> UpdatableElements = new List<IUpdatable>();

        public void AddUpdatableElement(IUpdatable newElem)
        {
            UpdatableElements.Add(newElem);
        }

        public void Update()
        {
            UpdatableElements.ForEach(c => c.Update());
        }
    }

    public class UltraUpdatableContainer
    {
        private List<BaseUltraUpdatable> _updatableElements = new List<BaseUltraUpdatable>();
        private List<OtherThreadProxyWithPerPostAction> _otherThreadProxys = new List<OtherThreadProxyWithPerPostAction>();
        private MyUtScheduler _utScheduler;
        private GlobalServicesProfileInfo _globalServicesProfileInfo;
        private UltraUpdatableContainerConfiguration _configuration;

        private bool _once = false;

        public UltraUpdatableContainer(MyUtSchedulerConfiguration myUtSchedulerConfiguration, GlobalServicesProfileInfo globalServicesProfileInfo, UltraUpdatableContainerConfiguration configuration)
        {
            _configuration = configuration;
            _globalServicesProfileInfo = globalServicesProfileInfo;
            _utScheduler = new MyUtScheduler(myUtSchedulerConfiguration);
        }

        public void AddUpdatableElement(BaseUltraUpdatable newElem)
        {
            _updatableElements.Add(newElem);
        }

        public void AddOtherThreadProxy(OtherThreadProxyWithPerPostAction otherThreadProxy)
        {
            _otherThreadProxys.Add(otherThreadProxy);
        }

        public void AddOtherThreadProxy(BaseOtherThreadProxy otherThreadProxy)
        {
            _otherThreadProxys.Add(new OtherThreadProxyWithPerPostAction()
            {
                OtherThreadProxy = otherThreadProxy,
                PerPostAction = () => { }
            });
        }

        public void Add(BaseUTService service)
        {
            _utScheduler.AddService(service);
        }

        public void Update(Camera camera)
        {
            if (!_once)
            {
                foreach (var otherThreadProxy in _otherThreadProxys)
                {
                    otherThreadProxy.OtherThreadProxy.StartThreading(otherThreadProxy.PerPostAction);
                }

                foreach (var otherThreadProxy in
                    _updatableElements.Select(c => c.RetriveOtherThreadProxies()).Where(c => c != null)
                        .SelectMany(c => c))
                {
                    otherThreadProxy.OtherThreadProxy.StartThreading(otherThreadProxy.PerPostAction);
                }
                _updatableElements.ForEach(c => c.StartUpdate());
                _updatableElements.ForEach(c => c.StartUpdateCamera(camera));
                _once = true;
            }
            else
            {
                _utScheduler.StartFrame();

                _updatableElements.ForEach(c => c.Update());
                _updatableElements.ForEach(c => c.UpdateCamera(camera));

                _otherThreadProxys.ForEach(c =>
                {
                    int todo = 22;
                    c.OtherThreadProxy.SynchronicUpdate();
                });

                _utScheduler.Update();

                if (_configuration.ServicesProfilingEnabled && TaskUtils.GetGlobalMultithreading())
                {
                    var utServicesProfileInfo = _utScheduler.GetUtServicesProfileInfo();
                    var otServicesProfileInfo = _otherThreadProxys.Select(c => c.OtherThreadProxy.GetServiceProfileInfo()).ToList();

                    _globalServicesProfileInfo.SetProfileInfos(utServicesProfileInfo, otServicesProfileInfo);
                }
            }
        }

        public void Stop()
        {
            _otherThreadProxys.ForEach(c => c.OtherThreadProxy.StopThreading());
        }
    }

    public class UltraUpdatableContainerConfiguration
    {
        public bool ServicesProfilingEnabled = true;
    }

    public abstract class BaseUltraUpdatable
    {
        public virtual void Update()
        {
        }

        public virtual void StartUpdate()
        {
        }

        public virtual void UpdateCamera(Camera camera)
        {
        }

        public virtual void StartUpdateCamera(Camera camera)
        {
        }

        public virtual List<OtherThreadProxyWithPerPostAction> RetriveOtherThreadProxies()
        {
            return new List<OtherThreadProxyWithPerPostAction>();
        }
    }

    public class FieldBasedUltraUpdatable : BaseUltraUpdatable
    {
        public Action UpdateField;
        public Action<Camera> UpdateCameraField;

        public Action StartField;
        public Action<Camera> StartCameraField;

        public override void Update()
        {
            UpdateField?.Invoke();
        }

        public override void StartUpdate()
        {
            StartField?.Invoke();
        }

        public override void UpdateCamera(Camera camera)
        {
            UpdateCameraField?.Invoke(camera);
        }

        public override void StartUpdateCamera(Camera camera)
        {
            StartCameraField?.Invoke(camera);
        }
    }

    public class OtherThreadProxyWithPerPostAction
    {
        public BaseOtherThreadProxy OtherThreadProxy;
        public Action PerPostAction;
    }
}