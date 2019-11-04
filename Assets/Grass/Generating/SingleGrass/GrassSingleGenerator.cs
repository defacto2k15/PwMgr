using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Grass.Generating
{
    class GrassSingleGenerator : IEntityGenerator
    {
        public GrassEntitiesSet Generate()
        {
            var angle = RandomGrassGenerator.GetAngle();
            float plantBendingSiffness = RandomGrassGenerator.GetBendingStiffness();
            float initialBendingValue = RandomGrassGenerator.GetInitialBendingValue();

            var grassEntity = new GrassEntity
            {
                Position = new Vector3(0, 0, 0),
                Rotation = new Vector3(0, angle, 0),
                Scale = RandomGrassGenerator.GetScale(),
            };
            //grassEntity.AddUniform(GrassShaderUniformName._PlantBendingStiffness,  plantBendingSiffness);
            //grassEntity.AddUniform(GrassShaderUniformName._InitialBendingValue,  initialBendingValue);
            //grassEntity.AddUniform(GrassShaderUniformName._Color,  RandomGrassGenerator.GetGrassColor() );
            //grassEntity.AddUniform(GrassShaderUniformName._RandSeed, UnityEngine.Random.value );
            return new GrassEntitiesSet(new List<GrassEntity> {grassEntity});
        }
    }
}