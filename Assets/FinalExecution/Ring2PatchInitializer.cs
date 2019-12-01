using System.Collections.Generic;
using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Ring2;
using Assets.Ring2.Db;
using Assets.Ring2.Devising;
using Assets.Ring2.GRuntimeManagementOtherThread;
using Assets.Ring2.Painting;
using Assets.Ring2.PatchTemplateToPatch;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Ring2.RuntimeManagementOtherThread.Finalizer;
using Assets.Ring2.Stamping;
using Assets.Utils.Services;
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;

namespace Assets.FinalExecution
{
    public class Ring2PatchInitializer
    {
        private GameInitializationFields _gameInitializationFields;
        private UltraUpdatableContainer _ultraUpdatableContainer;
        private Ring2PatchInitializerConfiguration _ring2InitializerConfiguration;

        public Ring2PatchInitializer(GameInitializationFields gameInitializationFields, UltraUpdatableContainer ultraUpdatableContainer, Ring2PatchInitializerConfiguration ring2InitializerConfiguration)
        {
            _gameInitializationFields = gameInitializationFields;
            _ultraUpdatableContainer = ultraUpdatableContainer;
            _ring2InitializerConfiguration = ring2InitializerConfiguration;
        }

        public void Start()
        {
            var ring2ShaderRepository = Ring2PlateShaderRepository.Create();

            var conciever = _gameInitializationFields.Retrive<TextureConcieverUTProxy>();
            var detailEnhancer =
                new Ring2IntensityPatternEnhancer(_gameInitializationFields.Retrive<UTTextureRendererProxy>(),
                    _ring2InitializerConfiguration.Ring2IntensityPatternEnhancingSizeMultiplier);

            var ring2PatchesPainterUtProxy = new Ring2PatchesPainterUTProxy(new Ring2PatchesPainter(
                new Ring2MultishaderMaterialRepository(ring2ShaderRepository, Ring2ShaderNames.ShaderNames)));
            _ultraUpdatableContainer.Add(ring2PatchesPainterUtProxy);

            var patchesCreator = new GRing2PatchesCreator(
                _gameInitializationFields.Retrive<IRing2RegionsDatabase>(),
                new GRing2RegionsToPatchTemplateConventer(),
                new Ring2PatchTemplateCombiner(),
                new Ring2PatchCreator(),
                new Ring2IntensityPatternProvider(conciever, detailEnhancer),
                new GRing2Deviser(),
                _ring2InitializerConfiguration.Ring2PatchesOverseerConfiguration
            );

            var gRing2PatchesCreatorProxy = new GRing2PatchesCreatorProxy(patchesCreator);
            _ultraUpdatableContainer.AddOtherThreadProxy(gRing2PatchesCreatorProxy);

            _gameInitializationFields.SetField(gRing2PatchesCreatorProxy);

            UTRing2PlateStamperProxy stamperProxy = new UTRing2PlateStamperProxy(
                new Ring2PlateStamper(_ring2InitializerConfiguration.Ring2PlateStamperConfiguration,
                    _gameInitializationFields.Retrive<ComputeShaderContainerGameObject>()));
            _ultraUpdatableContainer.Add(stamperProxy);

            Ring2PatchStamplingOverseerFinalizer patchStamper = new Ring2PatchStamplingOverseerFinalizer(
                stamperProxy,
                _gameInitializationFields.Retrive<UTTextureRendererProxy>(), _gameInitializationFields.Retrive<CommonExecutorUTProxy>());
            _gameInitializationFields.SetField(patchStamper);
        }
    }

    public class Ring2PatchInitializerConfiguration // TODO embedd this in Main configuration
    {
        private FEConfiguration _feConfiguration;

        public Ring2PatchInitializerConfiguration(FEConfiguration feConfiguration)
        {
            _feConfiguration = feConfiguration;
        }

        public int Ring2IntensityPatternEnhancingSizeMultiplier => 12;

        public Ring2PlateStamperConfiguration Ring2PlateStamperConfiguration = new Ring2PlateStamperConfiguration()
        {
            PlateStampPixelsPerUnit = new Dictionary<int, float>()
            {
                [0] = 3f,
                [1] = 3 / 8f,
                [2] = 3 / 64f
            }
        };

        public Dictionary<int, float> Ring2PatchesOverseerConfiguration_IntensityPatternPixelsPerUnit = new Dictionary<int, float>() //TODO it is ugly
            {
                [0] = 1 / 3f,
                [1] = 1 / (3 * 8f),
                [2] = 1 / (3f * 64f)
            };

        public Ring2PatchesOverseerConfiguration Ring2PatchesOverseerConfiguration =>
            new Ring2PatchesOverseerConfiguration()
            {
                IntensityPatternPixelsPerUnit = Ring2PatchesOverseerConfiguration_IntensityPatternPixelsPerUnit,
                PatchSize = _feConfiguration.Ring2PatchSize
            };

        public int MipmapLevelToExtract = 1;
    }
}