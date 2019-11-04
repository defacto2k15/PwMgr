using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.MeshGeneration;
using Assets.ShaderUtils;
using Assets.Utils;
using UnityEngine;

namespace Assets.Grass.Billboard
{
    class GrassBillboardGenerator
    {
        public GrassEntitiesSet GenerateStarTuft(int elementsInTuftCount = 3)
        {
            var entities = new List<GrassEntity>();
            for (int i = 0; i < elementsInTuftCount; i++)
            {
                var obj = new GrassEntity
                {
                    Rotation = MyMathUtils.DegToRad(new Vector3(0, 0, ((float) i) / elementsInTuftCount * 180))
                };
                obj.AddUniform(ShaderUniformName._MinUv, 0);
                obj.AddUniform(ShaderUniformName._MaxUv, 1);
                obj.AddUniform(ShaderUniformName._RandSeed, UnityEngine.Random.value);
                entities.Add(obj);
            }
            return new GrassEntitiesSet(entities);
        }

        public GrassEntitiesSet GenerateTriangleTurf()
        {
            var scale = 0.75f;
            //var obj1 = generateOnePlantSegment(material, 0, scale, 0,0, 0, 0.33f);
            //var obj2 = generateOnePlantSegment(material, 0, scale, 0.75f, 0, 0.33f, 0.66f);
            //var obj3 = generateOnePlantSegment(material, 0, scale, 1.5f, 0, 0.66f, 1); 

            // instead of 0.5 there was 0.433, but this is better

            var randSeed1 = UnityEngine.Random.value;
            var obj1 = GenerateOnePlantSegment(0, scale, 0, -2.5f * 0.125f, 0.125f, 1f - 0.125f, randSeed1);
            var obj11 = GenerateOnePlantSegment(0, 0.125f, -(3.5f) * 0.125f, -2.5f * 0.125f, 0f, 0.125f, randSeed1);
            var obj12 = GenerateOnePlantSegment(0, 0.125f, 3.5f * 0.125f, -2.5f * 0.125f, 1f - 0.125f, 1, randSeed1);

            var randSeed2 = UnityEngine.Random.value;
            var obj2 = GenerateOnePlantSegment(120, scale, -1.5f * 0.125f, 0, 0.125f, 1f - 0.125f, randSeed2);
            var obj21 = GenerateOnePlantSegment(120, 0.125f, -(3.25f) * 0.125f, (-2.5f - 0.5f) * 0.125f, 1f - 0.125f, 1,
                randSeed2);
            var obj22 = GenerateOnePlantSegment(120, 0.125f, (0.25f) * 0.125f, (2.5f + 0.5f) * 0.125f, 0f, 0.125f,
                randSeed2);

            var randSeed3 = UnityEngine.Random.value;
            var obj3 = GenerateOnePlantSegment(60, scale, 1.5f * 0.125f, 0, 0.125f, 1f - 0.125f, randSeed3);
            var obj31 = GenerateOnePlantSegment(60, 0.125f, (3.25f) * 0.125f, (-2.5f - 0.5f) * 0.125f, 1f - 0.125f, 1,
                randSeed3);
            var obj32 = GenerateOnePlantSegment(60, 0.125f, -(0.25f) * 0.125f, (2.5f + 0.5f) * 0.125f, 0f, 0.125f,
                randSeed3);

            return new GrassEntitiesSet(new List<GrassEntity>()
            {
                obj1,
                obj11,
                obj12,
                obj2,
                obj21,
                obj22,
                obj3,
                obj31,
                obj32
            });
        }

        private GrassEntity GenerateOnePlantSegment(float yEulerAngle, float xScale, float xPos,
            float zPos, float minUv, float maxUv, float randSeed)
        {
            var obj = new GrassEntity
            {
                Rotation = MyMathUtils.DegToRad(new Vector3(0, yEulerAngle, 0)),
                Scale = new Vector3(xScale, 1, 1),
                Position = new Vector3(xPos, 0, zPos)
            };
            obj.AddUniform(ShaderUniformName._MinUv, minUv);
            obj.AddUniform(ShaderUniformName._MaxUv, maxUv);
            obj.AddUniform(ShaderUniformName._RandSeed, randSeed);
            return obj;
        }
    }
}