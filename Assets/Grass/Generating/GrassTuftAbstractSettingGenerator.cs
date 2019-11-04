using Assets.Grass.Generating;
using Assets.ShaderUtils;
using Assets.Utils;

namespace Assets.Grass
{
    internal class GrassTuftAbstractSettingGenerator : AbstractSettingGenerator
    {
        public override void SetSettings(GrassEntitiesSet aGrass)
        {
            var tuftHue = RandomGrassGenerator.GetHue();
            var tuftValue = RandomGrassGenerator.GetValue();
            var randomSaturation = RandomGrassGenerator.GetSaturation(); //todo use it
            MyRange basePlantBendingStiffness = RandomTuftGenerator.GetBasePlantBendingStiffness();
            MyRange basePlantBendingValue = RandomTuftGenerator.GetBasePlantBendingValue();
            ForeachEntity(aGrass,
                c =>
                    c.AddUniform(ShaderUniformName._PlantBendingStiffness,
                        RandomTuftGenerator.GetPlantBendingStiffness(basePlantBendingStiffness)));
            ForeachEntity(aGrass,
                c =>
                    c.AddUniform(ShaderUniformName._InitialBendingValue,
                        RandomTuftGenerator.GetPlantBendingValue(basePlantBendingValue)));
            ForeachEntity(aGrass,
                c => c.AddUniform(ShaderUniformName._Color, RandomGrassGenerator.GetGrassColor(tuftHue)));
            ForeachEntity(aGrass, c => c.AddUniform(ShaderUniformName._RandSeed, UnityEngine.Random.value));
        }
    }
}