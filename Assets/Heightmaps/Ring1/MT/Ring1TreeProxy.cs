using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using UnityEngine;
using BaseOtherThreadProxy = Assets.Utils.MT.BaseOtherThreadProxy;

namespace Assets.Heightmaps.Ring1.MT
{
    public class Ring1TreeProxy : BaseOtherThreadProxy
    {
        private Ring1Tree _tree;

        public Ring1TreeProxy(Ring1Tree tree) : base("Ring1TreeProxyThread", false)
        {
            _tree = tree;
        }

        public Task CreateHeightmap(Ring1Tree.RootNodeCreationParameters creationParameters)
        {
            var tcs = new TaskCompletionSource<object>();
            PostAction(() =>
            {
                _tree.CreateHeightmap(creationParameters);
                tcs.SetResult(null);
                return TaskUtils.EmptyCompleted();
            });
            return tcs.Task;
        }

        public Task UpdateCamera(FovData fovData)
        {
            var tcs = new TaskCompletionSource<object>();
            PostAction(() =>
            {
                _tree.UpdateLod(fovData);
                tcs.SetResult(null);
                return TaskUtils.EmptyCompleted();
            });
            return tcs.Task;
        }
    }
}