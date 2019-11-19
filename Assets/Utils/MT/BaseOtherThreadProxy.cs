using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scheduling;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

namespace Assets.Utils.MT
{
    public abstract class BaseOtherThreadProxy : IOtherThreadProxy
    {
        private string _threadName;
        private string _derivedClassName;
        private bool _synchronicBufferingUntilThreadStart;

        private Thread _thread;
        private SingleThreadSynchronizationContext _synchronizationContext;

        private List<ActionWithNamedCaller> _synchronicActionsToPreform = new List<ActionWithNamedCaller>();
        private Action _perEveryPostAction;

        private List<ActionWithNamedCaller> _actionsToPreformStoppedAtBarrier = new List<ActionWithNamedCaller>();
        private bool _newActionsBarrierIsErected = false;

        public BaseOtherThreadProxy(string threadName, bool synchronicBufferingUntilThreadStart)
        {
            _threadName = threadName;
            _derivedClassName = this.GetType().Name;
            _synchronicBufferingUntilThreadStart = synchronicBufferingUntilThreadStart;
            _newActionsBarrierIsErected = false;
            _actionsToPreformStoppedAtBarrier = new List<ActionWithNamedCaller>();
        }

        public void StartThreading(Action perEveryPostAction = null)
        {
            _perEveryPostAction = perEveryPostAction;
            if (!TaskUtils.GetGlobalMultithreading())
            {
                return;
            }
            _synchronizationContext = new SingleThreadSynchronizationContext();
            _thread = new Thread(() =>
            {
                try
                {
                    SingleThreadSynchronizationContext.SetSynchronizationContext(_synchronizationContext);
                    _synchronizationContext.SetPerEveryOrderAction(perEveryPostAction);
                    if (_synchronicActionsToPreform.Any())
                    {
                        _synchronicActionsToPreform.ForEach(c =>
                        {
                            PostAction(c.Action, c.CallerName);
                            perEveryPostAction();
                        });
                    }
                    _synchronizationContext.RunMessagePump();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"{_threadName} exception " + e.ToString());
                }
            });
            _thread.Name = _threadName;
            _thread.Start();
        }

        public void StopThreading()
        {
            if (TaskUtils.GetGlobalMultithreading())
            {
                _synchronizationContext.Cancel();
            }
        }

        public void ExecuteAction(Func<Task> actionToPreform)
        {
            PostAction(actionToPreform);
        }

        public void SynchronicUpdate()
        {
            if (!TaskUtils.GetGlobalMultithreading() || TaskUtils.GetMultithreadingOverride())
            {
                foreach (var action in _synchronicActionsToPreform)
                {
                    MyProfiler.BeginSample(_derivedClassName + " " + action.CallerName);
                    try
                    {
                        action.Action();
                        _perEveryPostAction?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("E7: " + e);
                        throw e;
                    }
                    finally
                    {
                        MyProfiler.EndSample();
                    }
                }
                _synchronicActionsToPreform.Clear();
            }
        }

        public TcsSemaphore ErectNewActionsBarrierAndWaitForSoleRemainingTask()
        {
            Preconditions.Assert(!_newActionsBarrierIsErected, "NewActionsBarrier is arleady erected");
            _newActionsBarrierIsErected = true;
            return _synchronizationContext.AcquireWaitingForSoleRemainingTaskSemaphore();
        }

        public void DismantleNewActionsBarrierAndWaitForQueuedActionsToStart()
        {
            Preconditions.Assert(_newActionsBarrierIsErected, "New actions barrier was not erected");
            _newActionsBarrierIsErected = false;
            var queuedActions = _actionsToPreformStoppedAtBarrier;
            _actionsToPreformStoppedAtBarrier = new List<ActionWithNamedCaller>();

            _synchronizationContext.RemoveWaitingForSoleRamainingTaskSemaphore();
            queuedActions.ForEach(c => PostAction(c.Action, c.CallerName));
        }

        public void PostAction(Func<Task> action, [CallerMemberName] string memberName = "")
        {
            if (!TaskUtils.GetGlobalMultithreading() || TaskUtils.GetMultithreadingOverride())
            {
                if (_synchronicBufferingUntilThreadStart)
                {
                    _synchronicActionsToPreform.Add(new ActionWithNamedCaller()
                    {
                        Action = action,
                        CallerName = memberName
                    });
                }
                else
                {
                    MyProfiler.BeginSample(_derivedClassName + " " + memberName);
                    action().Wait();
                    _perEveryPostAction?.Invoke();
                    MyProfiler.EndSample();
                }
            }
            else
            {
                if (_newActionsBarrierIsErected)
                {
                    _actionsToPreformStoppedAtBarrier.Add(new ActionWithNamedCaller()
                    {
                        Action = action,
                        CallerName = memberName
                    });
                }
                else
                {
                    _synchronizationContext.PostNew(()=> action());
                }
            }
        }

        public Task<TReturn> GenericPostAction<TReturn>(Func<Task<TReturn>> action)
        {
            var tcs = new TaskCompletionSource<TReturn>();
            PostAction(async () => tcs.SetResult(await action()));
            return tcs.Task;
        } 

        public void PostPureAsyncAction(Func<Task> taskedAction, [CallerMemberName] string memberName = "")
        {
            {
                PostAction(async () =>
                {

                    try
                    {
                        var task = taskedAction();
                        await task;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("E09: Exception withle pure async execution " + e);
                        throw;
                    }
                }, memberName);
            }
        }


        public void SetPerEveryOrderAction(Action action)
        {
            _perEveryPostAction = action;
            if (_synchronizationContext != null)
            {
                _synchronizationContext.SetPerEveryOrderAction(_perEveryPostAction);
            }
        }

        private Task _previousChainTask = null;
        public void PostChainedAction(Func<Task> action)
        {
            var tcs = new TaskCompletionSource<object>();
            Task taskObject = _previousChainTask; 
            PostAction(async () =>
            {
                if (taskObject != null)
                {
                    await taskObject;
                }
                await action();
                tcs.SetResult(null);
            });
            _previousChainTask = tcs.Task;
        }

        private class ActionWithNamedCaller
        {
            public Func<Task> Action;
            public string CallerName;
        }

        public OtherThreadServiceProfileInfo GetServiceProfileInfo()
        {
            var info = _synchronizationContext.GetOtherThreadServiceProfileInfo();
            info.Name = _threadName;
            return info;
        }
    }
}