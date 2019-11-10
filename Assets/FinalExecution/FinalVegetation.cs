using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.ComputeShaders;
using Assets.Grass;
using Assets.Grass2;
using Assets.Grass2.Billboards;
using Assets.Grass2.GrassIntensityMap;
using Assets.Grass2.Groups;
using Assets.Grass2.Growing;
using Assets.Grass2.IntenstityDb;
using Assets.Grass2.Planting;
using Assets.Grass2.PositionResolving;
using Assets.Grass2.Types;
using Assets.Habitat;
using Assets.Heightmaps;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Repositioning;
using Assets.Ring2;
using Assets.Roads;
using Assets.ShaderUtils;
using Assets.TerrainMat;
using Assets.Trees.Db;
using Assets.Trees.DesignBodyDetails;
using Assets.Trees.DesignBodyDetails.BucketsContainer;
using Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics;
using Assets.Trees.DesignBodyDetails.DetailProvider;
using Assets.Trees.Generation;
using Assets.Trees.Placement;
using Assets.Trees.RuntimeManagement;
using Assets.Trees.RuntimeManagement.Management;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Global;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.InstanceThread;
using Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.UnityThread;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using Assets.Utils.Spatial;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;
using NetTopologySuite.Index.Quadtree;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class FinalVegetation
    {
        private readonly GameInitializationFields _initializationFields;
        private readonly UltraUpdatableContainer _ultraUpdatableContainer;
        private readonly FinalVegetationConfiguration _veConfiguration;

        public FinalVegetation(GameInitializationFields initializationFields,
            UltraUpdatableContainer ultraUpdatableContainer, FinalVegetationConfiguration veConfiguration)
        {
            this._initializationFields = initializationFields;
            this._ultraUpdatableContainer = ultraUpdatableContainer;
            _veConfiguration = veConfiguration;
        }

        public void Start()
        {
            if (_veConfiguration.GenerateGrass || _veConfiguration.GenerateSmallBushes)
            {
                CreateGrass2IntensityDb();
            }
            if (_veConfiguration.GenerateTrees || _veConfiguration.GenerateBigBushes)
            {
                StartTreesRuntimeManagment();
            }
            if (_veConfiguration.GenerateGrass)
            {
                StartGrassRuntimeManagmentSource();
            }
            if (_veConfiguration.GenerateSmallBushes)
            {
                StartBushVegetation();
            }
        }

        private void StartTreesRuntimeManagment()
        {
            var globalInstancingContainer = _initializationFields.Retrive<GlobalGpuInstancingContainer>();
            var representationContainer = new DesignBodyRepresentationContainer();
            var instanceBucketsContainer = new DesignBodyInstanceBucketsContainer(globalInstancingContainer);

            var quadBillboardMesh = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshFilter>().mesh;

            IDesignBodyRepresentationInstanceCombinationProvider combinationProvider;
            if (_veConfiguration.Mode == VegetationMode.Legacy)
            {
                var gShifter = new GTreeDetailProviderShifter(new DetailProviderRepository(), quadBillboardMesh);
                var treeFileManager = new TreeFileManager(new TreeFileManagerConfiguration()
                {
                    WritingTreeCompletedClanDirectory = _veConfiguration.TreeCompletedClanDirectiory
                });
                combinationProvider = new GDesignBodyRepresentationInstanceCombinationProvider(treeFileManager, gShifter);
            }
            else
            {
                var eVegetationShifter = new EVegetationDetailProviderShifter(new DetailProviderRepository(), quadBillboardMesh,
                    _veConfiguration.ReferencedAssets);
                combinationProvider = new EVegetationDesignBodyRepresentationInstanceCombinationProvider(new TreePrefabManager(), eVegetationShifter, _veConfiguration.ReferencedAssets);
            }

            foreach (var pair in _veConfiguration.ShiftingConfigurations.Where(c=>  _veConfiguration.SupportedVegetationSpecies.Contains(c.Key)))
            {
                var clanRepresentations = combinationProvider.CreateRepresentations(pair.Value, pair.Key);
                representationContainer.InitializeLists(clanRepresentations);
                instanceBucketsContainer.InitializeLists(clanRepresentations);
            }

            var designBodySpotUpdaterProxy = _initializationFields.Retrive<DesignBodySpotUpdaterProxy>();
            var mediatorSpotUpdater = new ListenerCenteredMediatorDesignBodyChangesUpdater(designBodySpotUpdaterProxy);

            var rootMediator = _initializationFields.Retrive<RootMediatorSpotPositionsUpdater>();
            rootMediator.AddListener(mediatorSpotUpdater);

            var repositioner = _initializationFields .Retrive<Repositioner >(); 
            var forgingContainerProxy = new ForgingVegetationSubjectInstanceContainerProxy(
                new ForgingVegetationSubjectInstanceContainer(
                    new DesignBodyPortrayalForger(
                        representationContainer,
                        instanceBucketsContainer,
                        repositioner),
                    mediatorSpotUpdater//teraz napisz tak, zeby info zwrotne se spotupdatera wracalo zgodnie z multithreadingiem (szlo do innego watku!)
                ));

            mediatorSpotUpdater.SetTargetChangesListener(new LambdaSpotPositionChangesListener(dict =>
            {
                forgingContainerProxy.AddSpotModifications(dict);
            }));

            _ultraUpdatableContainer.AddOtherThreadProxy(forgingContainerProxy);

            MyProfiler.BeginSample("Vegetation1: Loading from file");
            var baseVegetationList = VegetationDatabaseFileUtils.LoadListFromFiles(_veConfiguration.LoadingVegetationDatabaseDictionaryPath);
            MyProfiler.EndSample();

            if (_veConfiguration.GenerateTrees)
            {
                MyProfiler.BeginSample("Vegetation2: pushingToFile");
                foreach (var pair in _veConfiguration.PerRankVegetationRuntimeManagementConfigurations)
                {
                    var rank = pair.Key;
                    var managementConfiguration = pair.Value;

                    var baseRankedDb = baseVegetationList[rank];

                    var supportedSpecies = _veConfiguration.SupportedTreeSpecies;

                    var filteredEntities = baseRankedDb.Where(c => supportedSpecies.Contains(c.Detail.SpeciesEnum));

                    var stagnantEntities = new List<VegetationSubjectEntity>();
                    var nonStagnantEntities = new List<VegetationSubjectEntity>();

                    var nonStagnantVegetationRect = _veConfiguration.NonStagnantVegetationArea;
                    foreach (var entity in filteredEntities)
                    {
                        if (nonStagnantVegetationRect.Contains(entity.Position2D))
                        {
                            nonStagnantEntities.Add(entity);
                        }
                        else
                        {
                            stagnantEntities.Add(entity);
                        }
                    }

                    var stagnantVegetationRuntimaManagement = new StagnantVegetationRuntimeManagement(forgingContainerProxy,
                        stagnantEntities, _veConfiguration.StagnantVegetationRuntimeManagementConfiguration);
                    var stagnantVegetationRuntimaManagementProxy = new StagnantVegetationRuntimeManagementProxy(stagnantVegetationRuntimaManagement);

                    _ultraUpdatableContainer.AddUpdatableElement(new FieldBasedUltraUpdatable()
                    {
                        StartField = () => { stagnantVegetationRuntimaManagementProxy.StartThreading(); }
                    });


                    var quadtree = new Quadtree<VegetationSubjectEntity>();
                    foreach (var entity in nonStagnantEntities)
                    {
                        quadtree.Insert(MyNetTopologySuiteUtils.ToPointEnvelope(entity.Position2D), entity);
                    }

                    var positionsProvider = new VegetationSubjectsPositionsDatabase(quadtree);

                    var runtimeManagement = new VegetationRuntimeManagement(
                        positionsProvider: positionsProvider,
                        vegetationSubjectsChangesListener: forgingContainerProxy,
                        visibleEntitiesContainer: new VegetationSubjectsVisibleEntitiesContainer(),
                        configuration: managementConfiguration);

                    var outerVegetationRuntimeManagementProxy = new VegetationRuntimeManagementProxy(runtimeManagement);

                    _ultraUpdatableContainer.AddUpdatableElement(new FieldBasedUltraUpdatable()
                    {
                        StartCameraField = (camera) =>
                        {
                            outerVegetationRuntimeManagementProxy.StartThreading();
                            var position = camera.transform.localPosition;
                            outerVegetationRuntimeManagementProxy.Start(repositioner.InvMove(position));
                        },
                        UpdateCameraField = (camera) =>
                        {
                            var position = camera.transform.localPosition;
                            outerVegetationRuntimeManagementProxy.AddUpdate(repositioner.InvMove(position));
                            outerVegetationRuntimeManagementProxy.SynchronicUpdate(repositioner.InvMove(position));
                        },
                    });
                }
                MyProfiler.EndSample();
            }

            if (_veConfiguration.GenerateBigBushes)
            {
                InitializeBushObjectsDb(baseVegetationList[VegetationLevelRank.Small], forgingContainerProxy);
            }
        }

        private void InitializeBushObjectsDb(List<VegetationSubjectEntity> smallDb, ForgingVegetationSubjectInstanceContainerProxy forgingContainerProxy)
        {
            var repositioner = _initializationFields.Retrive<Repositioner>();

            var supportedSpecies = _veConfiguration.SupportedLeadingBushSpecies;

            var speciesChanger = new VegetationSpeciesRandomChanger(_veConfiguration.SpeciesChangingList, 661); //todo!

            var filteredEntities = smallDb
                .Where(c => supportedSpecies.Contains(c.Detail.SpeciesEnum))
                .Select(c => speciesChanger.ChangeSpecies(c));
            var quadtree = new Quadtree<VegetationSubjectEntity>();
            foreach (var entity in filteredEntities)
            {
                quadtree.Insert(MyNetTopologySuiteUtils.ToPointEnvelope(entity.Position2D), entity);
            }

            var positionsProvider = new VegetationSubjectsPositionsDatabase(quadtree);

            var runtimeManagement = new VegetationRuntimeManagement(
                positionsProvider: positionsProvider,
                vegetationSubjectsChangesListener: forgingContainerProxy,
                visibleEntitiesContainer: new VegetationSubjectsVisibleEntitiesContainer(),
                configuration: _veConfiguration.BushObjectsVegetationRuntimeManagementConfiguration);

            var vegetationRuntimeManagementProxy = new VegetationRuntimeManagementProxy(runtimeManagement);

            _ultraUpdatableContainer.AddUpdatableElement(new FieldBasedUltraUpdatable()
            {
                StartCameraField = (camera) =>
                {
                    vegetationRuntimeManagementProxy.StartThreading();
                    var position = camera.transform.localPosition;
                    vegetationRuntimeManagementProxy.Start(repositioner.InvMove(position));
                },
                UpdateCameraField = (camera) =>
                {
                    var position = camera.transform.localPosition;
                    vegetationRuntimeManagementProxy.AddUpdate(repositioner.InvMove(position));
                    vegetationRuntimeManagementProxy.SynchronicUpdate(repositioner.InvMove(position));
                },
            });
        }

        private void StartGrassRuntimeManagmentSource()
        {
            var positionsGenerator = new GrassVegetationSubjectsPositionsGenerator( _veConfiguration.GrassVegetationSubjectsPositionsGeneratorConfiguration);

            var grassIntensityDbProxy = _initializationFields.Retrive<Grass2IntensityDbProxy>();

            var otherThreadExecutingLocation = new OtherThreadExecutingLocation();

            var planter = CreateGrassGroupsPlanter(otherThreadExecutingLocation);
            var grower = new GrassGroupsGrower(planter, grassIntensityDbProxy);

            Grass2RuntimeManager grass2RuntimeManager = new Grass2RuntimeManager(grower, _veConfiguration.Grass2RuntimeManagerConfiguration);

            var grass2RuntimeManagerProxy = new Grass2RuntimeManagerProxy(grass2RuntimeManager);
            otherThreadExecutingLocation.SetExecutingTarget(grass2RuntimeManagerProxy);

            _ultraUpdatableContainer.AddOtherThreadProxy(grass2RuntimeManagerProxy);

            var runtimeManagement = new VegetationRuntimeManagement(
                positionsProvider: positionsGenerator,
                vegetationSubjectsChangesListener: grass2RuntimeManagerProxy,
                visibleEntitiesContainer: new VegetationSubjectsVisibleEntitiesContainer(),
                configuration: _veConfiguration.Grass2VegetationRuntimeManagementConfiguration);

            var vegetationRuntimeManagementProxy = new VegetationRuntimeManagementProxy(runtimeManagement);

            var repositioner = _initializationFields.Retrive<Repositioner>();
            _ultraUpdatableContainer.AddUpdatableElement(new FieldBasedUltraUpdatable()
            {
                StartCameraField = (camera) =>
                {
                    vegetationRuntimeManagementProxy.StartThreading();
                    var position = camera.transform.localPosition;
                    vegetationRuntimeManagementProxy.Start(repositioner.InvMove(position));
                },
                UpdateCameraField = (camera) =>
                {
                    var position = camera.transform.localPosition;
                    vegetationRuntimeManagementProxy.AddUpdate(repositioner.InvMove(position));
                    vegetationRuntimeManagementProxy.SynchronicUpdate(repositioner.InvMove(position));
                },
            });
        }

        private void CreateGrass2IntensityDb()
        {
            HabitatToGrassIntensityMapGenerator habitatToGrassIntensityMapGenerator =
                new HabitatToGrassIntensityMapGenerator(
                    _initializationFields.Retrive<ComputeShaderContainerGameObject>(),
                    _initializationFields.Retrive<UnityThreadComputeShaderExecutorObject>(),
                    _initializationFields.Retrive<CommonExecutorUTProxy>(),
                    _veConfiguration.HabitatToGrassIntensityMapGeneratorConfiguration);

            HabitatMapDbProxy habitatDbProxy = _initializationFields.Retrive<HabitatMapDbProxy>();

            var pathProximityTextureDbProxy = _initializationFields.Retrive<PathProximityTextureDbProxy>();

            var mapsGenerator = new Grass2IntensityMapGenerator(
                habitatToGrassIntensityMapGenerator,
                new HabitatTexturesGenerator(habitatDbProxy,
                    _veConfiguration.HabitatTexturesGeneratorConfiguration,
                    _initializationFields.Retrive<TextureConcieverUTProxy>()),
                _veConfiguration.Grass2IntensityMapGeneratorConfiguration,
                pathProximityTextureDbProxy);

            var db = new SpatialDb<List<Grass2TypeWithIntensity>>(mapsGenerator,
                _veConfiguration.Grass2IntensityDbConfiguration
            );

            var databaseProxy = new Grass2IntensityDbProxy(db);
            _ultraUpdatableContainer.AddOtherThreadProxy(databaseProxy);
            _ultraUpdatableContainer.AddOtherThreadProxy(habitatDbProxy);

            _initializationFields.SetField(databaseProxy);
        }

        private GrassGroupsPlanter CreateGrassGroupsPlanter(OtherThreadExecutingLocation otherThreadExecutingLocation)
        {
            var meshGenerator = new GrassMeshGenerator();
            var mesh = meshGenerator.GetGrassBladeMesh(1);

            var instancingMaterial = _veConfiguration.ReferencedAssets.GrassMaterial;

            var commonUniforms = new UniformsPack();
            commonUniforms.SetUniform("_BendingStrength", 0.6f);
            commonUniforms.SetUniform("_WindDirection", Vector4.one);

            var gpuInstancerCommonData = new GpuInstancerCommonData(mesh, instancingMaterial, commonUniforms);
            gpuInstancerCommonData.CastShadows = _veConfiguration.GrassCastShadows;
            var instancingContainer = new GpuInstancingVegetationSubjectContainer(
                gpuInstancerCommonData,
                new GpuInstancingUniformsArrayTemplate(new List<GpuInstancingUniformTemplate>()
                {
                    new GpuInstancingUniformTemplate("_Color", GpuInstancingUniformType.Vector4),
                    new GpuInstancingUniformTemplate("_InitialBendingValue", GpuInstancingUniformType.Float),
                    new GpuInstancingUniformTemplate("_PlantBendingStiffness", GpuInstancingUniformType.Float),
                    new GpuInstancingUniformTemplate("_PlantDirection", GpuInstancingUniformType.Vector4),
                    new GpuInstancingUniformTemplate("_RandSeed", GpuInstancingUniformType.Float),
                })
            );

            var globalGpuInstancingContainer = _initializationFields.Retrive<GlobalGpuInstancingContainer>();
            var bucketId = globalGpuInstancingContainer.CreateBucket(instancingContainer);
            var grassGroupsContainer = new GrassGroupsContainer(globalGpuInstancingContainer, bucketId);

            var grassPositionResolver = new SimpleRandomSamplerPositionResolver();

            var grassDetailInstancer = new GrassDetailInstancer();

            var designBodySpotUpdaterProxy = _initializationFields.Retrive<DesignBodySpotUpdaterProxy>();

            var mediatorSpotUpdater = new ListenerCenteredMediatorDesignBodyChangesUpdater(designBodySpotUpdaterProxy);

            IGrass2AspectsGenerator grassAspectGenerator;
            if (_veConfiguration.Mode == VegetationMode.Legacy)
            {
                grassAspectGenerator = new LegacyGrass2BladeAspectsGenerator();
            }
            else
            {
                grassAspectGenerator = new EVegetationGrass2BladeAspectsGenerator();
            }

            var grassGroupsPlanter = new GrassGroupsPlanter(
                grassDetailInstancer, grassPositionResolver,
                grassGroupsContainer,
                mediatorSpotUpdater,
                grassAspectGenerator,
                _veConfiguration.GrassTemplates,
                _initializationFields.Retrive<Repositioner>());

            mediatorSpotUpdater.SetTargetChangesListener(new LambdaSpotPositionChangesListener(null, dict =>
            {
                otherThreadExecutingLocation.Execute(() =>
                {
                    foreach (var pair in dict)
                    {
                        grassGroupsPlanter.GrassGroupSpotChanged(pair.Key, pair.Value);
                    }
                    return TaskUtils.EmptyCompleted();
                });
            }));

            var rootMediator = _initializationFields.Retrive<RootMediatorSpotPositionsUpdater>();
            rootMediator.AddListener(mediatorSpotUpdater);

            return grassGroupsPlanter;
        }


        public class VegetationManagementSource
        {
            public VegetationSubjectsInstancingChangeListenerWithFilter InstancingChangeListener;
            public IVegetationSubjectsPositionsProvider PositionsProvider;
        }

        private void StartBushVegetation()
        {
            var singleGenerationArea = _veConfiguration.BushSingleGenerationArea;
            var positionsProvider =
                new GrassVegetationSubjectsPositionsGenerator(
                    new GrassVegetationSubjectsPositionsGenerator.
                        GrassVegetationSubjectsPositionsGeneratorConfiguration()
                        {
                            PositionsGridSize = singleGenerationArea
                        });


            var grassIntensityDbProxy = _initializationFields.Retrive<Grass2IntensityDbProxy>();

            OtherThreadExecutingLocation otherThreadExecutingLocation = new OtherThreadExecutingLocation();
            var planter = CreateBushGroupsPlanter(otherThreadExecutingLocation);

            GrassGroupsGrower grassGroupsGrower = new GrassGroupsGrower(planter, grassIntensityDbProxy);
            Grass2RuntimeManager grass2RuntimeManager = new Grass2RuntimeManager(grassGroupsGrower,
                new Grass2RuntimeManager.Grass2RuntimeManagerConfiguration()
                {
                    GroupSize = singleGenerationArea
                });

            var vegetationSubjectsChangesListener = new Grass2RuntimeManagerProxy(grass2RuntimeManager);
            otherThreadExecutingLocation.SetExecutingTarget(vegetationSubjectsChangesListener);

            //////
            var runtimeManagement = new VegetationRuntimeManagement(
                positionsProvider: positionsProvider,
                vegetationSubjectsChangesListener: vegetationSubjectsChangesListener,
                visibleEntitiesContainer: new VegetationSubjectsVisibleEntitiesContainer(),
                configuration: _veConfiguration.BushVegetationRuntimeManagementConfiguration);

            var vegetationRuntimeManagementProxy = new VegetationRuntimeManagementProxy(runtimeManagement);

            var repositioner = _initializationFields.Retrive<Repositioner>();
            _ultraUpdatableContainer.AddUpdatableElement(new FieldBasedUltraUpdatable()
            {
                StartCameraField = (camera) =>
                {
                    vegetationRuntimeManagementProxy.StartThreading();
                    var newPosition = repositioner.InvMove(camera.transform.localPosition);
                    vegetationRuntimeManagementProxy.Start(newPosition);
                },
                UpdateCameraField = (camera) =>
                {
                    var newPosition = repositioner.InvMove(camera.transform.localPosition);
                    vegetationRuntimeManagementProxy.AddUpdate(newPosition);
                    vegetationRuntimeManagementProxy.SynchronicUpdate(newPosition);
                },
            });
            _ultraUpdatableContainer.AddOtherThreadProxy(vegetationSubjectsChangesListener);
        }

        private GrassGroupsPlanter CreateBushGroupsPlanter(OtherThreadExecutingLocation otherThreadExecutingLocation)
        {
            var meshGenerator = new GrassMeshGenerator();
            var mesh = meshGenerator.GetGrassBillboardMesh(0, 1);

            var instancingMaterial = new Material(Shader.Find("Custom/Vegetation/GrassBushBillboard.Instanced"));
            instancingMaterial.enableInstancing = true;

            /// CLAN

            var billboardsFileManger = new Grass2BillboardClanFilesManager();
            var clan = billboardsFileManger.Load(_veConfiguration.Grass2BillboardsPath, new IntVector2(256, 256));
            var singleToDuo = new Grass2BakingBillboardClanGenerator(
                _initializationFields.Retrive<ComputeShaderContainerGameObject>(),
                _initializationFields.Retrive<UnityThreadComputeShaderExecutorObject>());
            var bakedClan = singleToDuo.GenerateBakedAsync(clan).Result;
            /// 

            var commonUniforms = new UniformsPack();
            commonUniforms.SetUniform("_BendingStrength", 0.6f);
            commonUniforms.SetUniform("_WindDirection", Vector4.one);

            commonUniforms.SetTexture("_DetailTex", bakedClan.DetailTextureArray);
            commonUniforms.SetTexture("_BladeSeedTex", bakedClan.BladeSeedTextureArray);

            var instancingContainer = new GpuInstancingVegetationSubjectContainer(
                new GpuInstancerCommonData(mesh, instancingMaterial, commonUniforms),
                new GpuInstancingUniformsArrayTemplate(new List<GpuInstancingUniformTemplate>()
                {
                    new GpuInstancingUniformTemplate("_Color", GpuInstancingUniformType.Vector4),
                    new GpuInstancingUniformTemplate("_InitialBendingValue", GpuInstancingUniformType.Float),
                    new GpuInstancingUniformTemplate("_PlantBendingStiffness", GpuInstancingUniformType.Float),
                    new GpuInstancingUniformTemplate("_PlantDirection", GpuInstancingUniformType.Vector4),
                    new GpuInstancingUniformTemplate("_RandSeed", GpuInstancingUniformType.Float),
                    new GpuInstancingUniformTemplate("_ArrayTextureIndex", GpuInstancingUniformType.Float),
                })
            );

            var globalGpuInstancingContainer = _initializationFields.Retrive<GlobalGpuInstancingContainer>();
            var bucketId = globalGpuInstancingContainer.CreateBucket(instancingContainer);
            GrassGroupsContainer grassGroupsContainer =
                new GrassGroupsContainer(globalGpuInstancingContainer, bucketId);

            IGrassPositionResolver grassPositionResolver =
                new PoissonDiskSamplerPositionResolver(_veConfiguration.BushExclusionRadiusRange);

            GrassDetailInstancer grassDetailInstancer = new GrassDetailInstancer();

            DesignBodySpotUpdaterProxy designBodySpotUpdaterProxy = _initializationFields.Retrive<DesignBodySpotUpdaterProxy>();

            var mediatorSpotUpdater = new ListenerCenteredMediatorDesignBodyChangesUpdater(designBodySpotUpdaterProxy);
            var grassGroupsPlanter = new GrassGroupsPlanter(
                grassDetailInstancer, grassPositionResolver, grassGroupsContainer, mediatorSpotUpdater,
                new Grass2BushAspectsGenerator(bakedClan), //todo! 
                _veConfiguration.BushTemplatesConfiguration, _initializationFields.Retrive<Repositioner>());

            mediatorSpotUpdater.SetTargetChangesListener(new LambdaSpotPositionChangesListener(null, dict =>
            {
                otherThreadExecutingLocation.Execute(() =>
                {
                    foreach (var pair in dict)
                    {
                        grassGroupsPlanter.GrassGroupSpotChanged(pair.Key, pair.Value);
                    }
                    return TaskUtils.EmptyCompleted();
                });
            }));

            var rootMediator = _initializationFields.Retrive<RootMediatorSpotPositionsUpdater>();
            rootMediator.AddListener(mediatorSpotUpdater);

            return grassGroupsPlanter;
        }
    }
}