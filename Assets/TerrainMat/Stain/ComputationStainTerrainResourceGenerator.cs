using System.Threading.Tasks;

namespace Assets.TerrainMat.Stain
{
    public class ComputationStainTerrainResourceGenerator : IStainTerrainResourceGenerator
    {
        private StainTerrainResourceComposer _stainTerrainResourceComposer;
        private StainTerrainArrayMelder _stainTerrainArrayMelder;
        private ITerrainArrayGenerator _arrayGenerator;

        public ComputationStainTerrainResourceGenerator(StainTerrainResourceComposer stainTerrainResourceComposer,
            StainTerrainArrayMelder stainTerrainArrayMelder, ITerrainArrayGenerator arrayGenerator)
        {
            _stainTerrainResourceComposer = stainTerrainResourceComposer;
            _stainTerrainArrayMelder = stainTerrainArrayMelder;
            _arrayGenerator = arrayGenerator;
        }

        public async Task<StainTerrainResource> GenerateTerrainTextureDataAsync()
        {
            var arrayData = _arrayGenerator.ProvideData();
            var paletteArray = arrayData.PaletteArray;
            var paletteIndexArray = arrayData.PaletteIndexArray;
            var controlArray = arrayData.ControlArray;

            _stainTerrainArrayMelder.AddJoints(paletteArray, paletteIndexArray, controlArray);

            return await _stainTerrainResourceComposer.ComposeAsync(paletteArray, paletteIndexArray, controlArray);
        }
    }
}