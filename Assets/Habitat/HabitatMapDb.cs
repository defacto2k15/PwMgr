using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Pathfinding.Fitting;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Quadtree;
using Assets.Utils.Services;
using UnityEngine.Networking;
using BaseOtherThreadProxy = Assets.Utils.MT.BaseOtherThreadProxy;

namespace Assets.Habitat
{
    public class HabitatMapDb
    {
        private HabitatMap _habitatMap;
        private string _rootSerializationPath;

        public HabitatMapDb(HabitatMapDbInitializationInfo initializationInfo)
        {
            _habitatMap = initializationInfo.Map;
            _rootSerializationPath = initializationInfo.RootSerializationPath;
        }

        public MyQuadtree<HabitatFieldInTree> Query(MyRectangle queryArea)
        {
            AssertMapIsPresent();
            return _habitatMap.QueryMap(queryArea);
        }

        private void AssertMapIsPresent()
        {
            if (_habitatMap == null)
            {
                var fileManager = new HabitatMapFileManager();
                _habitatMap = fileManager.LoadHabitatMap(_rootSerializationPath);
            }
        }

        public class HabitatMapDbInitializationInfo
        {
            public HabitatMap Map;
            public string RootSerializationPath;
        }
    }

    public class HabitatMapDbProxy : BaseOtherThreadProxy
    {
        private HabitatMapDb _db;

        public HabitatMapDbProxy(HabitatMapDb db) : base("HabitatMapDbProxyThread", false)
        {
            _db = db;
        }

        public Task<MyQuadtree<HabitatFieldInTree>> Query(MyRectangle query)
        {
            var tcs = new TaskCompletionSource<MyQuadtree<HabitatFieldInTree>>();
            PostAction(() =>
            {
                tcs.SetResult(_db.Query(query));
                return TaskUtils.EmptyCompleted();
            });
            return tcs.Task;
        }
    }
}