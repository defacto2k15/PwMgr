using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.NPRResources.TonalArtMap;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.NPR.Filling.TamID
{
    public class TamIdDebug2OC : MonoBehaviour
    {
        public Texture2DArray TamIdArray;
        private RunOnceBox _once;
        public string StrokeImagePath = @"C:\mgr\PwMgrProject\precomputedResources\NPR\Stroke1WithT5.png" ;
        public string BlankImagePath = @"C:\mgr\PwMgrProject\precomputedResources\NPR\blank.png";

        public void Start()
        {
            _once = new RunOnceBox(() =>
            {

            var sw = new MyStopWatch();
            sw.StartSegment("TamIdGeneration");
            TaskUtils.SetGlobalMultithreading(false);
            var tonesCount = 2;
            var levelsCount = 4;
            var layersCount = 2;

            var tamTones = TAMTone.CreateList(tonesCount, new Dictionary<int, TAMStrokeOrientation>()
            {
                {0,TAMStrokeOrientation.Horizontal},
                {3,TAMStrokeOrientation.Vertical },
                {5,TAMStrokeOrientation.Both }
            });
            var tamMipmapLevels = TAMMipmapLevel.CreateList(levelsCount);

            var configuration = TamIdPackGenerationConfiguration
                .GetDefaultTamIdConfiguration(tamTones, tamMipmapLevels, 1f, layersCount, StrokeImagePath, BlankImagePath);
            var packGenerator = new TamIdPackGenerator();
            var pack = packGenerator.GenerateTamPack(configuration, false, FindObjectOfType<ComputeShaderContainerGameObject>());
            var fileManager = new TamIdPackFileManager();
            Debug.Log("Sw: "+sw.CollectResults());

            fileManager.Save(@"C:\mgr\tmp\tamid1\", tamTones, tamMipmapLevels, layersCount, pack);
            var generator = new TamIdArrayGenerator();
            var tex2DArray = generator.Generate(pack, tamTones, tamMipmapLevels, layersCount);
                TamIdArray = tex2DArray;


                var tamIdPostProcessingDirectorOc = FindObjectOfType<TamIdPostProcessingDirectorOC>();
                var mat = GetComponent<MeshRenderer>().material;
                TamIdArray.filterMode = FilterMode.Point;
                mat.SetTexture("_TamIdTexArray", TamIdArray);

                mat.SetBuffer("_AppendBuffer", tamIdPostProcessingDirectorOc.TamidFragmentBuffer);
                mat.SetInt("_FragmentTexWidth", tamIdPostProcessingDirectorOc.FragmentTexWidth);
            });
        }

        public void Update()
        {
            _once.Update();
        }
    }
}
