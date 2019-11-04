using System;
using Assets.ShaderUtils;

namespace Assets.Grass.Generating
{
    abstract class AbstractSettingGenerator
    {
        public abstract void SetSettings(GrassEntitiesSet aGrass);

        protected void ForeachEntity(GrassEntitiesSet aGrass, Action<GrassEntity> method)
        {
            aGrass.EntitiesBeforeTransform.ForEach(method);
        }
    }

    internal class GrassSingleAbstractSettingGenerator : AbstractSettingGenerator
    {
        public override void SetSettings(GrassEntitiesSet aGrass)
        {
            float plantBendingSiffness = RandomGrassGenerator.GetBendingStiffness();
            float initialBendingValue = RandomGrassGenerator.GetInitialBendingValue();
            ForeachEntity(aGrass,
                grassEntity => grassEntity.AddUniform(ShaderUniformName._PlantBendingStiffness, plantBendingSiffness));
            ForeachEntity(aGrass,
                grassEntity => grassEntity.AddUniform(ShaderUniformName._InitialBendingValue, initialBendingValue));
            ForeachEntity(aGrass,
                grassEntity => grassEntity.AddUniform(ShaderUniformName._Color, RandomGrassGenerator.GetGrassColor()));
            ForeachEntity(aGrass,
                grassEntity => grassEntity.AddUniform(ShaderUniformName._RandSeed, UnityEngine.Random.value));
        }
    }
}