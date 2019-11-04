using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Trees.Generation.ETree;
using Assets.Utils;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Trees.Generation
{
    public class TreeFileManager //TODO Refactor, clean maybe delete. TreePrefabManager does the same thing
    {
        private TreeFileManagerConfiguration _configuration;

        public TreeFileManager(TreeFileManagerConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SaveCompleteTreeClan(TreeClanTemplate clanTemplate, string clanName)
        {
#if UNITY_EDITOR
            var samplePyramidTemplate = clanTemplate.TreePyramids[0];
            var newInfo = CreateTreeClanInfoJson(clanTemplate, clanName, samplePyramidTemplate);

            var newFolderGuid = AssetDatabase.CreateFolder(_configuration.WritingTreeCompletedClanDirectory, clanName);
            var familyFolderPath = AssetDatabase.GUIDToAssetPath(newFolderGuid);

            WriteAsJsonInfo(newInfo, familyFolderPath);
            SaveTreeTextures(samplePyramidTemplate.FullDetailTree, familyFolderPath);

            for (int pyramidIndex = 0; pyramidIndex < clanTemplate.TreePyramids.Count; pyramidIndex++)
            {
                var treePyramidTemplate = clanTemplate.TreePyramids[pyramidIndex];
                var trees = new List<Tree> {treePyramidTemplate.FullDetailTree, treePyramidTemplate.SimplifiedTree};
                for (var i = 0; i < trees.Count; i++)
                {
                    var tree = trees[i];
                    var mesh = (Mesh) GameObject.Instantiate(tree.GetComponent<MeshFilter>().sharedMesh);

                    var destinationPath = familyFolderPath + "/tree-" + pyramidIndex + "-" + i + ".asset";
                    AssetDatabase.CreateAsset(mesh, destinationPath);
                }

                SavingFileManager.SaveTextureToPngFile(familyFolderPath + "/billboard-" + pyramidIndex + ".png",
                    treePyramidTemplate.CollageBillboardTexture.Collage);
            }
            AssetDatabase.SaveAssets();
#else
            Preconditions.Fail("Not supported when out of editor");
#endif
        }

        private void SaveTreeTextures(Tree tree, string familyPath)
        {
            var renderer = tree.GetComponent<MeshRenderer>();
            var barkMaterial = renderer.sharedMaterials[0];

            WriteTexture(barkMaterial.GetTexture("_MainTex"), familyPath + "/bark_MainTex.png");
            WriteTexture(barkMaterial.GetTexture("_BumpSpecMap"), familyPath + "/bark_BumpSpecMap.png");
            WriteTexture(barkMaterial.GetTexture("_TranslucencyMap"), familyPath + "/bark_TranslucencyMap.png");

            var leafMaterial = renderer.sharedMaterials[1];
            WriteTexture(leafMaterial.GetTexture("_MainTex"), familyPath + "/leaf_MainTex.png");
            WriteTexture(leafMaterial.GetTexture("_ShadowTex"), familyPath + "/leaf_ShadowTex.png");
            WriteTexture(leafMaterial.GetTexture("_BumpSpecMap"), familyPath + "/leaf_BumpSpecMap.png");
            WriteTexture(leafMaterial.GetTexture("_TranslucencyMap"), familyPath + "/leaf_TranslucencyMap.png");
        }

        private void WriteTexture(Texture texture, string path)
        {
#if UNITY_EDITOR
            var zt = (Texture2D) texture;
            var oldAssetPath = AssetDatabase.GetAssetPath(zt);
            AssetDatabase.CopyAsset(oldAssetPath, path);
#endif
        }

        private static TreeClanInfoJson CreateTreeClanInfoJson(TreeClanTemplate clanTemplate, string clanName,
            TreePyramidTemplate samplePyramidTemplate)
        {
            var newInfo = new TreeClanInfoJson();

            newInfo.ColumnsCount = samplePyramidTemplate.CollageBillboardTexture.ColumnsCount;
            newInfo.LinesCount = samplePyramidTemplate.CollageBillboardTexture.LinesCount;
            newInfo.SubTexturesCount = samplePyramidTemplate.CollageBillboardTexture.SubTexturesCount;
            newInfo.BillboardHeight = samplePyramidTemplate.CollageBillboardTexture.Collage.height;
            newInfo.BillboardWidth = samplePyramidTemplate.CollageBillboardTexture.Collage.width;
            newInfo.ScaleOffsets = clanTemplate.TreePyramids.Select(c => c.CollageBillboardTexture.ScaleOffsets)
                .ToList();

            newInfo.ClanName = clanName;
            newInfo.PyramidsCount = clanTemplate.TreePyramids.Count;
            return newInfo;
        }

        private void WriteAsJsonInfo(TreeClanInfoJson newInfo, string familyFolderPath)
        {
            var serializedToString = JsonUtility.ToJson(newInfo);
            File.WriteAllText(familyFolderPath + "/info.json", serializedToString);
        }

        [Serializable]
        public class TreeClanInfoJson
        {
            public String ClanName;
            public int SubTexturesCount;
            public int LinesCount;
            public int ColumnsCount;
            public int LodTreesCount;
            public int PyramidsCount;
            public int BillboardWidth;
            public int BillboardHeight;
            public List<Vector3> ScaleOffsets;
        }

        public TreeClanEnhanced LoadTreeClan(string clanName)
        {
            var clanFolderPath = _configuration.ReadingTreeCompletedClanDirectory+ "/" + clanName + "/";
            var clanInfo = JsonUtility.FromJson<TreeClanInfoJson>(Resources.Load<TextAsset>(clanFolderPath+"info").text);
            //var infoFilePath = Application.dataPath+"/Resources/"+ clanFolderPath + "info.json";
            //var clanInfo = JsonUtility.FromJson<TreeClanInfoJson>(File.ReadAllText(infoFilePath));

            var pyramidsCount = clanInfo.PyramidsCount;
            var billboardWidth = clanInfo.BillboardWidth;
            var billboardHeight = clanInfo.BillboardHeight;

            var texturesPack = LoadTreeTexturesPack(clanFolderPath);

            var pyramidsList = new List<TreePyramidEnhanced>();
            for (int i = 0; i < pyramidsCount; i++)
            {
                var fullDetailsTreeMesh =
                    (Mesh)Resources.Load(clanFolderPath + "tree-" + i + "-0");
                var simplifiedDetailsTreeMesh =
                    (Mesh)Resources.Load(clanFolderPath + "tree-" + i + "-1");

                var collageTexture = Resources.Load<Texture2D>(
                    clanFolderPath + "billboard-" + i + "");
                var billboardCollageTexture = new BillboardCollageTexture(clanInfo.SubTexturesCount,
                    clanInfo.LinesCount,
                    clanInfo.ColumnsCount, collageTexture, clanInfo.ScaleOffsets[i]);

                var newPyramid = new TreePyramidEnhanced()
                {
                    FullDetailMesh = fullDetailsTreeMesh,
                    SimplifiedMesh = simplifiedDetailsTreeMesh,
                    CollageTexture = billboardCollageTexture
                };
                pyramidsList.Add(newPyramid);
            }
            return new TreeClanEnhanced(pyramidsList, texturesPack);
        }

        private TreeTexturesPack LoadTreeTexturesPack(string familyPath)
        {
            return new TreeTexturesPack()
            {
                BarkMainTex = ReadTexture(familyPath + "bark_MainTex"),
                BarkBumpSpecMap = ReadTexture(familyPath + "bark_BumpSpecMap"),
                BarkTranslucencyMap = ReadTexture(familyPath + "bark_TranslucencyMap"),

                LeafMainTex = ReadTexture(familyPath + "leaf_MainTex"),
                LeafShadowTex = ReadTexture(familyPath + "leaf_ShadowTex"),
                LeafBumpSpecMap = ReadTexture(familyPath + "leaf_BumpSpecMap"),
                LeafTranslucencyMap = ReadTexture(familyPath + "leaf_TranslucencyMap"),
            };
        }

        private Texture ReadTexture(string path)
        {
            return Resources.Load<Texture>(path);
        }

        public TreeClanEnhanced LoadTreeClanFromLivePrefab(string prefabPath)
        {
            var prefab = Resources.Load<Tree>(prefabPath);
            var filter = prefab.GetComponent<MeshFilter>();

            TreeTexturesPack treeTexturesPack;
            var materials = prefab.GetComponent<MeshRenderer>().sharedMaterials;
            if (materials.Length == 2)
            {
                var barkMaterial = materials[0];
                var leafMaterial = materials[1];
                treeTexturesPack = new TreeTexturesPack()
                {
                    BarkMainTex = barkMaterial.GetTexture("_MainTex"),
                    BarkBumpSpecMap = barkMaterial.GetTexture("_BumpSpecMap"),
                    BarkTranslucencyMap = barkMaterial.GetTexture("_TranslucencyMap"),

                    LeafMainTex = leafMaterial.GetTexture("_MainTex"),
                    LeafShadowTex = leafMaterial.GetTexture("_ShadowTex"),
                    LeafBumpSpecMap = leafMaterial.GetTexture("_BumpSpecMap"),
                    LeafTranslucencyMap = leafMaterial.GetTexture("_TranslucencyMap"),
                };
            }
            else
            {
                var leafMaterial = materials[0];
                treeTexturesPack = new TreeTexturesPack()
                {
                    LeafMainTex = leafMaterial.GetTexture("_MainTex"),
                    LeafShadowTex = leafMaterial.GetTexture("_ShadowTex"),
                    LeafBumpSpecMap = leafMaterial.GetTexture("_BumpSpecMap"),
                    LeafTranslucencyMap = leafMaterial.GetTexture("_TranslucencyMap"),
                };
            }

            return new TreeClanEnhanced(new List<TreePyramidEnhanced>()
            {
                new TreePyramidEnhanced()
                {
                    FullDetailMesh = filter.sharedMesh
                }
            }, treeTexturesPack);
        }
    }

    public class TreeFileManagerConfiguration
    {
        public string WritingTreeCompletedClanDirectory = "Assets/Resources/treePrefabs/completedGenerated2";
        public string ReadingTreeCompletedClanDirectory = "treePrefabs/completedGenerated2";
    }


    public class TreeClanEnhanced
    {
        private List<TreePyramidEnhanced> _pyramids;
        private TreeTexturesPack _treeTexturesPack;

        public TreeClanEnhanced(List<TreePyramidEnhanced> pyramids, TreeTexturesPack treeTexturesPack)
        {
            _pyramids = pyramids;
            _treeTexturesPack = treeTexturesPack;
        }

        public List<TreePyramidEnhanced> Pyramids
        {
            get { return _pyramids; }
        }

        public TreeTexturesPack TreeTexturesPack => _treeTexturesPack;
        public bool HasSimplifiedVersion => _pyramids.First().SimplifiedMesh != null;
        public bool HasBillboard => _pyramids.First().CollageTexture != null;
    }

    public class TreePyramidEnhanced
    {
        public Mesh FullDetailMesh;
        public Mesh SimplifiedMesh;
        public BillboardCollageTexture CollageTexture;
    }

    public class ETreePyramidEnhanced
    {
        public Mesh FullDetailMesh;
        public Mesh SimplifiedMesh;
        public EBillboardTextureArray BillboardTextureArray;
    }

    public class TreeTexturesPack
    {
        public Texture BarkMainTex;
        public Texture BarkBumpSpecMap;
        public Texture BarkTranslucencyMap;

        public Texture LeafMainTex;
        public Texture LeafShadowTex;
        public Texture LeafBumpSpecMap;
        public Texture LeafTranslucencyMap;
    }
}