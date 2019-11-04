using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Files;
using Assets.Roads.Pathfinding.Fitting;
using Assets.TerrainMat;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Quadtree;
using GeoAPI.Geometries;

namespace Assets.Roads
{
    public class RoadDatabase
    {
        private MyQuadtree<PathWithEnvelope> _tree;
        private string _rootWrtPath;


        public RoadDatabase(string rootWrtPath)
        {
            _rootWrtPath = rootWrtPath;
        }

        public Task<List<PathQuantisized>> Query(MyRectangle queryRectange)
        {
            AssertPathsAreLoaded();
            return TaskUtils.MyFromResult(_tree
                .QueryWithIntersection(MyNetTopologySuiteUtils.ToGeometryEnvelope(queryRectange))
                .Select(c => c.Path).ToList());
        }

        private void AssertPathsAreLoaded()
        {
            if (_tree == null)
            {
                var fileManager = new PathFileManager();
                var paths = fileManager.LoadPaths(_rootWrtPath);

                var simplifier = new PathSimplifier(0.2f);

                _tree = new MyQuadtree<PathWithEnvelope>();
                int i = 0;
                foreach (var aPath in paths)
                {
                    var simplifiedPath = new PathQuantisized(simplifier.Simplify(aPath.PathNodes));
                    _tree.Add(new PathWithEnvelope(simplifiedPath));
                    i++;
                }
            }
        }

        private class PathWithEnvelope : IHasEnvelope, ICanTestIntersect
        {
            private PathQuantisized _path;

            public PathWithEnvelope(PathQuantisized path)
            {
                _path = path;
            }

            public Envelope CalculateEnvelope()
            {
                return _path.Line.EnvelopeInternal;
            }

            public bool Intersects(IGeometry geometry)
            {
                return _path.Line.Envelope.Intersects(geometry);
            }

            public PathQuantisized Path => _path;
        }
    }

    public class RoadDatabaseProxy : BaseOtherThreadProxy
    {
        private readonly RoadDatabase _roadDatabase;

        public RoadDatabaseProxy(RoadDatabase roadDatabase) : base("RoadDatabaseProxyThread", false)
        {
            _roadDatabase = roadDatabase;
        }

        public Task<List<PathQuantisized>> Query(MyRectangle query)
        {
            var tcs = new TaskCompletionSource<List<PathQuantisized>>();
            PostAction(async () => tcs.SetResult(await _roadDatabase.Query(query)));
            return tcs.Task;
        }
    }
}