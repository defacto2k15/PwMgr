using System;
using System.Collections.Generic;
using System.IO;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.NPRResources.TonalArtMap;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.NPR.TamID
{
    public class TamIdGeneratorDiagramGO : MonoBehaviour
    {
        public String TamName;
        public string TemporaryImagesPath = @"C:\mgr\tmp\";
        public string StrokeImagePath = @"C:\mgr\PwMgrProject\precomputedResources\NPR\Stroke1WithT5.png";
        public string BlankImagePath = @"C:\mgr\PwMgrProject\precomputedResources\NPR\blank.png";
        public void Start()
        {
            var sw = new MyStopWatch();
            sw.StartSegment("TamIdGeneration");
            TaskUtils.SetGlobalMultithreading(false);
            var tonesCount = 3;
            var levelsCount = 3;
            var layersCount = 1;

            var tamTones = TAMTone.CreateList(tonesCount, new Dictionary<int, TAMStrokeOrientation>()
            {
                {0,TAMStrokeOrientation.Horizontal},
                {3,TAMStrokeOrientation.Vertical },
                {5,TAMStrokeOrientation.Both }
            });
            var tamMipmapLevels = TAMMipmapLevel.CreateList(levelsCount);

            var configuration = TamIdPackGenerationConfiguration
                .GetDefaultTamIdConfiguration(tamTones, tamMipmapLevels, 1f, layersCount, StrokeImagePath, BlankImagePath,  false);
            configuration.SmallestLevelSoleImageResolution = new IntVector2(256,256);
            var packGenerator = new TamIdPackGenerator();
            var pack = packGenerator.GenerateTamPack(configuration, false, FindObjectOfType<ComputeShaderContainerGameObject>());
            TamIdGeneratorGO.DrawDebugPlates(pack);
            var fileManager = new TamIdPackFileManager();
            Debug.Log("Sw: "+sw.CollectResults());

            var tamIdPath =TemporaryImagesPath+ TamName + @"\";
            Directory.CreateDirectory(tamIdPath);
            fileManager.Save(tamIdPath, tamTones, tamMipmapLevels, layersCount, pack);
        }
    }
}