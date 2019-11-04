using System.Collections.Generic;
using Assets.Grass2.Types;
using Assets.Random;
using UnityEngine;

namespace Assets.Grass2.Planting
{
    public class GrassDetailInstancer
    {
        public List<UnplantedGrassInstance> Initialize(int instancesCount, GrassTypeTemplate template)
        {
            var randomProvider = new RandomProvider(652); //todo!
            var outList = new List<UnplantedGrassInstance>();
            for (int i = 0; i < instancesCount; i++)
            {
                var size = new Vector3((float) randomProvider.RandomGaussian(template.FlatSize.A),
                    1,
                    (float) randomProvider.RandomGaussian(template.FlatSize.B));
                //size = new Vector3(1,1,1);

                var color = new Color(
                    (float) randomProvider.RandomGaussian(template.Color.A),
                    (float) randomProvider.RandomGaussian(template.Color.B),
                    (float) randomProvider.RandomGaussian(template.Color.C)
                );

                var initialBendingValue = (float) randomProvider.RandomGaussian(template.InitialBendingValue);
                var initialBendingStiffness = (float) randomProvider.RandomGaussian(template.InitialBendingValue);

                var flatRotationInRadians = randomProvider.Next(0, 2 * Mathf.PI);

                outList.Add(new UnplantedGrassInstance()
                {
                    Color = color,
                    Size = size,
                    InitialBendingValue = initialBendingValue,
                    InitialBendingStiffness = initialBendingStiffness,
                    FlatRotationInRadians = flatRotationInRadians
                });
            }

            return outList;
        }
    }
}