using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Assets.Grass2.GrassIntensityMap;
using Assets.Grass2.Growing;
using Assets.Grass2.Types;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Spatial;
using BaseOtherThreadProxy = Assets.Utils.MT.BaseOtherThreadProxy;

namespace Assets.Grass2.IntenstityDb
{
    public class Grass2IntensityDbProxy : BaseOtherThreadProxy, IGrassIntensityMapProvider
    {
        private SpatialDb<List<Grass2TypeWithIntensity>> _grassIntensityDb;

        public Grass2IntensityDbProxy(SpatialDb<List<Grass2TypeWithIntensity>> grassIntensityDb) : base(
            "Grass2IntensityDbProxyThread", false)
        {
            _grassIntensityDb = grassIntensityDb;
        }

        public Task<UvdCoordedPart<List<Grass2TypeWithIntensity>>> ProvideMapsAtAsync(MyRectangle queryArea)
        {
            if (!TaskUtils.GetGlobalMultithreading() || TaskUtils.GetMultithreadingOverride())
            {
                return _grassIntensityDb.ProvidePartsAt(queryArea);
            }
            else
            {
                var tcs = new TaskCompletionSource<UvdCoordedPart<List<Grass2TypeWithIntensity>>>();
                PostAction(async () => tcs.SetResult(await _grassIntensityDb.ProvidePartsAt(queryArea)));
                return tcs.Task;
            }
        }
    }

    public class Grass2TypeWithIntensity
    {
        public GrassType GrassType;
        public IntensityFieldFigure IntensityFigure;
    }
}