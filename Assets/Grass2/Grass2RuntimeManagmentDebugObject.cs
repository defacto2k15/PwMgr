using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Grass2.Growing;
using Assets.Grass2.Planting;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Roads.Files;
using Assets.Roads.Pathfinding.Fitting;
using Assets.TerrainMat;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.DesignBodyDetails.BucketsContainer;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.Generation;
using Assets.Trees.RuntimeManagement;
using Assets.Trees.RuntimeManagement.Management;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;
using BaseOtherThreadProxy = Assets.Utils.MT.BaseOtherThreadProxy;

namespace Assets.Grass2
{ //todo przy szybkim ruszaniu kamerą są exceptiony, i to bez multithreadingu!!!
    public class Grass2RuntimeManagmentDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        private VegetationRuntimeManagement _runtimeManagement;
        private VegetationRuntimeManagementProxy _vegetationRuntimeManagementProxy;
        private GlobalGpuInstancingContainer _globalInstancingContainer;
        private ForgingVegetationSubjectInstanceContainerProxy _forgingContainerProxy;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            TreePrefabManager prefabManager = new TreePrefabManager();
            TreeClan clan = prefabManager.LoadTreeClan("clan1");

            _globalInstancingContainer = new GlobalGpuInstancingContainer();
            var representationContainer = new DesignBodyRepresentationContainer();
            DesignBodyInstanceBucketsContainer instanceBucketsContainer =
                new DesignBodyInstanceBucketsContainer(_globalInstancingContainer);

            var quadBillboardMesh = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshFilter>().mesh;
            var shifter = new TreeClanToDetailProviderShifter(new DetailProviderRepository(), quadBillboardMesh,
                representationContainer, instanceBucketsContainer);
            shifter.AddClan(clan, VegetationSpeciesEnum.Tree1A);

            _forgingContainerProxy = new ForgingVegetationSubjectInstanceContainerProxy(
                new ForgingVegetationSubjectInstanceContainer(
                    new DesignBodyPortrayalForger(
                        representationContainer,
                        instanceBucketsContainer)));

            ///// GRASSING!!!!!

            var singleGenerationArea = new Vector2(10, 10);
            var positionsProvider = new CompositeVegetationSubjectsPositionProvider(
                new List<IVegetationSubjectsPositionsProvider>()
                {
                    //CreateSamplePositionsDatabase(),
                    new GrassVegetationSubjectsPositionsGenerator(
                        new GrassVegetationSubjectsPositionsGenerator.
                            GrassVegetationSubjectsPositionsGeneratorConfiguration()
                            {
                                PositionsGridSize = singleGenerationArea
                            })
                });

            _debugGrassGroupsGrowerUnderTest = new DebugGrassGroupsGrowerUnderTest(new DebugGrassPlanterUnderTest());
            _debugGrassGroupsGrowerUnderTest.Start(ComputeShaderContainer);

            GrassGroupsGrower grassGroupsGrower = _debugGrassGroupsGrowerUnderTest.Grower;
            Grass2RuntimeManager grass2RuntimeManager = new Grass2RuntimeManager(grassGroupsGrower,
                new Grass2RuntimeManager.Grass2RuntimeManagerConfiguration()
                {
                    GroupSize = singleGenerationArea
                });
            var vegetationSubjectsChangesListener = new CompositeVegetationSubjectsChangesListener(
                new List<VegetationSubjectsInstancingChangeListenerWithFilter>()
                {
                    new VegetationSubjectsInstancingChangeListenerWithFilter()
                    {
                        ChangeListener = new Grass2RuntimeManagerProxy(grass2RuntimeManager),
                        Filter = (entity => entity.Detail.SpeciesEnum == VegetationSpeciesEnum.Grass2SpotMarker)
                    },
                    //new VegetationSubjectsInstancingChangeListenerWithFilter()
                    //{
                    //    ChangeListener = _forgingContainerProxy,
                    //    Filter = entity => true
                    //}
                });

            //////

            _runtimeManagement = new VegetationRuntimeManagement(
                positionsProvider: positionsProvider,
                vegetationSubjectsChangesListener: vegetationSubjectsChangesListener,
                visibleEntitiesContainer: new VegetationSubjectsVisibleEntitiesContainer(),
                configuration: new VegetationRuntimeManagementConfiguration()
                {
                    DetailFieldsTemplate = new CenterHolesDetailFieldsTemplate(new List<DetailFieldsTemplateOneLine>()
                    {
                        new DetailFieldsTemplateOneLine(VegetationDetailLevel.FULL, 0, 60),
                        new DetailFieldsTemplateOneLine(VegetationDetailLevel.REDUCED, 40, 120),
                        new DetailFieldsTemplateOneLine(VegetationDetailLevel.BILLBOARD, 100, 280),
                    }),
                    UpdateMinDistance = 10
                });

            _vegetationRuntimeManagementProxy = new VegetationRuntimeManagementProxy(_runtimeManagement);
            _forgingContainerProxy.StartThreading(() => { });
        }

        private bool _once = false;
        private DebugGrassGroupsGrowerUnderTest _debugGrassGroupsGrowerUnderTest;

        public void Update()
        {
            var newPosition = Camera.main.transform.position;
            if (!_once)
            {
                var msw = new MyStopWatch();
                msw.StartSegment("Starting segment.");
                _once = true;
                _vegetationRuntimeManagementProxy.Start(newPosition);
                _vegetationRuntimeManagementProxy.StartThreading();
                //_globalInstancingContainer.StartThread();
                _debugGrassGroupsGrowerUnderTest.FinalizeStart();
                Debug.Log("L8: segment " + msw.CollectResults());
            }
            else
            {
                //_globalInstancingContainer.DrawFrame();
                _vegetationRuntimeManagementProxy.AddUpdate(newPosition);
                const int maxMsPerFrame = 4000;

                //_globalInstancingContainer.FinishUpdateBatch();
            }
            _debugGrassGroupsGrowerUnderTest.Update();
        }

        private VegetationSubjectsPositionsDatabase CreateSamplePositionsDatabase()
        {
            var tree = new Quadtree<VegetationSubjectEntity>();
            for (int x = 0; x < 4000; x += 30)
            {
                for (int y = 0; y < 4000; y += 30)
                {
                    var newEntity = new VegetationSubjectEntity(
                        new DesignBodyLevel0Detail()
                        {
                            Pos2D = new Vector2(x, y),
                            Radius = 0,
                            Size = 0,
                            SpeciesEnum = VegetationSpeciesEnum.Tree1A
                        });
                    tree.Insert(MyNetTopologySuiteUtils.ToPointEnvelope(newEntity.Position2D), newEntity);
                }
            }
            return new VegetationSubjectsPositionsDatabase(tree);
        }
    }

    public class CompositeVegetationSubjectsChangesListener : IVegetationSubjectInstancingContainerChangeListener
    {
        private List<VegetationSubjectsInstancingChangeListenerWithFilter> _listenersWithFilters;

        public CompositeVegetationSubjectsChangesListener(
            List<VegetationSubjectsInstancingChangeListenerWithFilter> listenersWithFilters)
        {
            _listenersWithFilters = listenersWithFilters;
        }

        public void AddInstancingOrder(
            VegetationDetailLevel level,
            List<VegetationSubjectEntity> gainedEntities,
            List<VegetationSubjectEntity> lostEntities)
        {
            var gainedLists = new List<List<VegetationSubjectEntity>>();
            var lostLists = new List<List<VegetationSubjectEntity>>();
            for (int i = 0; i < _listenersWithFilters.Count; i++)
            {
                gainedLists.Add(new List<VegetationSubjectEntity>());
                lostLists.Add(new List<VegetationSubjectEntity>());
            }

            foreach (var entity in gainedEntities)
            {
                int i = 0;
                bool foundListener = false;
                foreach (var listenerWithFilter in _listenersWithFilters)
                {
                    if (listenerWithFilter.Filter(entity))
                    {
                        gainedLists[i].Add(entity);
                        foundListener = true;
                        break;
                    }
                    i++;
                }
                if (!foundListener)
                {
                    Debug.LogError("E41. No listener accepted entity " + entity);
                }
            }

            foreach (var entity in lostEntities)
            {
                int i = 0;
                bool foundListener = false;
                foreach (var listenerWithFilter in _listenersWithFilters)
                {
                    if (listenerWithFilter.Filter(entity))
                    {
                        lostLists[i].Add(entity);
                        foundListener = true;
                        break;
                    }
                    i++;
                }
                if (!foundListener)
                {
                    Debug.LogError("E42. No listener accepted entity " + entity);
                }
            }

            int k = 0;
            foreach (var listener in _listenersWithFilters.Select(c => c.ChangeListener))
            {
                var gained = gainedLists[k];
                var lost = lostLists[k];
                if (gained.Any() || lost.Any())
                {
                    listener.AddInstancingOrder(level, gained, lost);
                }
                k++;
            }
        }
    }

    public class VegetationSubjectsInstancingChangeListenerWithFilter
    {
        public IVegetationSubjectInstancingContainerChangeListener ChangeListener;
        public Predicate<VegetationSubjectEntity> Filter;
    }

    public class Grass2RuntimeManagerProxy : BaseOtherThreadProxy, IVegetationSubjectInstancingContainerChangeListener
    {
        private readonly Grass2RuntimeManager _grass2RuntimeManager;

        public Grass2RuntimeManagerProxy(Grass2RuntimeManager grass2RuntimeManager)
            : base("Grass2RuntimeManagerProxyThread", false)
        {
            _grass2RuntimeManager = grass2RuntimeManager;
        }

        public void AddInstancingOrder(
            VegetationDetailLevel level,
            List<VegetationSubjectEntity> gainedEntities,
            List<VegetationSubjectEntity> lostEntities)
        {
            PostChainedAction(
                () => _grass2RuntimeManager.AddInstancingOrderAsync(level, gainedEntities, lostEntities));
        }
    }

    public class Grass2RuntimeManager
    {
        private GrassGroupsGrower _grassGroupsGrower;
        private Grass2RuntimeManagerConfiguration _configuration;
        private Dictionary<int, GrassBandInfo> _entityToGrassBand = new Dictionary<int, GrassBandInfo>();

        public Grass2RuntimeManager(GrassGroupsGrower grassGroupsGrower,
            Grass2RuntimeManagerConfiguration configuration)
        {
            _grassGroupsGrower = grassGroupsGrower;
            _configuration = configuration;
        }

        public async Task AddInstancingOrderAsync(
            VegetationDetailLevel level,
            List<VegetationSubjectEntity> gainedEntities,
            List<VegetationSubjectEntity> lostEntities)
        {
            Debug.Log("G473 Gained: "+gainedEntities.Count);
            foreach (var entity in gainedEntities)
            {
                Preconditions.Assert(entity.Detail.SpeciesEnum == VegetationSpeciesEnum.Grass2SpotMarker,
                    $"Given entity is not of type spotMarker. It is {entity.Detail.SpeciesEnum}");
                var position = entity.Position2D;

                var generationArea = MyRectangle.CenteredAt(position, _configuration.GroupSize);
                var grassBandInfo = await _grassGroupsGrower.GrowGrassBandAsync(generationArea);

                _entityToGrassBand[entity.Id] = grassBandInfo;
            }
            foreach (var entity in lostEntities)
            {
                var id = entity.Id;
                var bandInfo = _entityToGrassBand[id];
                _entityToGrassBand.Remove(id);
                _grassGroupsGrower.RemoveGrassBand(bandInfo);
            }
        }

        public class Grass2RuntimeManagerConfiguration
        {
            public Vector2 GroupSize;
        }
    }

    public class CompositeVegetationSubjectsPositionProvider : IVegetationSubjectsPositionsProvider
    {
        private List<IVegetationSubjectsPositionsProvider> _sources;

        public CompositeVegetationSubjectsPositionProvider(List<IVegetationSubjectsPositionsProvider> sources)
        {
            _sources = sources;
        }

        public List<VegetationSubjectEntity> GetEntiesFrom(IGeometry area, VegetationDetailLevel level)
        {
            return _sources.SelectMany(c => c.GetEntiesFrom(area, level)).ToList();
        }
    }

    public class GrassVegetationSubjectsPositionsGenerator : IVegetationSubjectsPositionsProvider
    {
        private GrassVegetationSubjectsPositionsGeneratorConfiguration _configuration;

        public GrassVegetationSubjectsPositionsGenerator(
            GrassVegetationSubjectsPositionsGeneratorConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<VegetationSubjectEntity> GetEntiesFrom(IGeometry area, VegetationDetailLevel level)
        {
            var outPositions = new List<Vector2>();

            if (level == VegetationDetailLevel.FULL)
            {
                var envelope = area.EnvelopeInternal;
                var gridPositionsStart = new IntVector2(
                    Mathf.CeilToInt((float) envelope.MinX / _configuration.PositionsGridSize.x),
                    Mathf.CeilToInt((float) envelope.MinY / _configuration.PositionsGridSize.y));

                var afterGridingLength = new Vector2(
                    (float) (envelope.MaxX - gridPositionsStart.X * _configuration.PositionsGridSize.x),
                    (float) (envelope.MaxY - gridPositionsStart.Y * _configuration.PositionsGridSize.y));

                var gridLength = new IntVector2( //ceil becouse there is point at length 0 !!
                    Mathf.CeilToInt(afterGridingLength.x / _configuration.PositionsGridSize.x),
                    Mathf.CeilToInt(afterGridingLength.y / _configuration.PositionsGridSize.y));

                for (int x = 0; x < gridLength.X; x++)
                {
                    for (int y = 0; y < gridLength.Y; y++)
                    {
                        var position = new Vector2(
                            (0.5f + (gridPositionsStart.X + x)) * _configuration.PositionsGridSize.x,
                            (0.5f + (gridPositionsStart.Y + y)) * _configuration.PositionsGridSize.y);

                        outPositions.Add(position);
                    }
                }
            }

            return outPositions.Where(c => area.Contains(MyNetTopologySuiteUtils.ToGeometryEnvelope(
                MyNetTopologySuiteUtils.ToPointEnvelope(c)))).Select(c => new VegetationSubjectEntity(
                new DesignBodyLevel0Detail()
                {
                    Pos2D = c,
                    Radius = 0,
                    Size = 0,
                    SpeciesEnum = VegetationSpeciesEnum.Grass2SpotMarker
                })).ToList();
        }

        public class GrassVegetationSubjectsPositionsGeneratorConfiguration
        {
            public Vector2 PositionsGridSize = new Vector2(10, 10);
        }
    }
}