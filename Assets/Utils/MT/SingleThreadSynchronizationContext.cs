
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
        private  SynchronizationContextWorkContainer _workContainer = new SynchronizationContextWorkContainer();
        private readonly object myLock = new object();
        private bool isRunning = true;

        private bool _threadIsWorking = false;
        private int _nonCompletedTasks = 0;

        private Action _perEveryOrderAction;
        private TcsSemaphore _waitingForSoleRemainingTaskSemaphore;

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
                    _workContainer.AddContinuationToProcess(() => codeToRun(state));
                }
                else
                {
                    _workContainer.AddNewTaskToProcess(() => codeToRun(state));
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

                CheckWaitingForSoleRemainingTaskSemaphore();
            }
        }

        private Action GrabItem()
        {
            lock (myLock)
            {
                while (CanContinue() && ! _workContainer.AnyWorkToDo())
                {
                    _threadIsWorking = false;
                    Monitor.Wait(myLock);
                    _threadIsWorking = true;
                }
                if (!CanContinue())
                {
                    return null;
                }

                return _workContainer.GetNextActionToPerform();
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

            var statistics = _workContainer.CalculateStatistics();
            return new OtherThreadServiceProfileInfo()
            {
                IsWorking = _threadIsWorking,
                NewTaskCount = statistics.NewTasksToProcessCount,
                ContinuingTasksCount = statistics.ContinuationsToProcessCount,
                BlockedTasksCount = _nonCompletedTasks - statistics.ContinuationsToProcessCount - addonToQueue
            };
        }

        public TcsSemaphore AcquireWaitingForSoleRemainingTaskSemaphore()
        {
            Preconditions.Assert( _waitingForSoleRemainingTaskSemaphore == null, "Semaphore is arleady present. ");
            Preconditions.Assert(_nonCompletedTasks>0,"There are no non-completed task present");
            var newSemaphore = new TcsSemaphore();
            _waitingForSoleRemainingTaskSemaphore = newSemaphore; 
            CheckWaitingForSoleRemainingTaskSemaphore();
            return newSemaphore;
        }

        private void CheckWaitingForSoleRemainingTaskSemaphore()
        {
            if (_waitingForSoleRemainingTaskSemaphore != null)
            {
                if (!_waitingForSoleRemainingTaskSemaphore.SemaphoreIsSet())
                {
                    Preconditions.Assert(_nonCompletedTasks > 0, "There are no non-completed task present");
                    Debug.Log("Checking is semaphore is up: " + _nonCompletedTasks + " " + _workContainer.CalculateStatistics().ContinuationsToProcessCount +
                              " " + _workContainer.CalculateStatistics().NewTasksToProcessCount);
                    if (_nonCompletedTasks == 1 && !_workContainer.AnyWorkToDo())
                    {
                        _waitingForSoleRemainingTaskSemaphore.Set();
                    }
                }
            }
        }

        public void RemoveWaitingForSoleRamainingTaskSemaphore()
        {
            Preconditions.Assert( _waitingForSoleRemainingTaskSemaphore != null, "Semaphore is not present. ");
            _waitingForSoleRemainingTaskSemaphore = null;
        }
    }

    public enum SynchronizationContextWorkPriority
    {
        Low, High
    }

    public class SynchronizationContextWorkContainer
    {
        private readonly Queue<Action> _continuationsToProcess = new Queue<Action>();
        private readonly Queue<Action> _newTasksToProcess = new Queue<Action>();

        public void AddContinuationToProcess(Action action)
        {
            _continuationsToProcess.Enqueue(action);
        }

        public void AddNewTaskToProcess(Action action)
        {
            _newTasksToProcess.Enqueue(action);
        }

        public bool AnyWorkToDo()
        {
            return _continuationsToProcess.Any() || _newTasksToProcess.Any();
        }

        public Action GetNextActionToPerform()
        {
            if (_continuationsToProcess.Any())
            {
                return _continuationsToProcess.Dequeue();
            }
            else if (_newTasksToProcess.Any())
            {
                return _newTasksToProcess.Dequeue();
            }
            Preconditions.Fail( "There is no work to do");
            return null;
        }

        public SynchronizationContextWorkContainerStatistics CalculateStatistics()
        {
            return new SynchronizationContextWorkContainerStatistics()
            {
                ContinuationsToProcessCount = _continuationsToProcess.Count,
                NewTasksToProcessCount = _newTasksToProcess.Count
            };
        }
    }

    public class SynchronizationContextWorkContainerStatistics
    {
        public int ContinuationsToProcessCount;
        public int NewTasksToProcessCount;
    }
}
