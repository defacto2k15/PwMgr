using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.MeshGeneration;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass
{
    class GrassTuftGenerator : IEntityGenerator
    {
        public GrassEntitiesSet Generate()
        {
            var elementsRange = RandomTuftGenerator.GetTuftElementsRange();
            int elementsCount = RandomTuftGenerator.GetTuftCount(elementsRange);
            var anglesList = RandomTuftGenerator.GetRandomAngles(elementsCount, elementsRange.Max);
            float radiusFromCenter = 0.1f;
            List<GrassEntity> entities = new List<GrassEntity>();

            foreach (var angle in anglesList)
            {
                float radiousRandomOffset = RandomTuftGenerator.GetPositionOffset();
                var grassEntity = new GrassEntity
                {
                    Position =
                        new Vector3((radiusFromCenter + radiousRandomOffset) * (float) Math.Sin(angle), 0,
                            (radiusFromCenter + radiousRandomOffset) * (float) Math.Cos(angle + 90)),
                    Rotation = new Vector3(0, angle, 0),
                    Scale = RandomGrassGenerator.GetScale(),
                };
                entities.Add(grassEntity);
            }

            return new GrassEntitiesSet(entities);
        }
    }
}