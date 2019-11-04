using System.Collections.Generic;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass2.Planting
{
    public class EVegetationGrass2BladeAspectsGenerator : IGrass2AspectsGenerator
    {
        public Grass2Aspect GenerateAspect(UnplantedGrassInstance unplantedInstance, Vector2 flatPosition)
        {
            var rotationInRad = unplantedInstance.FlatRotationInRadians;
            MyTransformTriplet transformTriplet = new MyTransformTriplet(
                new Vector3(0, 0, 0),
                new Vector3(Mathf.Deg2Rad * unplantedInstance.InitialBendingValue*20, rotationInRad, 0), //TODO i use properties from Grass2 as inputs to EVegetation
                unplantedInstance.Size);

            UniformsPack uniforms = new UniformsPack();
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
}