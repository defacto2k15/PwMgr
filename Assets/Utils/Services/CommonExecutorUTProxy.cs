using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Random;
using Assets.Utils.UTUpdating;
using UnityEngine;

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

        public Task<object> AddAction(Action action)
        {
            return AddAction(() =>
            {
                    action();
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