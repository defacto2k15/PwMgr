using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.Creator;
using Assets.MeshGeneration;
using Assets.Random;
using Assets.Utils;
using Assets.Utils.ArrayUtils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.Erosion
{
    public class ErosionComparisionDebugObject : MonoBehaviour
    {
        private GameObject _go;

        public void Start()
        {
            //Start_Hydraulic_Debuggable();
            Start_Mai_Debug();
        }

        public void Start_Hydraulic()
        {
            var heightTexture1 =
                SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\temp3.png", 240, 240, TextureFormat.RGBA32, true,
                    true);
            var heightmap1 = HeightmapUtils.CreateHeightmapArrayFromTexture(heightTexture1);

            //DiamondSquareCreator creator = new DiamondSquareCreator(new RandomProvider(22));
            //var heightmap2 = creator.CreateDiamondSquareNoiseArray(new IntVector2(240, 240), 64);
            //MyArrayUtils.Multiply(heightmap2.HeightmapAsArray, 0.1f);

            var tParam = EroderDebugObject.CalculateFromHillFactor(1, new ArrayExtremes(0, 5000), 24);

            var extents = MyArrayUtils.CalculateExtremes(heightmap1.HeightmapAsArray);
            MyArrayUtils.Normalize(heightmap1.HeightmapAsArray);

            var erodedArrays = GenerateHydraulicErodedArrays(heightmap1, new List<HydraulicEroderConfiguration>()
            {
                new HydraulicEroderConfiguration()
                {
                    StepCount = 40,
                    NeighbourFinder = NeighbourFinders.Big9Finder,
                    kr_ConstantWaterAddition = 0.001f,
                    ks_GroundToSedimentFactor = 1f,
                    ke_WaterEvaporationFactor = 0.05f,
                    kc_MaxSedimentationFactor = 0.8f,
                    FinalSedimentationToGround = true,
                    WaterGenerator = HydraulicEroderWaterGenerator.AllFrames,
                    DestinationFinder = HydraulicEroderWaterDestinationFinder.OnlyBest
                },
            });

            CreateTestObject(heightmap1, erodedArrays);
        }

        public void Start_Hydraulic_Debuggable()
        {
            var heightTexture1 = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\smallCut.png", 240,
                240, TextureFormat.RGBA32, true, true);
            var heightmap1 = HeightmapUtils.CreateHeightmapArrayFromTexture(heightTexture1);

            //DiamondSquareCreator creator = new DiamondSquareCreator(new RandomProvider(22));
            //var heightmap2 = creator.CreateDiamondSquareNoiseArray(new IntVector2(240, 240), 64);
            //MyArrayUtils.Multiply(heightmap2.HeightmapAsArray, 0.1f);

            var tParam = EroderDebugObject.CalculateFromHillFactor(1, new ArrayExtremes(0, 5000), 24);

            var extents = MyArrayUtils.CalculateExtremes(heightmap1.HeightmapAsArray);
            MyArrayUtils.Normalize(heightmap1.HeightmapAsArray);

            var configuration =
                new HydraulicEroderConfiguration()
                {
                    StepCount = 20,
                    NeighbourFinder = NeighbourFinders.Big9Finder,
                    kr_ConstantWaterAddition = 0.001f,
                    ks_GroundToSedimentFactor = 1f,
                    ke_WaterEvaporationFactor = 0.05f,
                    kc_MaxSedimentationFactor = 0.8f,
                    FinalSedimentationToGround = false,
                    WaterGenerator = HydraulicEroderWaterGenerator.FirstFrame,
                    DestinationFinder = HydraulicEroderWaterDestinationFinder.OnlyBest
                };

            var copyArray = MyArrayUtils.DeepClone(heightmap1.HeightmapAsArray);
            var currentHeightArray = new SimpleHeightArray(copyArray);
            var eroder = new DebuggableHydraulicEroder();
            var debuggingOutput = eroder.Erode(currentHeightArray, configuration, 1);

            var sedimentExtentsArr = debuggingOutput.SedimentSnapshots
                .Select(c => MyArrayUtils.CalculateExtremes(c.Array)).ToList();
            var sedimentExtents = new ArrayExtremes(sedimentExtentsArr.Min(c => c.Min),
                sedimentExtentsArr.Max(c => c.Max));
            debuggingOutput.SedimentSnapshots.ForEach(c => MyArrayUtils.Normalize(c.Array, sedimentExtents));

            var waterExtentsArr = debuggingOutput.WaterSnapshots.Select(c => MyArrayUtils.CalculateExtremes(c.Array))
                .ToList();
            var waterExtents = new ArrayExtremes(waterExtentsArr.Min(c => c.Min), waterExtentsArr.Max(c => c.Max));
            debuggingOutput.WaterSnapshots.ForEach(c => MyArrayUtils.Normalize(c.Array, waterExtents));


            _go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("Custom/Terrain/Terrain_Debug_Comparision_StepByStep"));
            _go.GetComponent<MeshRenderer>().material = material;
            _go.name = "Terrain";
            _go.transform.localRotation = Quaternion.Euler(0, 0, 0);
            _go.transform.localScale = new Vector3(10, 1, 10);
            _go.transform.localPosition = new Vector3(0, 0, 0);
            _go.GetComponent<MeshFilter>().mesh = PlaneGenerator.CreateFlatPlaneMesh(240, 240);

            MyHeightTextureArray heightTextureArray =
                new MyHeightTextureArray(240, 240, 2, TextureFormat.ARGB32, false, true);
            heightTextureArray.AddElementArray(heightmap1, 0);
            heightTextureArray.AddElementArray(new HeightmapArray(currentHeightArray.Array), 1);
            _go.GetComponent<MeshRenderer>().material
                .SetTexture("_HeightmapTexArray", heightTextureArray.ApplyAndRetrive());


            MyHeightTextureArray waterArray = new MyHeightTextureArray(240, 240, debuggingOutput.WaterSnapshots.Count,
                TextureFormat.ARGB32, false, true);
            int i = 0;
            foreach (var waterSnapshot in debuggingOutput.WaterSnapshots)
            {
                waterArray.AddElementArray(new HeightmapArray(waterSnapshot.Array), i);
                i++;
            }
            _go.GetComponent<MeshRenderer>().material.SetTexture("_WaterArray", waterArray.ApplyAndRetrive());


            MyHeightTextureArray sedimentArray = new MyHeightTextureArray(240, 240,
                debuggingOutput.SedimentSnapshots.Count, TextureFormat.ARGB32, false, true);
            int j = 0;
            foreach (var sedimentSnapshot in debuggingOutput.SedimentSnapshots)
            {
                sedimentArray.AddElementArray(new HeightmapArray(sedimentSnapshot.Array), j);
                j++;
            }
            _go.GetComponent<MeshRenderer>().material.SetTexture("_SedimentArray", sedimentArray.ApplyAndRetrive());
        }


        public void Start_Thermal()
        {
            var heightTexture1 = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\smallCut.png", 240,
                240, TextureFormat.RGBA32, true, true);
            var heightmap1 = HeightmapUtils.CreateHeightmapArrayFromTexture(heightTexture1);

            DiamondSquareCreator creator = new DiamondSquareCreator(new RandomProvider(22));
            var heightmap2 = creator.CreateDiamondSquareNoiseArray(new IntVector2(240, 240), 64);
            MyArrayUtils.Multiply(heightmap2.HeightmapAsArray, 0.1f);

            var tParam = EroderDebugObject.CalculateFromHillFactor(1, new ArrayExtremes(0, 5000), 24);
            var erodedArrays = GenerateErodedArrays(heightmap1, new List<ThermalErosionConfiguration>()
            {
                new ThermalErosionConfiguration()
                {
                    CParam = 0.5f,
                    StepCount = 10,
                    TParam = tParam * 0.6f,
                    NeighbourFinder = NeighbourFinders.Big9Finder,
                    GroundMover = ThermalErosionGroundMovers.OnlyBestMover
                },
                new ThermalErosionConfiguration()
                {
                    CParam = 0.5f,
                    StepCount = 10,
                    TParam = tParam * 0.6f,
                    NeighbourFinder = NeighbourFinders.Big9Finder,
                    GroundMover = ThermalErosionGroundMovers.AllNeighboursMover
                },
                new ThermalErosionConfiguration()
                {
                    CParam = 0.5f,
                    StepCount = 10,
                    TParam = tParam * 0.6f,
                    NeighbourFinder = NeighbourFinders.Big9Finder,
                    GroundMover = ThermalErosionGroundMovers.OnlyBestMoverTweaked,
                    NeighboursChooser = ThermalEroderNeighboursChoosers.LesserEqualThanTChooser
                },
            });
            CreateTestObject(heightmap1, erodedArrays);
        }

        public List<HeightmapArray> GenerateHydraulicErodedArrays(HeightmapArray baseArray,
            List<HydraulicEroderConfiguration> configurations)
        {
            var msw = new MyStopWatch();
            List<HeightmapArray> outArray = new List<HeightmapArray>();
            int i = 0;
            foreach (var aConfiguration in configurations)
            {
                var copyArray = MyArrayUtils.DeepClone(baseArray.HeightmapAsArray);
                var currentHeightArray = new SimpleHeightArray(copyArray);
                msw.StartSegment("eroding-" + i);
                var eroder = new HydraulicEroder();
                eroder.Erode(currentHeightArray, aConfiguration);
                msw.StopSegment();

                outArray.Add(SimpleHeightArray.ToHeightmap(currentHeightArray));
                i++;
            }
            Debug.Log("T22: " + msw.CollectResults());
            return outArray;
        }

        public List<HeightmapArray> GenerateErodedArrays(HeightmapArray baseArray,
            List<ThermalErosionConfiguration> configurations)
        {
            var msw = new MyStopWatch();
            List<HeightmapArray> outArray = new List<HeightmapArray>();
            int i = 0;
            foreach (var aConfiguration in configurations)
            {
                var copyArray = MyArrayUtils.DeepClone(baseArray.HeightmapAsArray);
                var currentHeightArray = new SimpleHeightArray(copyArray);
                msw.StartSegment("eroding-" + i);
                var eroder = new ThermalEroder();
                //var eroder = new TweakedThermalEroder();
                eroder.Erode(currentHeightArray, aConfiguration);
                msw.StopSegment();

                outArray.Add(SimpleHeightArray.ToHeightmap(currentHeightArray));
                i++;
            }
            Debug.Log("T22: " + msw.CollectResults());
            return outArray;
        }

        public List<HeightmapArray> GenerateErodedArrays(HeightmapArray baseArray,
            List<MeiHydraulicEroderConfiguration> configurations)
        {
            var msw = new MyStopWatch();
            List<HeightmapArray> outArray = new List<HeightmapArray>();
            int i = 0;
            foreach (var aConfiguration in configurations)
            {
                var copyArray = MyArrayUtils.DeepClone(baseArray.HeightmapAsArray);
                var currentHeightArray = new SimpleHeightArray(copyArray);
                msw.StartSegment("eroding-" + i);
                var eroder = new MeiHydraulicEroder();
                eroder.Erode(currentHeightArray, aConfiguration);
                msw.StopSegment();

                outArray.Add(SimpleHeightArray.ToHeightmap(currentHeightArray));
                i++;
            }
            Debug.Log("T22: " + msw.CollectResults());
            return outArray;
        }


        private void CreateTestObject(HeightmapArray baseArray, List<HeightmapArray> otherArrays)
        {
            _go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("Custom/Terrain/Terrain_Debug_Comparision"));
            _go.GetComponent<MeshRenderer>().material = material;
            _go.name = "Terrain";
            _go.transform.localRotation = Quaternion.Euler(0, 0, 0);
            _go.transform.localScale = new Vector3(10, 1, 10);
            _go.transform.localPosition = new Vector3(0, 0, 0);
            _go.GetComponent<MeshFilter>().mesh = PlaneGenerator.CreateFlatPlaneMesh(240, 240);

            MyHeightTextureArray heightTextureArray =
                new MyHeightTextureArray(240, 240, otherArrays.Count + 1, TextureFormat.ARGB32, false, true);
            heightTextureArray.AddElementArray(baseArray, 0);
            for (int i = 0; i < otherArrays.Count; i++)
            {
                heightTextureArray.AddElementArray(otherArrays[i], i + 1);
            }

            _go.GetComponent<MeshRenderer>().material
                .SetTexture("_HeightmapTexArray", heightTextureArray.ApplyAndRetrive());
        }


        public void Start_Mai()
        {
            var heightTexture1 = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\smallCut.png", 240,
                240, TextureFormat.RGBA32, true, true);
            var heightmap1 = HeightmapUtils.CreateHeightmapArrayFromTexture(heightTexture1);

            //DiamondSquareCreator creator = new DiamondSquareCreator(new RandomProvider(22));
            //var heightmap2 = creator.CreateDiamondSquareNoiseArray(new IntVector2(240, 240), 64);
            //MyArrayUtils.Multiply(heightmap2.HeightmapAsArray, 0.1f);
            HeightmapArray workingHeightmap = LoadHeightmapFromTextureFile(@"C:\inz\cont\temp3.png");

            MyArrayUtils.Normalize(workingHeightmap.HeightmapAsArray);
            MyArrayUtils.InvertNormalized(workingHeightmap.HeightmapAsArray);

            var generatedArrays = GenerateErodedArrays(workingHeightmap, new List<MeiHydraulicEroderConfiguration>()
            {
                new MeiHydraulicEroderConfiguration()
                {
                    StepCount = 10,
                    A_PipeCrossSection = 0.00005f,
                    ConstantWaterAdding = 1 / 16f,
                    GravityAcceleration = 9.81f,
                    DeltaT = 1 / 60f,
                    DepositionConstant = 0.0001f * 12 * 10f,
                    DissolvingConstant = 0.0001f * 12 * 10f,
                    EvaporationConstant = 0.00011f * 0.5f * 10,
                    GridSize = new Vector2(1, 1),
                    L_PipeLength = 1,
                    SedimentCapacityConstant = 25000
                },
            });
            CreateTestObject(workingHeightmap, generatedArrays);
        }

        public HeightmapArray LoadHeightmapFromTextureFile(string pathToTemplateFile)
        {
            var tex = SavingFileManager.LoadPngTextureFromFile(pathToTemplateFile, 240, 240,
                TextureFormat.ARGB32, false,
                false);
            return HeightmapUtils.CreateHeightmapArrayFromTexture(tex);
        }


        public void Start_Mai_Debug()
        {
            var heightTexture1 = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\smallCut.png", 240,
                240, TextureFormat.RGBA32, true, true);
            var heightmap1 = HeightmapUtils.CreateHeightmapArrayFromTexture(heightTexture1);

            //DiamondSquareCreator creator = new DiamondSquareCreator(new RandomProvider(22));
            //var heightmap2 = creator.CreateDiamondSquareNoiseArray(new IntVector2(240, 240), 64);
            //MyArrayUtils.Multiply(heightmap2.HeightmapAsArray, 0.1f);
            //HeightmapArray workingHeightmap = LoadHeightmapFromTextureFile(@"C:\inz\cont\temp3.png"); 
            HeightmapArray workingHeightmap = heightmap1;

            MyArrayUtils.Normalize(workingHeightmap.HeightmapAsArray);
            MyArrayUtils.InvertNormalized(workingHeightmap.HeightmapAsArray);
            HeightmapArray originalMap = new HeightmapArray(MyArrayUtils.DeepClone(workingHeightmap.HeightmapAsArray));
            MyArrayUtils.Multiply(workingHeightmap.HeightmapAsArray, 400);

            var configuration = new MeiHydraulicEroderConfiguration()
            {
                StepCount = 50,
                A_PipeCrossSection = 0.05f,
                ConstantWaterAdding = 1 / 64f,
                GravityAcceleration = 9.81f,
                DeltaT = 1f,
                DepositionConstant = 0.0001f * 12 * 2f,
                DissolvingConstant = 0.0001f * 12 * 2f,
                EvaporationConstant = 0.05f * 10,
                GridSize = new Vector2(1, 1),
                L_PipeLength = 1,
                SedimentCapacityConstant = 250
            };

            var eroder = new MeiHydraulicEroder();
            var debOutput = eroder.ErodeWithDebug(SimpleHeightArray.FromHeightmap(workingHeightmap), configuration);
            MyArrayUtils.Multiply(workingHeightmap.HeightmapAsArray, 1f / 400);
            debOutput.NormalizeInGroups();


            _go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("Custom/Terrain/Terrain_Mei_Debug_Comparision_StepByStep"));
            _go.GetComponent<MeshRenderer>().material = material;
            _go.name = "Terrain";
            _go.transform.localRotation = Quaternion.Euler(0, 0, 0);
            _go.transform.localScale = new Vector3(10, 1, 10);
            _go.transform.localPosition = new Vector3(0, 0, 0);
            _go.GetComponent<MeshFilter>().mesh = PlaneGenerator.CreateFlatPlaneMesh(240, 240);

            MyHeightTextureArray heightTextureArray =
                new MyHeightTextureArray(240, 240, 2, TextureFormat.ARGB32, false, true);
            heightTextureArray.AddElementArray(originalMap, 0);
            heightTextureArray.AddElementArray(workingHeightmap, 1);
            _go.GetComponent<MeshRenderer>().material
                .SetTexture("_HeightmapTexArray", heightTextureArray.ApplyAndRetrive());


            var arrayListsCount = debOutput.OneArrayListCount;
            var arrayListsLength = debOutput.OneArrayListLength;
            MyHeightTextureArray detailHeightTexturesArray =
                new MyHeightTextureArray(240, 240, arrayListsCount * arrayListsLength, TextureFormat.ARGB32, false,
                    true);

            foreach (var snapshot in debOutput.ArraysDict.Values.SelectMany(c => c))
            {
                detailHeightTexturesArray.AddElementArray(snapshot);
            }
            _go.GetComponent<MeshRenderer>().material
                .SetTexture("_DetailTexArray", detailHeightTexturesArray.ApplyAndRetrive());
            _go.GetComponent<MeshRenderer>().material.SetFloat("_DetailTexLength", arrayListsLength);
        }
    }
}