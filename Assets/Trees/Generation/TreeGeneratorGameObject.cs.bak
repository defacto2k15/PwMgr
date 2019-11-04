using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Trees.Generation
{
    public class TreeGeneratorGameObject : MonoBehaviour
    {
#if UNITY_EDITOR
        public BillboardGenerator BillboardGeneratorObject;
        public Tree TreeInstance;
        public string ClanName;

        public void Start()
        {
            var mainGenerator = new MainTreeGenerator(BillboardGeneratorObject);
            mainGenerator.Generate(TreeInstance, ClanName, 1.0f);
        }

        public void StartA()
        {
            var treeFileManager = new TreeFileManager(new TreeFileManagerConfiguration());

            var clan = treeFileManager.LoadTreeClan("cypres1Small");
            var texPack = clan.TreeTexturesPack;
            var pyramid1 = clan.Pyramids[0];

            var barkInstancingMaterial = new Material(Shader.Find("Custom/Nature/Tree Creator Bark Optimized"));
            barkInstancingMaterial.SetTexture("_MainTex", texPack.BarkMainTex);
            barkInstancingMaterial.SetTexture("_BumpSpecMap", texPack.BarkBumpSpecMap);
            barkInstancingMaterial.SetTexture("_TranslucencyMap", texPack.BarkTranslucencyMap);

            var leafInstancingMaterial = new Material(Shader.Find("Custom/Nature/Tree Creator Leaves Optimized Ugly"));
            leafInstancingMaterial.SetTexture("_MainTex", texPack.LeafMainTex);
            leafInstancingMaterial.SetTexture("_ShadowTex", texPack.LeafShadowTex);
            leafInstancingMaterial.SetTexture("_BumpSpecMap", texPack.LeafBumpSpecMap);
            leafInstancingMaterial.SetTexture("_TranslucencyMap", texPack.LeafTranslucencyMap);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.GetComponent<MeshFilter>().sharedMesh = pyramid1.FullDetailMesh;
            go.GetComponent<MeshRenderer>().sharedMaterials = new Material[]
            {
                barkInstancingMaterial,
                leafInstancingMaterial
            };
        }
#endif
    }
}