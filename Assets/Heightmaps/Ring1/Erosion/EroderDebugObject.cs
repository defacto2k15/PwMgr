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
    public class EroderDebugObject : MonoBehaviour
    {
        public Texture2D SourceTexture;

        public void Start231()
        {
            string bilFilePath = @"C:\inz\cont\n49_e019_1arc_v3.bil";
            HeightmapFile heightmapFile = new HeightmapFile();
            const int filePixelWidth = 3601;
            heightmapFile.LoadFile(bilFilePath, 3601);
            heightmapFile.MirrorReflectHeightDataInXAxis();
            var heightArray = heightmapFile.GlobalHeightArray;

            var smallArray = HeightmapUtils.CutSubArray(heightArray, new IntArea(100, 100, 240, 240));

            SavingFileManager.SaveTextureToPngFile(@"C:\inz\cont\smallCut.png",
                HeightmapUtils.CreateTextureFromHeightmap(smallArray));
        }

        public void StartY()
        {
            var heightTexture = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\smallCut.png", 240,
                240, TextureFormat.RGBA32, true, true);
            _currentHeightmap = HeightmapUtils.CreateHeightmapArrayFromTexture(heightTexture);
            var array = _currentHeightmap.HeightmapAsArray;
            MyArrayUtils.Multiply(array, 65000);
            var extremes = MyArrayUtils.CalculateExtremes(array);
            Debug.Log("Max: " + extremes.Max + " min: " + extremes.Min);
        }

        public void Start()
        {
            //var heightTexture = SavingFileManager.LoadPngTextureFromFile(@"C:\inz\cont\smallCut.png", 240,
            //    240, TextureFormat.RGBA32, true, true);
            //_currentHeightmap = HeightmapUtils.CreateHeightmapArrayFromTexture(heightTexture);

            DiamondSquareCreator creator = new DiamondSquareCreator(new RandomProvider(22));
            _currentHeightmap = creator.CreateDiamondSquareNoiseArray(new IntVector2(240, 240), 64);
            MyArrayUtils.Multiply(_currentHeightmap.HeightmapAsArray, 0.1f);

            _go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var material = new Material(Shader.Find("Custom/Terrain/Terrain_Debug"));
            _go.GetComponent<MeshRenderer>().material = material;
            _go.name = "Terrain";
            _go.transform.localRotation = Quaternion.Euler(0, 0, 0);
            _go.transform.localScale = new Vector3(10, 10, 10);
            _go.transform.localPosition = new Vector3(0, 0, 0);
            _go.GetComponent<MeshFilter>().mesh = PlaneGenerator.CreateFlatPlaneMesh(240, 240);

            var doneTexture =
                HeightmapUtils.CreateTextureFromHeightmap(_currentHeightmap);
            _go.GetComponent<MeshRenderer>().material.SetTexture("_HeightmapTex0", doneTexture);
            _go.GetComponent<MeshRenderer>().material.SetTexture("_HeightmapTex1", doneTexture);
            _oldDoneTexture = doneTexture;
        }

        private GameObject _go;
        private Texture2D _oldDoneTexture;
        private HeightmapArray _currentHeightmap;

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                ErodeOneStep();
            }
        }

        public void ErodeOneStep()
        {
            var currentHeightArray = SimpleHeightArray.FromHeightmap(_currentHeightmap);
            var eroder = new ThermalEroder();

            var extremes = new ArrayExtremes(1517, 6161);
            var tParam = CalculateFromHillFactor(1, extremes, 24);
            Debug.Log("TPAN IS " + tParam);

            eroder.Erode(currentHeightArray, new ThermalErosionConfiguration()
            {
                StepCount = 5,
                CParam = 0.3f,
                TParam = tParam
            });

            var doneTexture =
                HeightmapUtils.CreateTextureFromHeightmap(
                    SimpleHeightArray.ToHeightmap(currentHeightArray));
            //_go.GetComponent<MeshRenderer>().material.SetTexture("_HeightmapTex0", _oldDoneTexture);
            _go.GetComponent<MeshRenderer>().material.SetTexture("_HeightmapTex1", doneTexture);
            _oldDoneTexture = doneTexture;
        }

        // hill factor. 0 - flat. 1 - equal
        public static float CalculateFromHillFactor(float hillFactor, ArrayExtremes extremes, float metersPerUnit)
        {
            var oneUnitTooBigDelta = metersPerUnit * hillFactor;
            var oneUnitTooBigDeltaInNormalizedSpace = oneUnitTooBigDelta / extremes.Delta;
            return oneUnitTooBigDeltaInNormalizedSpace;
        }
    }
}