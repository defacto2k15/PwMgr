using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utils.MT
{
    public static class TaskUtils
    {
        private static bool? _isMultithreading = null;
        private static Thread _unityThread = null;

        public static bool GetGlobalMultithreading()
        {
            Preconditions.Assert(_isMultithreading.HasValue, "Multithreading is not set");
            return _isMultithreading.Value;
        }

        public static void SetGlobalMultithreading(bool isMultithreading)
        {
            _isMultithreading = isMultithreading;
            _unityThread = Thread.CurrentThread;
        }

        public static bool IsThisUnityTHread => Thread.CurrentThread == _unityThread;

        private static bool _multithreadingOverride = false;

        public static void SetMultithreadingOverride(bool val)
        {
            _multithreadingOverride = val;
        }

        public static bool GetMultithreadingOverride()
        {
            return _multithreadingOverride;
        }

        public static void ExecuteActionWithOverridenMultithreading(bool overrideValue, Action action)
        {
            var original = GetMultithreadingOverride();
            SetMultithreadingOverride(overrideValue);
            action();
            SetMultithreadingOverride(original);
        }

        public static T ExecuteFunctionWithOverridenMultithreading<T>(bool overrideValue,  Func<T> function)
        {
            var original = GetMultithreadingOverride();
            SetMultithreadingOverride(overrideValue);
            var outValue = function();
            SetMultithreadingOverride(original);
            return outValue;
        }

        public static Task<T> MyFromResult<T>(T elem)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(elem);
            return tcs.Task;
        }

        public static Task MyFromResultGenetic()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        public static Task WhenAll(IEnumerable<Task> tasks)
        {
            if (!GetGlobalMultithreading())
            {
                var tcs = new TaskCompletionSource<List<object>>();
                foreach (var task in tasks)
                {
                    while (!task.IsCompleted)
                    {
                    }
                    if (task.IsFaulted)
                    {
                        UnityEngine.Debug.LogError(task.Exception);
                        throw task.Exception;
                    }
                }
                tcs.SetResult(null);
                return tcs.Task;
            }
            else
            {
                var tcs = new TaskCompletionSource<List<object>>();
                var remainingTasks = tasks.ToList();
                var count = remainingTasks.Count();
                if (count == 0)
                {
                    return TaskUtils.EmptyCompleted();
                }

                foreach (var task in remainingTasks)
                {
                    task.ContinueWith(t =>
                    {
                        if (Interlocked.Decrement(ref count) == 0)
                        {
                            var exceptions = new List<Exception>();
                            foreach (var task1 in remainingTasks)
                            {
                                if (task1.IsFaulted)
                                {
                                    if (task1.Exception != null)
                                    {
                                        exceptions.Add(task1.Exception);
                                    }
                                    else
                                    {
                                        exceptions.Add(
                                            new OperationCanceledException("For task " + task1.ToString()));
                                    }
                                }
                            }
                            if (exceptions.Any())
                            {
                                var aggregateException = new AggregateException(exceptions);
                                tcs.SetException(aggregateException);
                            }
                            else
                            {
                                tcs.SetResult(null);
                            }
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
                return tcs.Task;
            }
        }

        public static Task<List<T>> WhenAll<T>(IEnumerable<Task<T>> tasks) where T : class
        {
            if (!GetGlobalMultithreading() || GetMultithreadingOverride())
            {
                var tcs1 = new TaskCompletionSource<List<T>>();
                var outList = tasks.Select(c => c.Result).ToList();
                tcs1.SetResult(outList);
                return tcs1.Task;
            }

            var tcs = new TaskCompletionSource<List<T>>();
            var remainingTasks = tasks.ToList();
            var count = remainingTasks.Count();

            if (count == 0)
            {
                tcs.SetResult(new List<T>());
                return tcs.Task;
            }

            var resultList = new List<T>();
            foreach (var task in remainingTasks)
            {
                task.ContinueWith(t =>
                {
                    if (Interlocked.Decrement(ref count) == 0)
                    {
                        var exceptions = new List<Exception>();
                        foreach (var task1 in remainingTasks)
                        {
                            if (task1.IsFaulted)
                            {
                                if (task1.Exception != null)
                                {
                                    exceptions.Add(task1.Exception);
                                }
                                else
                                {
                                    exceptions.Add(new OperationCanceledException("For task " + task1.ToString()));
                                }
                            }
                            else
                            {
                                resultList.Add(task1.Result);
                            }
                        }
                        if (exceptions.Any())
                        {
                            var aggregateException = new AggregateException(exceptions);
                            tcs.SetException(aggregateException);
                        }
                        else
                        {
                            tcs.SetResult(resultList);
                        }
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

            return tcs.Task;
        }

        public static Task EmptyCompleted()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        public static Task<T> RunInThreadPool<T>(Func<T> func)
        {
            if (!TaskUtils.GetGlobalMultithreading() || TaskUtils.GetMultithreadingOverride())
            {
                return TaskUtils.MyFromResult(func());
            }
            else
            {
                return Task.Factory.StartNew(func);
            }
        }

        public static void DebuggerAwareWait(Task task)
        {
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                bool timeout = !task.Wait(3000);
                if (timeout)
                {
                    Debug.LogError("E783 Task timeouted!");
                }
            }
            else
            {
                task.Wait();
            }
        }
    }
}