using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Trees.Generation.ETree
{
    public class ETreeClanGenerator
    {
        private BillboardGenerator _billboardGenerator;
        private Material _treeMaterial;
        private ETreeClanGeneratorConfiguration _configuration;

        public ETreeClanGenerator(BillboardGenerator billboardGenerator, Material treeMaterial, ETreeClanGeneratorConfiguration configuration)
        {
            _billboardGenerator = billboardGenerator;
            _treeMaterial = treeMaterial;
            _configuration = configuration;
        }

        public void CreateClanPyramid(List<Mesh> meshes, Action<ETreeClanTemplate> finishedCallback)
        {
            var pyramidTemplateList = new List<ETreePyramidTemplate>();

            var generatedPyramidsCount = 0;
            foreach (var aMesh in meshes)
            {
                var instancedTreeObject = CreateTreeGameObject(aMesh);
                instancedTreeObject.gameObject.transform.localPosition = new Vector3(0, 0, 0);
                instancedTreeObject.SetActive(false);
                _billboardGenerator.AddOrder(new BillboardTemplateGenerationOrder(
                    _configuration.BillboardWidth, _configuration.BillboardsCount, instancedTreeObject.gameObject, (generationResult) =>
                    {
                        var collageBillboardTexture = CreateBillboardTextureArray(generationResult);

                        pyramidTemplateList.Add(new ETreePyramidTemplate(collageBillboardTexture, aMesh,aMesh));

                        GameObject.Destroy(instancedTreeObject);
                        generatedPyramidsCount++;
                        if (generatedPyramidsCount == meshes.Count)
                        {
                            finishedCallback(new ETreeClanTemplate(pyramidTemplateList));
                        }
                    }, _configuration.BillboardMarginMultiplier));
            }
        }

        private GameObject CreateTreeGameObject(Mesh treeMesh)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.GetComponent<MeshFilter>().mesh = treeMesh;
            go.GetComponent<MeshRenderer>().material = _treeMaterial;
            return go;
        }

        private EBillboardTextureArray CreateBillboardTextureArray(BillboardTemplateGeneratorResult generatorResult)
        {
            var listOfTextures = generatorResult.GeneratedTextures;
            int width = listOfTextures[0].width;
            int height = listOfTextures[0].height;

            var array = new Texture2DArray(width, height, listOfTextures.Count, TextureFormat.RGBA32, false);
            for (var texIndex = 0; texIndex < listOfTextures.Count; texIndex++)
            {
                array.SetPixels32(listOfTextures[texIndex].GetPixels32(), texIndex);
            }
            array.Apply(false);
            return new EBillboardTextureArray(array, generatorResult.ScaleOffsets);
        }
    }

    public class ETreeClanGeneratorConfiguration
    {
        public int BillboardWidth;
        public int BillboardsCount;
        public float BillboardMarginMultiplier;
    }

    public class EBillboardTextureArray
    {
        private Texture2DArray _array;
        private readonly Vector3 _scaleOffsets;

        public EBillboardTextureArray( Texture2DArray array, Vector3 scaleOffsets)
        {
            _array = array;
            _scaleOffsets = scaleOffsets;
        }

        public Texture2DArray Array => _array;

        public Vector3 ScaleOffsets => _scaleOffsets;
    }
}
