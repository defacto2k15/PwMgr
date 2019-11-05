using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Random;
using Assets.Trees.Generation.ETree;
using Assets.Utils;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Trees.Generation
{
    public class TreePrefabManager
    {
        private static string TREE_HANDMADE_PREFABS_DIRECTORY = "Assets/treePrefabs/handmade";
        private static string TREE_GENERATED_PREFABS_DIRECTORY = "Assets/treePrefabs/generated";
        private static string TREE_COMPLETED_GENERATED_PREFABS_DIRECTORY = "Assets/treePrefabs/completedGenerated";

        public void ClearGeneratedFiles()
        {
            ClearDirectory(TREE_GENERATED_PREFABS_DIRECTORY);
        }

        public void ClearCompletedGeneratedFiles()
        {
            ClearDirectory(TREE_COMPLETED_GENERATED_PREFABS_DIRECTORY);
        }

#if UNITY_EDITOR
        private void ClearDirectory(string path)
        {
            var assetsToDelete =
                AssetDatabase.FindAssets("*", new[] {path})
                    .Select(c => AssetDatabase.GUIDToAssetPath(c)).ToList();
            foreach (var asset in assetsToDelete)
            {
                AssetDatabase.DeleteAsset(asset);
            }
        }
#else

        private void ClearDirectory(string path)
        {
            Preconditions.Fail("Not supported in build");
        }
#endif

#if UNITY_EDITOR
        public TreeFileFamily CreateTreeFileFamily(Tree handmadeTree, string familyName, int elementsToCreate)
        {
            return CreateTreeFileFamilyWithPath(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(handmadeTree)),
                familyName, elementsToCreate);
        }
#else

        public TreeFileFamily CreateTreeFileFamily(Tree handmadeTree, string familyName, int elementsToCreate)
        {
            Preconditions.Fail("Not supported in build");
            return null;
        }
#endif

        public TreeFileFamily CreateTreeFileFamily(string handmadeName, string familyName, int elementsToCreate)
        {
            return CreateTreeFileFamilyWithPath(GetHandmadeTreeName(handmadeName), familyName, elementsToCreate);
        }

#if UNITY_EDITOR
        private TreeFileFamily CreateTreeFileFamilyWithPath(string sourcePath, string familyName, int elementsToCreate)
        {
            List<Tree> createdTreesList = new List<Tree>();

            for (var i = 0; i < elementsToCreate; i++)
            {
                var destinationPath = TREE_GENERATED_PREFABS_DIRECTORY + "/" + familyName + "-" + i + ".prefab";
                var copyResult = AssetDatabase.CopyAsset(sourcePath, destinationPath);
                Preconditions.Assert(copyResult,
                    "Copying file from " + sourcePath + " to " + destinationPath + " failed!");

                var copiedTree = AssetDatabase.LoadAssetAtPath<Tree>(destinationPath);
                Preconditions.Assert(copiedTree != null, "Loading tree from " + destinationPath + " failed");
                createdTreesList.Add(copiedTree);
            }

            return new TreeFileFamily(createdTreesList, familyName);
        }
#else


        private TreeFileFamily CreateTreeFileFamilyWithPath(string sourcePath, string familyName, int elementsToCreate)
        {
            Preconditions.Fail("Not supported in build");
            return null;
        }
#endif

        private static string GetHandmadeTreeName(string handmadeName)
        {
            var sourcePath = TREE_HANDMADE_PREFABS_DIRECTORY + "/" + handmadeName + ".prefab";
            return sourcePath;
        }

#if UNITY_EDITOR
        public Tree LoadHandmadeTreePrefab(string treeName)
        {
            var path = GetHandmadeTreeName(treeName);
            var tree = AssetDatabase.LoadAssetAtPath<Tree>(path);
            Preconditions.Assert(tree != null, "Loading tree from " + path + " failed");
            return tree;
        }
#else

        public Tree LoadHandmadeTreePrefab(string treeName)
        {
            Preconditions.Fail("Not supported in build");
            return null;
        }
#endif

#if UNITY_EDITOR
        public void SaveCompleteTreeClan(TreeClanTemplate clanTemplate, string clanName)
        {
            var newInfo = new TreeClanInfoJson();

            var samplePyramidTemplate = clanTemplate.TreePyramids[0];
            newInfo.ColumnsCount = samplePyramidTemplate.CollageBillboardTexture.ColumnsCount;
            newInfo.LinesCount = samplePyramidTemplate.CollageBillboardTexture.LinesCount;
            newInfo.SubTexturesCount = samplePyramidTemplate.CollageBillboardTexture.SubTexturesCount;
            newInfo.BillboardHeight = samplePyramidTemplate.CollageBillboardTexture.Collage.height;
            newInfo.BillboardWidth = samplePyramidTemplate.CollageBillboardTexture.Collage.width;
            newInfo.ScaleOffsets = clanTemplate.TreePyramids.Select(c => c.CollageBillboardTexture.ScaleOffsets)
                .ToList();

            newInfo.ClanName = clanName;
            newInfo.PyramidsCount = clanTemplate.TreePyramids.Count;

            var newFolderGuid = AssetDatabase.CreateFolder(TREE_COMPLETED_GENERATED_PREFABS_DIRECTORY, clanName);
            var familyFolderPath = AssetDatabase.GUIDToAssetPath(newFolderGuid);

            WriteAsJsonInfo(newInfo, familyFolderPath);

            for (int pyramidIndex = 0; pyramidIndex < clanTemplate.TreePyramids.Count; pyramidIndex++)
            {
                var treePyramidTemplate = clanTemplate.TreePyramids[pyramidIndex];
                var trees = new List<Tree> {treePyramidTemplate.FullDetailTree, treePyramidTemplate.SimplifiedTree};
                for (var i = 0; i < trees.Count; i++)
                {
                    var sourcePath = GetPrefabPath(trees[i]);
                    var destinationPath = familyFolderPath + "/tree-" + pyramidIndex + "-" + i + ".prefab";
                    var copyResult = AssetDatabase.CopyAsset(sourcePath, destinationPath);
                    Preconditions.Assert(copyResult,
                        "Copying file from " + sourcePath + " to " + destinationPath + " failed!");
                }

                SavingFileManager.SaveTextureToPngFile(familyFolderPath + "/billboard-" + pyramidIndex + ".png",
                    treePyramidTemplate.CollageBillboardTexture.Collage);
            }
        }
#else

        public void SaveCompleteTreeClan(TreeClanTemplate clanTemplate, string clanName)
        {
            Preconditions.Fail("Not supported in build");
        }
#endif

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

#if UNITY_EDITOR
        public List<Tree> CreateTreeCopies(Tree baseTree, int copiesCount)
        {
            List<Tree> createdTreesList = new List<Tree>();

            var sourcePath = GetPrefabPath(baseTree);
            string randomPrefix = "RAND-" + RandomUtils.RandomString();
            for (var i = 0; i < copiesCount; i++)
            {
                var destinationPath = TREE_GENERATED_PREFABS_DIRECTORY + "/" + randomPrefix + "-" + i + ".prefab";
                var copyResult = AssetDatabase.CopyAsset(sourcePath, destinationPath);
                Preconditions.Assert(copyResult,
                    "Copying file from " + sourcePath + " to " + destinationPath + " failed!");

                var copiedTree = AssetDatabase.LoadAssetAtPath<Tree>(destinationPath);
                Preconditions.Assert(copiedTree != null, "Loading tree from " + destinationPath + " failed");
                createdTreesList.Add(copiedTree);
            }

            return createdTreesList;
        }
#else

        public List<Tree> CreateTreeCopies(Tree baseTree, int copiesCount)
        {
            Preconditions.Fail("Not supported in build");
            return null;
        }
#endif

#if UNITY_EDITOR
        private string GetPrefabPath(Tree baseTree)
        {
            var prefabParentPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(baseTree));
            if (!string.IsNullOrEmpty(prefabParentPath))
            {
                return prefabParentPath;
            }

            var prefabObjectPath = AssetDatabase.GetAssetPath(PrefabUtility.GetPrefabObject(baseTree));
            if (!string.IsNullOrEmpty(prefabObjectPath))
            {
                return prefabObjectPath;
            }

            var objectPath = AssetDatabase.GetAssetPath(baseTree);
            if (!string.IsNullOrEmpty(objectPath))
            {
                return objectPath;
            }

            Preconditions.Fail("Cannot get path of " + baseTree);
            return null;
        }

        private string GetMeshPath(Mesh baseMesh)
        {
            var prefabParentPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(baseMesh));
            if (!string.IsNullOrEmpty(prefabParentPath))
            {
                return prefabParentPath;
            }

            var prefabObjectPath = AssetDatabase.GetAssetPath(PrefabUtility.GetPrefabObject(baseMesh));
            if (!string.IsNullOrEmpty(prefabObjectPath))
            {
                return prefabObjectPath;
            }

            var objectPath = AssetDatabase.GetAssetPath(baseMesh);
            if (!string.IsNullOrEmpty(objectPath))
            {
                return objectPath;
            }

            Preconditions.Fail("Cannot get path of " + baseMesh);
            return null;
        }
#else

        private string GetPrefabPath(Tree baseTree)
        {
            Preconditions.Fail("Not supported in build");
            return null;
        }
#endif

#if UNITY_EDITOR
        public TreeClan LoadTreeClan(string clanName)
        {
            var clanFolderPath = TREE_COMPLETED_GENERATED_PREFABS_DIRECTORY + "/" + clanName + "/";
            var infoFilePath = clanFolderPath + "info.json";
            var clanInfo = JsonUtility.FromJson<TreeClanInfoJson>(File.ReadAllText(infoFilePath));

            var pyramidsCount = clanInfo.PyramidsCount;
            var billboardWidth = clanInfo.BillboardWidth;
            var billboardHeight = clanInfo.BillboardHeight;

            var pyramidsList = new List<TreePyramid>();
            for (int i = 0; i < pyramidsCount; i++)
            {
                var fullDetailsTreePath = clanFolderPath + "tree-" + i + "-0.prefab";
                var fullDetailsTree = AssetDatabase.LoadAssetAtPath<Tree>(fullDetailsTreePath);

                var simplifiedTreePath = clanFolderPath + "tree-" + i + "-1.prefab";
                var simplifiedTree = AssetDatabase.LoadAssetAtPath<Tree>(simplifiedTreePath);

                var collageTexture = SavingFileManager.LoadPngTextureFromFile(
                    clanFolderPath + "billboard-" + i + ".png",
                    billboardWidth, billboardHeight, TextureFormat.RGBA32, true, true);
                var billboardCollageTexture = new BillboardCollageTexture(clanInfo.SubTexturesCount,
                    clanInfo.LinesCount,
                    clanInfo.ColumnsCount, collageTexture, clanInfo.ScaleOffsets[i]);

                var newPyramid = new TreePyramid(fullDetailsTree, simplifiedTree, billboardCollageTexture);
                pyramidsList.Add(newPyramid);
            }

            return new TreeClan(pyramidsList);
        }
#else

        public TreeClan LoadTreeClan(string clanName)
        {
            Preconditions.Fail("Not supported in build");
            return null;
        }
#endif
        public void ESaveCompleteTreeClan(ETreeClanTemplate clanTemplate, string clanName)
        {
#if UNITY_EDITOR
            var so = ScriptableObject.CreateInstance<ETreeClanScriptableObject>();
            so.Pyramids = clanTemplate.TreePyramids.Select(c => new ESerializableTreePyramidTemplate()
            {
                BillboardArray = c.BillboardTextureArray.Array,
                SimplifiedTreeMesh = c.SimplifiedTreeMesh,
                FullTreeMesh = c.FullTreeMesh,
                ScaleOffsets = c.BillboardTextureArray.ScaleOffsets
            }).ToList();

            var i = 0;
            foreach (var billboard in clanTemplate.TreePyramids.Select(c => c.BillboardTextureArray))
            {
                AssetDatabase.CreateAsset(billboard.Array, TREE_COMPLETED_GENERATED_PREFABS_DIRECTORY + $"/treeClan-{clanName}.billboard-{i}.asset");
                i++;
            }

            AssetDatabase.CreateAsset(so, TREE_COMPLETED_GENERATED_PREFABS_DIRECTORY + $"/treeClan-{clanName}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#else
            Preconditions.Fail("ESaveCompleteTreeClan is not supported outside of editor");
#endif
        }


        public ETreeClanTemplate ELoadCompleteTreeClan(string clanName)
        {
#if UNITY_EDITOR
            var path = TREE_COMPLETED_GENERATED_PREFABS_DIRECTORY + $"/treeClan-{clanName}.asset";
            var so = AssetDatabase.LoadAssetAtPath<ETreeClanScriptableObject>(path);
            Preconditions.Assert(so!=null, "Cannot find ETreeClan SO at path "+path);
            return new ETreeClanTemplate(so.Pyramids.Select(c =>
                new ETreePyramidTemplate(billboardTextureArray: new EBillboardTextureArray(c.BillboardArray, c.ScaleOffsets), fullTreeMesh: c.FullTreeMesh,
                    simplifiedTreeMesh: c.SimplifiedTreeMesh)).ToList());
#else
            Preconditions.Fail("ELoadCompleteTreeClan is not supported outside of editor");
            return null;
#endif
        }

        public ETreeClanTemplate ELoadTreeClanFromLivePrefab(GameObject prefab)
        {
            Mesh mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
            Preconditions.Assert(mesh != null, $"Cannot take mesh from prefab of name {prefab.name}");
            return new ETreeClanTemplate(new List<ETreePyramidTemplate>()
            {
                new ETreePyramidTemplate(null, mesh, null)
            });
        }
    }

    [System.Serializable]
    public class ESerializableTreePyramidTemplate
    {
        public Mesh FullTreeMesh;
        public Mesh SimplifiedTreeMesh;
        public Texture2DArray BillboardArray;
        public Vector3 ScaleOffsets;
    }
}