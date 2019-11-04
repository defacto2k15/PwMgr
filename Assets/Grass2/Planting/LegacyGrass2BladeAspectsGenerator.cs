using System.Collections.Generic;
using Assets.Grass2.Billboards;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass2.Planting
{
    public class LegacyGrass2BladeAspectsGenerator : IGrass2AspectsGenerator
    {
        public Grass2Aspect GenerateAspect(UnplantedGrassInstance unplantedInstance, Vector2 flatPosition)
        {
            var rotationInRad = unplantedInstance.FlatRotationInRadians;
            MyTransformTriplet transformTriplet = new MyTransformTriplet(
                new Vector3(0, 0, 0),
                new Vector3(0, rotationInRad, 0), 
                unplantedInstance.Size);

            UniformsPack uniforms = new UniformsPack();
            uniforms.SetUniform("_InitialBendingValue", unplantedInstance.InitialBendingValue);
            uniforms.SetUniform("_PlantBendingStiffness", unplantedInstance.InitialBendingStiffness);
            uniforms.SetUniform("_PlantDirection",
                new Vector4(Mathf.Sin(rotationInRad), Mathf.Cos(rotationInRad), 0, 0)); //todo!
            uniforms.SetUniform("_Color", unplantedInstance.Color);

            return new Grass2Aspect()
            {
                Entities = new List<Grass2Entity>()
                {
                    new Grass2Entity()
                    {
                        DeltaTransformTriplet = transformTriplet,
                        Uniforms = uniforms,
                        FlatRotation = rotationInRad
                    }
                },
                FlatPos = flatPosition
            };
        }
    }

    public class Grass2BushAspectsGenerator : IGrass2AspectsGenerator
    {
        private Grass2BakedBillboardClan _billboardClan;

        public Grass2BushAspectsGenerator(Grass2BakedBillboardClan billboardClan)
        {
            _billboardClan = billboardClan;
        }


        public Grass2Aspect GenerateAspect(UnplantedGrassInstance unplantedInstance, Vector2 flatPosition)
        {
            var entityRotationInRad = unplantedInstance.FlatRotationInRadians;
            var usedBillboardIndex = _billboardClan.QueryRandom((int) (entityRotationInRad * 543.21)); //todo seed

            var entities = new List<Grass2Entity>();
            int billboardsCount = 3;
            for (int i = 0; i < billboardsCount; i++)
            {
                var additionalRotation = ((Mathf.PI) / (billboardsCount)) * i;
                var finalRotationInRad = additionalRotation + entityRotationInRad;

                MyTransformTriplet transformTriplet = new MyTransformTriplet(
                    new Vector3(0, 0, 0),
                    new Vector3(0, finalRotationInRad, 0),
                    unplantedInstance.Size);

                UniformsPack uniforms = new UniformsPack();
                uniforms.SetUniform("_InitialBendingValue", unplantedInstance.InitialBendingValue);
                uniforms.SetUniform("_PlantBendingStiffness", unplantedInstance.InitialBendingStiffness);
                uniforms.SetUniform("_PlantDirection",
                    new Vector4(Mathf.Sin(finalRotationInRad), Mathf.Cos(finalRotationInRad), finalRotationInRad, 0)); //todo!
                uniforms.SetUniform("_Color", unplantedInstance.Color);
                uniforms.SetUniform("_ArrayTextureIndex", usedBillboardIndex);


                var newEntity = new Grass2Entity()
                {
                    DeltaTransformTriplet = transformTriplet,
                    Uniforms = uniforms,
                    FlatRotation = finalRotationInRad
                };

                entities.Add(newEntity);
            }

            return new Grass2Aspect()
            {
                Entities = entities,
                FlatPos = flatPosition
            };
        }
    }
}