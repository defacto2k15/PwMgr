using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Grass2.GrassIntensityMap;
using Assets.Grass2.IntensitySampling;
using Assets.Grass2.Planting;
using Assets.Grass2.Types;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Random.Fields;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Grass2.Growing
{
    public class GrassGroupsGrowerDebugObject : MonoBehaviour
    {
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        private DebugGrassGroupsGrowerUnderTest _growerUnderTest =
            new DebugGrassGroupsGrowerUnderTest(new DebugGrassPlanterUnderTest());

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);

            _growerUnderTest.Start(ComputeShaderContainer);
            var grower = _growerUnderTest.Grower;
            var bandId = grower.GrowGrassBandAsync(new MyRectangle(0, 0, 90, 90)).Result;
            _growerUnderTest.FinalizeStart();
        }

        public static IGrassIntensityMapProvider CreateDebugIntensityMapProvider()
        {
            var grass1Intensity = new IntensityFieldFigure(16, 16);
            var grass2Intensity = new IntensityFieldFigure(16, 16);
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    grass1Intensity.SetPixel(x, y, Mathf.InverseLerp(4, 10, x));
                    grass2Intensity.SetPixel(x, y, Mathf.InverseLerp(0, 4, y));
                }
            }

            return new DebugGrassIntensityMapProvider(new List<GrassTypeWithUvedIntensity>()
            {
                new GrassTypeWithUvedIntensity()
                {
                    Figure = new IntensityFieldFigureWithUv()
                    {
                        FieldFigure = grass1Intensity,
                        Uv = new MyRectangle(0, 0, 1, 1)
                    },
                    Type = GrassType.Debug1,
                },
                new GrassTypeWithUvedIntensity()
                {
                    Figure = new IntensityFieldFigureWithUv()
                    {
                        FieldFigure = grass2Intensity,
                        Uv = new MyRectangle(0, 0, 1, 1)
                    },
                    Type = GrassType.Debug2,
                },
            });
        }

        public void Update()
        {
            _growerUnderTest.Update();
        }
    }

    public class DebugGrassGroupsGrowerUnderTest
    {
        private IDebugPlanterUnderTest _planterUnderTest;
        private GrassGroupsGrower _grower;

        public DebugGrassGroupsGrowerUnderTest(IDebugPlanterUnderTest planterUnderTest)
        {
            _planterUnderTest = planterUnderTest;
        }

        public GrassGroupsGrower Grower => _grower;

        public void Start(ComputeShaderContainerGameObject ComputeShaderContainer)
        {
            IGrassIntensityMapProvider grassIntensityMapProvider =
                GrassGroupsGrowerDebugObject.CreateDebugIntensityMapProvider();
            _planterUnderTest.Start(ComputeShaderContainer);

            GrassGroupsPlanter grassGroupsPlanter = _planterUnderTest.GrassGroupsPlanter;
            _grower = new GrassGroupsGrower(grassGroupsPlanter, grassIntensityMapProvider);
        }

        public void FinalizeStart()
        {
            _planterUnderTest.FinalizeStart();
        }

        public void Update()
        {
            _planterUnderTest.Update();
        }
    }
}