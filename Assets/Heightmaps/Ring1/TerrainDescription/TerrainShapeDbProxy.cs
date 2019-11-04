using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription.Cache;
using Assets.Ring2.Painting;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using UnityEngine;
using BaseOtherThreadProxy = Assets.Utils.MT.BaseOtherThreadProxy;

namespace Assets.Heightmaps.Ring1.TerrainDescription
{
    public class TerrainShapeDbProxy : BaseOtherThreadProxy, ITerrainShapeDb
    {
        private TerrainShapeDb _db;

        public TerrainShapeDbProxy(TerrainShapeDb db) : base("TerrainShapeDbProxyThread", false)
        {
            _db = db;
        }

        public Task<TerrainDescriptionOutput> Query(TerrainDescriptionQuery query)
        {
            var tcs = new TaskCompletionSource<TerrainDescriptionOutput>();
            PostAction(async () => tcs.SetResult(await ProcessQueryOrderAsync(query)));
            return tcs.Task;
        }

        public Task DisposeTerrainDetailElement(TerrainDetailElementToken token)
        {
            var tcs = new TaskCompletionSource<object>();
            PostAction(
                async () =>
                {
                    await ProcessElementRemovalOrderAsync(token);
                    tcs.SetResult(null);
                }
            );
            return tcs.Task;
        }

        private Task<TerrainDescriptionOutput> ProcessQueryOrderAsync(TerrainDescriptionQuery query)
        {
            return _db.QueryAsync(query);
        }

        private Task ProcessElementRemovalOrderAsync(TerrainDetailElementToken token)
        {
            return _db.RemoveTerrainDetailElementAsync(token);
        }
    }
}