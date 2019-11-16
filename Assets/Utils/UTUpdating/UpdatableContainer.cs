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

        public void Update(ICameraForUpdate camera)
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

        public virtual void UpdateCamera(ICameraForUpdate camera)
        {
        }

        public virtual void StartUpdateCamera(ICameraForUpdate camera)
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
        public Action<ICameraForUpdate> UpdateCameraField;

        public Action StartField;
        public Action<ICameraForUpdate> StartCameraField;

        public override void Update()
        {
            UpdateField?.Invoke();
        }

        public override void StartUpdate()
        {
            StartField?.Invoke();
        }

        public override void UpdateCamera(ICameraForUpdate camera)
        {
            UpdateCameraField?.Invoke(camera);
        }

        public override void StartUpdateCamera(ICameraForUpdate camera)
        {
            StartCameraField?.Invoke(camera);
        }
    }

    public class OtherThreadProxyWithPerPostAction
    {
        public BaseOtherThreadProxy OtherThreadProxy;
        public Action PerPostAction;
    }

    public interface ICameraForUpdate
    {
        Vector3 Position { get; set; }
        Plane[] CalculateFrustumPlanes(ICameraForUpdate camera);
    }

    public class EncapsulatedCameraForUpdate : ICameraForUpdate
    {
        private Camera _camera;

        public EncapsulatedCameraForUpdate(Camera camera)
        {
            _camera = camera;
        }

        public Vector3 Position
        {
            get => _camera.transform.position;
            set => _camera.transform.position = value;
        }

        public Plane[] CalculateFrustumPlanes(ICameraForUpdate camera)
        {
            return GeometryUtility.CalculateFrustumPlanes(_camera);
        }
    }

    public class MockedFromGameObjectCameraForUpdate : ICameraForUpdate
    {
        private GameObject _go;

        public MockedFromGameObjectCameraForUpdate(GameObject go)
        {
            _go = go;
        }

        public Vector3 Position
        {
            get => _go.transform.position;
            set => _go.transform.position = value;
        }
        public Plane[] CalculateFrustumPlanes(ICameraForUpdate camera)
        {
            throw new NotImplementedException("This object only mocks camera from GameObject. Cannot calculate frustum planes.");
        }
    }
}