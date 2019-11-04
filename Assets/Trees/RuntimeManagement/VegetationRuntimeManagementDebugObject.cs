using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Ring2;
using Assets.TerrainMat;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.DesignBodyDetails.BucketsContainer;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.Generation;
using Assets.Trees.RuntimeManagement.Management;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using Assets.Trees.RuntimeManagement.TerrainShape;
using Assets.Utils;
using Assets.Utils.MT;
using GeoAPI.Geometries;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;
using UnityEngine.Profiling;

namespace Assets.Trees.RuntimeManagement
{
    public class VegetationRuntimeManagementDebugObject : MonoBehaviour
    {
        private VegetationRuntimeManagement _runtimeManagement;
        private VegetationRuntimeManagementProxy _vegetationRuntimeManagementProxy;
        private DummyVegetationSubjectInstanceContainer _dummyVegetationSubjectInstanceContainer;
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

            _dummyVegetationSubjectInstanceContainer = new DummyVegetationSubjectInstanceContainer();
            _forgingContainerProxy = new ForgingVegetationSubjectInstanceContainerProxy(
                new ForgingVegetationSubjectInstanceContainer(
                    new DesignBodyPortrayalForger(
                        representationContainer,
                        instanceBucketsContainer)));

            _runtimeManagement = new VegetationRuntimeManagement(
                positionsProvider: CreateSamplePositionsDatabase(),
                vegetationSubjectsChangesListener: _forgingContainerProxy,
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
                }
            );
            Profiler.EndSample();

            _vegetationRuntimeManagementProxy = new VegetationRuntimeManagementProxy(_runtimeManagement);
            _forgingContainerProxy.StartThreading(() => { });
        }

        private bool _once = false;

        public void Update()
        {
            var newPosition = Camera.main.transform.position;
            if (!_once)
            {
                _once = true;
                _vegetationRuntimeManagementProxy.Start(newPosition);
                _vegetationRuntimeManagementProxy.StartThreading();
                _globalInstancingContainer.StartThread();
            }
            else
            {
                _globalInstancingContainer.DrawFrame();
                _vegetationRuntimeManagementProxy.AddUpdate(newPosition);
                Debug.Log("NEWPOS is "+newPosition);
                const int maxMsPerFrame = 4000;
                _dummyVegetationSubjectInstanceContainer.Update(maxMsPerFrame);

                _globalInstancingContainer.FinishUpdateBatch();

                // synchro
                _vegetationRuntimeManagementProxy.SynchronicUpdate(newPosition);
            }
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
                            Size = 1,
                            SpeciesEnum = VegetationSpeciesEnum.Tree1A
                        });
                    tree.Insert(MyNetTopologySuiteUtils.ToPointEnvelope(newEntity.Position2D), newEntity);
                }
            }
            return new VegetationSubjectsPositionsDatabase(tree);

            //var tree = new Quadtree<VegetationSubjectEntity>();

            //var newEntity = new VegetationSubjectEntity(
            //    new DesignBodyLevel0Detail()
            //    {
            //        Pos2D = new Vector2(120, 120),
            //        Radius = 0,
            //        Size = 1,
            //        SpeciesEnum = VegetationSpeciesEnum.Tree1A
            //    });
            //tree.Insert(MyNetTopologySuiteUtils.ToPointEnvelope(newEntity.Position2D), newEntity);

            return new VegetationSubjectsPositionsDatabase(tree);
        }
    }

    //public class GridVegetationSubjectsPositionsDatabase : IVegetationSubjectsPositionsProvider
    //{
    //    private readonly float _cellLength;

    //    private Dictionary<VegetationGridPosition, List<VegetationSubjectEntity>> _grid 
    //        = new Dictionary<VegetationGridPosition, List<VegetationSubjectEntity>>();

    //    public GridVegetationSubjectsPositionsDatabase(List<VegetationSubjectEntity> entities, float cellLength)
    //    {
    //        _cellLength = cellLength;
    //        foreach (var entity in entities)
    //        {
    //            var position = FindGridCell(entity.Position2D);
    //            if (!_grid.ContainsKey(position))
    //            {
    //                _grid.Add(position, new List<VegetationSubjectEntity>());
    //            }
    //            _grid[position].Add(entity);
    //        }
    //    }

    //    private VegetationGridPosition FindGridCell(Vector2 position)
    //    {
    //        var divised = new Vector2(position.x / _cellLength, position.y / _cellLength);
    //        var gridPos = new IntVector2(Mathf.RoundToInt(divised.x), Mathf.RoundToInt(divised.y));
    //        return new VegetationGridPosition(gridPos, _cellLength);
    //    }

    //    public List<VegetationSubjectEntity> GetEntiesFrom(VegetationQueryArea area, VegetationDetailLevel level)
    //    {
    //    }
    //}

    //public class VegetationQueryArea
    //{
        
    //}

    //public class VegetationGridRectangle
    //{
    //    private IntRectangle _cellsRect;
    //    private float _cellLength;
    //}

    //public class VegetationGridPosition
    //{
    //    private IntVector2 _cellPosition;
    //    private float _cellLength;

    //    public VegetationGridPosition(IntVector2 cellPosition, float cellLength)
    //    {
    //        _cellPosition = cellPosition;
    //        _cellLength = cellLength;
    //    }

    //    public IntVector2 CellPosition => _cellPosition;

    //    protected bool Equals(VegetationGridPosition other)
    //    {
    //        return _cellPosition.Equals(other._cellPosition);
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        if (ReferenceEquals(null, obj)) return false;
    //        if (ReferenceEquals(this, obj)) return true;
    //        if (obj.GetType() != this.GetType()) return false;
    //        return Equals((VegetationGridPosition) obj);
    //    }

    //    public override int GetHashCode()
    //    {
    //        return _cellPosition.GetHashCode();
    //    }
    //}
}