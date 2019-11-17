
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scheduling;
using UnityEngine;

namespace Assets.Utils.MT
{
    public class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<Action> _continuationsToProcess = new Queue<Action>();
        private readonly Queue<Action> _newTasksToProcess = new Queue<Action>();
        private readonly object myLock = new object();
        private bool isRunning = true;

        private bool _threadIsWorking = false;
        private int _nonCompletedTasks = 0;

        private Action _perEveryOrderAction;

        public override void Send(SendOrPostCallback codeToRun, object state)
        {
            throw new NotImplementedException();
        }

        public void PostNew(Func<Task> action)
        {
            Post((obj) =>
            {
                Interlocked.Increment(ref _nonCompletedTasks);
                action().ReportUnityExceptions().ContinueWith((t) =>
                {
                    Interlocked.Decrement(ref _nonCompletedTasks);
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }, null);
        }

        public override void Post(SendOrPostCallback codeToRun, object state)
        {
            lock (myLock)
            {
                if (state != null)
                {
                    _continuationsToProcess.Enqueue(() => codeToRun(state));
                }
                else
                {
                    _newTasksToProcess.Enqueue(() => codeToRun(state));
                }
                SignalContinue();
            }
        }

        public void RunMessagePump()
        {
            while (CanContinue())
            {
                Action nextToRun = GrabItem();
                if (nextToRun == null)
                {
                    Preconditions.Assert(!CanContinue(), "E824 Got null action when CanCantinue is still true");
                    return; // 
                }
                nextToRun();
                if (_perEveryOrderAction != null)
                {
                    _perEveryOrderAction();
                }
            }
        }

        public void OneMessageLoop()
        {
            Action action = null;
            lock (myLock)
            {
                if (_continuationsToProcess.Any())
                {
                    action = _continuationsToProcess.Dequeue();
                }else if (_newTasksToProcess.Any())
                {
                    action = _newTasksToProcess.Dequeue();
                }
                else
                {
                    return;
                }
            }

            action();
            _perEveryOrderAction?.Invoke();
        }

        private Action GrabItem()
        {
            lock (myLock)
            {
                while (CanContinue() && _newTasksToProcess.Count == 0 && _continuationsToProcess.Count == 0)
                {
                    _threadIsWorking = false;
                    Monitor.Wait(myLock);
                    _threadIsWorking = true;
                }
                if (!CanContinue())
                {
                    return null;
                }

                if (_continuationsToProcess.Any())
                {
                    return _continuationsToProcess.Dequeue();
                }
                else
                {
                    return _newTasksToProcess.Dequeue();
                }
            }
        }

        private bool CanContinue()
        {
            lock (myLock)
            {
                return isRunning;
            }
        }

        public void Cancel()
        {
            lock (myLock)
            {
                isRunning = false;
                SignalContinue();
            }
        }

        private void SignalContinue()
        {
            Monitor.Pulse(myLock);
        }

        public void SetPerEveryOrderAction(Action action)
        {
            _perEveryOrderAction = action;
        }

        public Action GetPerEveryOrderAction()
        {
            return _perEveryOrderAction;
        }

        public OtherThreadServiceProfileInfo GetOtherThreadServiceProfileInfo()
        {
            var addonToQueue = 0;
            if (_threadIsWorking)
            {
                addonToQueue++;
            }
            return new OtherThreadServiceProfileInfo()
            {
                IsWorking = _threadIsWorking,
                NewTaskCount = _newTasksToProcess.Count,
                ContinuingTasksCount = _continuationsToProcess.Count,
                BlockedTasksCount = _nonCompletedTasks - _continuationsToProcess.Count - addonToQueue
            };
        }
    }
}
