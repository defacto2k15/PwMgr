using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass.Lod
{
    class GrassLodManager
    {
        private readonly LodLevelResolver _lodLevelResolver;
        private Vector2 _terrainSize;
        private Vector2 _groupSize;

        private readonly Dictionary<MapAreaPosition, LodGroupQueue> _groups =
            new Dictionary<MapAreaPosition, LodGroupQueue>();

        private readonly MultithreadLodGroupsActioner _lodGroupsActioner;

        public GrassLodManager(LodLevelResolver lodLevelResolver, MultithreadLodGroupsActioner lodGroupsActioner,
            Vector2 terrainSize, Vector2 groupSize)
        {
            _lodLevelResolver = lodLevelResolver;
            _terrainSize = terrainSize;
            _groupSize = groupSize;
            _lodGroupsActioner = lodGroupsActioner;
        }

        public void InitializeGroups(Vector3 cameraPosition)
        {
            int groupXCount = (int) (_terrainSize.x / _groupSize.x);
            int groupYCount = (int) (_terrainSize.y / _groupSize.y);

            for (int x = 0; x < groupXCount; x++)
            {
                for (int y = 0; y < groupYCount; y++)
                {
                    var groupDownLeftPoint = new Vector3(_groupSize.x * (x), 0,
                        _groupSize.y * (y));
                    var groupCenter = new Vector3(_groupSize.x * (x + 0.5f), 0,
                        _groupSize.y * (y + 0.5f)); //todo set y when have heightmap!
                    var mapAreaPosition = new MapAreaPosition(_groupSize, groupDownLeftPoint);
                    _groups.Add(mapAreaPosition, new LodGroupQueue(mapAreaPosition));
                }
            }

            var generationResults
                = _lodGroupsActioner.OrderGeneration(
                    _groups.Select(
                            c => new LodGroupGenerationOrderData(
                                c.Key, _lodLevelResolver.Resolve(cameraPosition, c.Value.MapAreaPosition.Center)))
                        .ToList());

            generationResults.ForEach(c => _groups[c.Position].CurrentGroup = c.LodGroup);
            foreach (var group in _groups.Values)
            {
                group.CurrentGroup.Enable();
            }
        }

        public void UpdateLod(Vector3 cameraPosition)
        {
            foreach (var pair in _groups)
            {
                var currentLodQueue = pair.Value;
                var newLodLevel = _lodLevelResolver.Resolve(cameraPosition, currentLodQueue.CurrentGroup.Position,
                    currentLodQueue.CurrentGroup.LodLevel);
                if (currentLodQueue.NewGroupBeingCreated)
                {
                    if (currentLodQueue.LodOfGroupBeingCreated == newLodLevel)
                    {
                        //ok
                    }
                    else
                    {
                        if (currentLodQueue.CurrentGroupLod == newLodLevel)
                        {
                            // we dont need new group
                            currentLodQueue.ResetNewGroupBeingCreated();
                            _lodGroupsActioner.OrderDropUpdate(currentLodQueue);
                        }
                        else
                        {
                            // we need different new group
                            currentLodQueue.SetNewGroupBeingCreated(newLodLevel);
                            _lodGroupsActioner.OrderDropUpdate(currentLodQueue);
                            _lodGroupsActioner.OrderUpdate(currentLodQueue.CurrentGroup.Position,
                                currentLodQueue.CurrentGroup, newLodLevel);
                        }
                    }
                }
                else
                {
                    if (currentLodQueue.CurrentGroupLod == newLodLevel)
                    {
                        // ok, dont change
                    }
                    else
                    {
                        currentLodQueue.SetNewGroupBeingCreated(newLodLevel);
                        _lodGroupsActioner.OrderDropUpdate(currentLodQueue);
                        _lodGroupsActioner.OrderUpdate(currentLodQueue.CurrentGroup.Position,
                            currentLodQueue.CurrentGroup, newLodLevel);
                    }
                }
            }
        }

        public void UpdateNewGroups()
        {
            var newGroups = _lodGroupsActioner.GetUpdatedLodGroups();
            foreach (LodGroup group in newGroups)
            {
                var groupQueue = _groups[group.Position];
                if (groupQueue.NewGroupBeingCreated && group.LodLevel == groupQueue.LodOfGroupBeingCreated)
                {
                    groupQueue.CurrentGroup.Remove();
                    groupQueue.CurrentGroup = group;
                    groupQueue.ResetNewGroupBeingCreated();
                    group.Enable();
                }
                else
                {
                    // we don't need it
                    group.Remove();
                }
            }
        }

        internal class LodGroupQueue
        {
            private readonly MapAreaPosition _mapAreaPosition; //todo set as sector
            private LodGroup _currentGroup;
            private int? _groupBeingCreatedLodLevel = null;

            public LodGroupQueue(LodGroup currentGroup)
            {
                this._currentGroup = currentGroup;
            }

            public LodGroupQueue(MapAreaPosition mapAreaPosition)
            {
                _mapAreaPosition = mapAreaPosition;
            }

            public LodGroup CurrentGroup
            {
                get { return _currentGroup; }
                set { _currentGroup = value; }
            }

            public MapAreaPosition MapAreaPosition
            {
                get { return _mapAreaPosition; }
            }

            public bool NewGroupBeingCreated
            {
                get { return _groupBeingCreatedLodLevel.HasValue; }
            }

            public int LodOfGroupBeingCreated
            {
                get { return _groupBeingCreatedLodLevel.Value; }
            }

            public int CurrentGroupLod
            {
                get { return CurrentGroup.LodLevel; }
            }

            public void ResetNewGroupBeingCreated()
            {
                _groupBeingCreatedLodLevel = null;
            }

            public void SetNewGroupBeingCreated(int newLodLevel)
            {
                _groupBeingCreatedLodLevel = newLodLevel;
            }
        }

        internal class MultithreadLodGroupsActioner
        {
            private readonly ILodGroupsProvider _lodGroupsProvider;
            private readonly MyThreadSafeList<LodGroup> _createdGroups = new MyThreadSafeList<LodGroup>();

            public MultithreadLodGroupsActioner(ILodGroupsProvider lodGroupsProvider)
            {
                this._lodGroupsProvider = lodGroupsProvider;
            }

            public void OrderUpdate(MapAreaPosition position, LodGroup currentGroup, int newLodLevel)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback((obj) =>
                {
                    LodGroupUpdateOrderData data = (LodGroupUpdateOrderData) obj;
                    LodGroup newGroup = data.CurrentGroup.UpdateLod(data.NewLodLevel);
                    _createdGroups.Add(newGroup);
                }), new LodGroupUpdateOrderData(position, currentGroup, newLodLevel));

                //_createdGroups.Add( currentGroup.UpdateLod(newLodLevel));
            }

            public List<LodGroup> GetUpdatedLodGroups()
            {
                var updatedLodGroups = _createdGroups.TakeAll();
                return updatedLodGroups;
            }

            public List<LodGroupGenerationResultData> OrderGeneration(
                List<LodGroupGenerationOrderData> generationOrderDatas)
            {
                var generationResult = new MyThreadSafeList<LodGroupGenerationResultData>(); //todo concurrent
                foreach (var order in generationOrderDatas)
                {
                    var newGroup = _lodGroupsProvider.GenerateLodGroup(order.MapAreaPosition);
                    newGroup.InitializeGroup(order.LodLevel);
                    generationResult.Add(new LodGroupGenerationResultData(newGroup, order.MapAreaPosition));
                }
                return generationResult.TakeAll();
            }

            public void OrderDropUpdate(LodGroupQueue currentLodQueue)
            {
                //throw new System.NotImplementedException(); //todo
            }

            public class LodGroupGenerationResultData
            {
                private readonly LodGroup _lodGroup;
                private readonly MapAreaPosition _position;

                public LodGroupGenerationResultData(LodGroup lodGroup, MapAreaPosition position)
                {
                    this._lodGroup = lodGroup;
                    this._position = position;
                }

                public LodGroup LodGroup
                {
                    get { return _lodGroup; }
                }

                public MapAreaPosition Position
                {
                    get { return _position; }
                }
            }

            private class LodGroupUpdateOrderData
            {
                private readonly MapAreaPosition _position;
                private readonly LodGroup _currentGroup;
                private readonly int _newLodLevel;

                public LodGroupUpdateOrderData(MapAreaPosition position, LodGroup currentGroup, int newLodLevel)
                {
                    this._position = position;
                    this._currentGroup = currentGroup;
                    this._newLodLevel = newLodLevel;
                }

                public MapAreaPosition Position
                {
                    get { return _position; }
                }

                public LodGroup CurrentGroup
                {
                    get { return _currentGroup; }
                }

                public int NewLodLevel
                {
                    get { return _newLodLevel; }
                }
            }
        }

        public class LodGroupGenerationOrderData
        {
            private readonly MapAreaPosition _mapAreaPosition;
            private readonly int _lodLevel;

            public LodGroupGenerationOrderData(MapAreaPosition mapAreaPosition, int lodLevel)
            {
                this._mapAreaPosition = mapAreaPosition;
                this._lodLevel = lodLevel;
            }

            public MapAreaPosition MapAreaPosition
            {
                get { return _mapAreaPosition; }
            }

            public int LodLevel
            {
                get { return _lodLevel; }
            }
        }
    }
}