using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Random;
using Assets.Utils.UTUpdating;
using Debug = UnityEngine.Debug;

namespace Assets.Utils.Services
{
    public class CommonExecutorUTProxy : BaseUTTransformProxy<object, Func<object>>
    {
        public async Task<T> AddAction<T>(Func<T> action)
        {
            return (T) await BaseUtAddOrder(() =>
            {
                    return (object) action();
            });
        }

        public Task<object> AddAction(Action action,
                [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0
            )
        {
            return AddAction(() =>
            {
                var sw = new Stopwatch();
                sw.Start();
                action();
                if (sw.ElapsedMilliseconds > 10)
                {
                    Debug.Log($"Long common service with time {sw.ElapsedMilliseconds}ms at {sourceFilePath}:{sourceLineNumber} {memberName}");
                }

                return new object();
            });
        }

        protected override object ExecuteOrder(Func<object> actionToPreform)
        {
            try
            {
                return actionToPreform();
            }
            catch (Exception e)
            {
                Debug.Log("E921 Exception during execution: "+e);
                throw;
            }
        }
    }
}