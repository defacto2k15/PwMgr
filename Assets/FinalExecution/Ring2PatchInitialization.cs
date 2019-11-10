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
using Assets.Utils.Textures;
using Assets.Utils.UTUpdating;

namespace Assets.FinalExecution
{
    public class Ring2PatchInitialization
    {
        private GameInitializationFields _gameInitializationFields;
        private UltraUpdatableContainer _ultraUpdatableContainer;
        private  FeRing2PatchConfiguration _ring2Configuration;

        public Ring2PatchInitialization(GameInitializationFields gameInitializationFields, UltraUpdatableContainer ultraUpdatableContainer, FeRing2PatchConfiguration ring2Configuration)
        {
            _gameInitializationFields = gameInitializationFields;
            _ultraUpdatableContainer = ultraUpdatableContainer;
            _ring2Configuration = ring2Configuration;
        }

        public void Start()
        {
            var ring2ShaderRepository = Ring2PlateShaderRepository.Create();

            var conciever = _gameInitializationFields.Retrive<TextureConcieverUTProxy>();
            var detailEnhancer =
                new Ring2IntensityPatternEnhancer(_gameInitializationFields.Retrive<UTTextureRendererProxy>(),
                    _ring2Configuration.Ring2IntensityPatternEnhancingSizeMultiplier);

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
                _ring2Configuration.Ring2PatchesOverseerConfiguration
            );

            var gRing2PatchesCreatorProxy = new GRing2PatchesCreatorProxy(patchesCreator);
            _ultraUpdatableContainer.AddOtherThreadProxy(gRing2PatchesCreatorProxy);

            _gameInitializationFields.SetField(gRing2PatchesCreatorProxy);

            UTRing2PlateStamperProxy stamperProxy = new UTRing2PlateStamperProxy(
                new Ring2PlateStamper(_ring2Configuration.Ring2PlateStamperConfiguration,
                    _gameInitializationFields.Retrive<ComputeShaderContainerGameObject>()));
            _ultraUpdatableContainer.Add(stamperProxy);

            Ring2PatchStamplingOverseerFinalizer patchStamper = new Ring2PatchStamplingOverseerFinalizer(
                stamperProxy,
                _gameInitializationFields.Retrive<UTTextureRendererProxy>());
            _gameInitializationFields.SetField(patchStamper);
        }
    }
}