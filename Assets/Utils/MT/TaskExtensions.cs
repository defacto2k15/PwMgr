using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Utils.MT
{
    public static class TaskExtensions
    {
        public static Task ReportUnityExceptions(this Task task)
        {
            task.ContinueWith(t =>
            {
                UnityEngine.Debug.LogError(t.Exception); //dont think it works :(
            }, TaskContinuationOptions.OnlyOnFaulted);
            return task;
        }

        public static Task<T> ReportUnityExceptions<T>(this Task<T> task)
        {
            task.ContinueWith(t =>
            {
                UnityEngine.Debug.LogError(t.Exception); //dont think it works :(
            }, TaskContinuationOptions.OnlyOnFaulted);
            return task;
        }
    }
}