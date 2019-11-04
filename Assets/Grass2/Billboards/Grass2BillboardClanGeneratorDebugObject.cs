using Assets.Heightmaps.Ring1.RenderingTex;
using Assets.Heightmaps.Ring1.TerrainDescription.FeatureGenerating;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.TextureRendering;
using UnityEngine;

namespace Assets.Grass2.Billboards
{
    public class Grass2BillboardClanGeneratorDebugObject : MonoBehaviour
    {
        public GameObject TextureShowingObject;
        public ComputeShaderContainerGameObject ComputeShaderContainer;

        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(false);
            var generator = new Grass2BillboardGenerator(new UTTextureRendererProxy(new TextureRendererService(
                new MultistepTextureRenderer(ComputeShaderContainer), new TextureRendererServiceConfiguration()
                {
                    StepSize = new Vector2(10, 10)
                })), new Grass2BillboardGenerator.Grass2BillboardGeneratorConfiguration()
            {
                BillboardSize = new IntVector2(256, 256)
            });

            //var tex = generator.GenerateBillboardImageAsync(50, 12.3f).Result;
            //TextureShowingObject.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", tex);

            var clansGenerator = new Grass2BillboardClanGenerator(generator);
            var clan = clansGenerator.Generate();

            var fileManager = new Grass2BillboardClanFilesManager();
            fileManager.Save(@"C:\inz\billboards\", clan);
        }
    }
}