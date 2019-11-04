﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.Creator;
using Assets.Random;
using Assets.Utils;
using UnityEngine;

#if UNITY_EDITOR
using TreeEditor;
#endif

using Debug = UnityEngine.Debug;

namespace Assets.Trees.Generation
{
#if  UNITY_EDITOR
    public class TreeClanGenerator
    {
        private BillboardGenerator _billboardGenerator;
        private TreePrefabManager _prefabManager;

        public TreeClanGenerator(BillboardGenerator billboardGenerator, TreePrefabManager prefabManager)
        {
            _billboardGenerator = billboardGenerator;
            _prefabManager = prefabManager;
        }

        [Conditional("DEBUG")]
        public void CreateClanPyramid(Tree baseTree, Action<TreeClanTemplate> finishedCallback, int pyramidsCount,
            float billboardMarginMultiplier = 1)
        {
            TreePrefabComplexer complexer = new TreePrefabComplexer();
            UnityEngine.Random.InitState(baseTree.data.GetHashCode());

            Tree baseTreeSaveCopy = _prefabManager.CreateTreeCopies(baseTree, 1)[0];

            List<Tree> treeCopies = _prefabManager.CreateTreeCopies(baseTreeSaveCopy, 5);
            List<Tree> createdTrees = new List<Tree>();

            for (int pyramidIdx = 0; pyramidIdx < pyramidsCount; pyramidIdx++)
            {
                var fullDetailTree = treeCopies[pyramidIdx];

                if (pyramidIdx > 0)
                {
                    int seed = pyramidIdx * 22;
                    RandomizeTree(fullDetailTree, baseTree.gameObject.transform.worldToLocalMatrix, seed);
                    complexer.AddFeatures(fullDetailTree, baseTree.gameObject.transform.worldToLocalMatrix);
                }

                var reducedDetailTree = _prefabManager.CreateTreeCopies(fullDetailTree, 1)[0];

                SimplyfyTree(reducedDetailTree, baseTree.gameObject.transform.worldToLocalMatrix);

                createdTrees.Add(fullDetailTree);
                createdTrees.Add(reducedDetailTree);
            }

            var pyramidTemplateList = new List<TreePyramidTemplate>();

            for (var i = 0; i < pyramidsCount; i++)
            {
                var instancedTreeObject = GameObject.Instantiate(createdTrees[i * 2]);
                instancedTreeObject.gameObject.transform.localPosition = new Vector3(-100 + i * 20, -100, -100);
                var i1 = i;
                _billboardGenerator.AddOrder(
                    new BillboardTemplateGenerationOrder(256, 8, instancedTreeObject.gameObject, (generationResult) =>
                    {
                        var collageBillboardTexture = CreateCollageBillboardTexture(generationResult.GeneratedTextures,
                            generationResult.ScaleOffsets);

                        pyramidTemplateList.Add(new TreePyramidTemplate(collageBillboardTexture, createdTrees[i1 * 2],
                            createdTrees[i1 * 2 + 1]));

                        if (pyramidTemplateList.Count == pyramidsCount)
                        {
                            finishedCallback(new TreeClanTemplate(pyramidTemplateList));
                        }
                    }, billboardMarginMultiplier));
            }
        }

        public class TreeMaterialData
        {
            public Material OptimizedCutoutMaterial;
            public Material OptimizedSolidMaterial;
            public Material LeafMaterial;
            public Material BranchMaterial;
            public Material BreakMaterial;
            public Material FrondMaterial;

            private TreeMaterialData()
            {
            }

            public static TreeMaterialData CreateFromTreeData(Tree tree)
            {
                var treeData = tree.data as TreeData;
                var materialData = new TreeMaterialData();
                materialData.OptimizedCutoutMaterial = treeData.optimizedCutoutMaterial;
                materialData.OptimizedSolidMaterial = treeData.optimizedSolidMaterial;

                if (treeData.branchGroups.Length != 0)
                {
                    var branchGroup = treeData.branchGroups[0];
                    materialData.BranchMaterial = branchGroup.materialBranch;
                    materialData.BreakMaterial = branchGroup.materialBreak;
                    materialData.FrondMaterial = branchGroup.materialFrond;
                }

                if (treeData.leafGroups.Length != 0)
                {
                    var leafGroup = treeData.leafGroups[0];
                    materialData.LeafMaterial = leafGroup.materialLeaf;
                }

                return materialData;
            }

            public void FillTree(Tree fullDetailTree)
            {
                TreeData treeData = fullDetailTree.data as TreeData;

                treeData.optimizedCutoutMaterial = OptimizedCutoutMaterial;
                treeData.optimizedSolidMaterial = OptimizedSolidMaterial;

                foreach (var branchGroup in treeData.branchGroups)
                {
                    branchGroup.materialBranch = BranchMaterial;
                    branchGroup.materialBreak = BreakMaterial;
                    branchGroup.materialFrond = FrondMaterial;
                }
                foreach (var leafGroup in treeData.leafGroups)
                {
                    leafGroup.materialLeaf = LeafMaterial;
                }
            }
        }

        private void RandomizeTree(Tree tree, Matrix4x4 transformMatrix, int newSeed)
        {
            var data = tree.data as TreeData;
            foreach (var group in data.branchGroups)
            {
                group.seed = newSeed;
                group.UpdateSeed();
            }
            foreach (var group in data.leafGroups)
            {
                group.seed = newSeed;
                group.UpdateSeed();
            }
            data.root.seed = newSeed;
            data.root.UpdateSeed();
            Material[] materials;
            data.UpdateMesh(transformMatrix, out materials);
        }

        private void SimplyfyTree(Tree tree, Matrix4x4 transformMatrix)
        {
            var data = tree.data as TreeData;

            data.root.adaptiveLODQuality = 0f;
            foreach (var branch in data.branchGroups)
            {
                branch.lodQualityMultiplier = 0f;
            }

            var leafBranchGroups = data.branchGroups
                .Where(
                    g => g.childGroupIDs.All(c => data.GetGroup(c) is TreeGroupLeaf)).ToList();
            if (leafBranchGroups.Count() != data.branchGroups.Length)
            {
                foreach (var groupToDisable in leafBranchGroups)
                {
                    Debug.Log($"Group Disabling!! {data.branchGroups.Length} {leafBranchGroups.Count}");
                    //groupToDisable.visible = false;
                }
            }

            const float lessLeavesMultiplier = 0.4f;
            const float leafSizeMultiplier = 1.03f;

            foreach (var leafGroup in data.leafGroups)
            {
                leafGroup.distributionFrequency = (int) (leafGroup.distributionFrequency * lessLeavesMultiplier);
                data.UpdateFrequency(leafGroup.uniqueID);

                leafGroup.distributionScale = 0f;
                data.UpdateDistribution(leafGroup.uniqueID);
                // growthScale to jest pole distributionScale

                leafGroup.size *= leafSizeMultiplier;
            }

            Material[] materials;
            data.UpdateMesh(transformMatrix, out materials);
        }

        private BillboardCollageTexture CreateCollageBillboardTexture(List<Texture2D> listOfTextures,
            Vector3 scaleOffsets)
        {
            int width = listOfTextures[0].width;
            int height = listOfTextures[0].height;
            int textureCount = listOfTextures.Count;

            int maxXTextures = Mathf.CeilToInt(2048 / (float) width);
            int xTexturesCount = Mathf.Min(maxXTextures, textureCount);
            int yTexturesCount = Mathf.CeilToInt(textureCount / (float) xTexturesCount);

            var collage = new Texture2D(width * xTexturesCount, height * yTexturesCount, TextureFormat.RGBA32, false);

            for (int texNo = 0; texNo < textureCount; texNo++)
            {
                int xTexturePosition = texNo % xTexturesCount;
                int yTexturePosition = Mathf.FloorToInt(texNo / (float) xTexturesCount);

                int xOffset = xTexturePosition * width;
                int yOffset = yTexturePosition * height;

                var currTexture = listOfTextures[texNo];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        collage.SetPixel(xOffset + x, y + yOffset, currTexture.GetPixel(x, y));
                    }
                }
            }

            return new BillboardCollageTexture(textureCount, yTexturesCount, xTexturesCount, collage, scaleOffsets);
        }
    }


    public class TreePrefabComplexer
    {
        private List<BaseTreeGroupFeatureComplexer<TreeGroupBranch>> _branchComplexers =
            new List<BaseTreeGroupFeatureComplexer<TreeGroupBranch>>();

        private List<BaseTreeGroupFeatureComplexer<TreeGroupLeaf>> _leafComplexers =
            new List<BaseTreeGroupFeatureComplexer<TreeGroupLeaf>>();

        public void AddFeatures(Tree tree, Matrix4x4 transformMatrix)
        {
            var treeData = tree.data as TreeData;
            foreach (var branchGroup in treeData.branchGroups)
            {
                List<AfterFeatureModificationAction> afterChangingActions = new List<AfterFeatureModificationAction>();
                foreach (var branchComplexer in _branchComplexers)
                {
                    var randomFeatureAddingDecider = UnityEngine.Random.value;
                    if (randomFeatureAddingDecider < branchComplexer.UsagePropability)
                    {
                        if (branchComplexer.CheckFiltering(treeData, branchGroup))
                        {
                            branchComplexer.AddFeature(branchGroup);
                            afterChangingActions.AddRange(branchComplexer.AfterModificationsActions);
                        }
                    }
                }
                //branchGroup.distributionFrequency = 5;
                //treeData.UpdateFrequency(branchGroup.uniqueID);
                foreach (var action in afterChangingActions.Distinct())
                {
                    action.PerformAction(branchGroup, treeData);
                }
            }

            foreach (var leafGroup in treeData.leafGroups)
            {
                List<AfterFeatureModificationAction> afterChangingActions = new List<AfterFeatureModificationAction>();
                foreach (var leafComplexer in _leafComplexers)
                {
                    var randomFeatureAddingDecider = UnityEngine.Random.value;
                    if (randomFeatureAddingDecider < leafComplexer.UsagePropability)
                    {
                        if (leafComplexer.CheckFiltering(treeData, leafGroup))
                        {
                            leafComplexer.AddFeature(leafGroup);
                            afterChangingActions.AddRange(leafComplexer.AfterModificationsActions);
                        }
                    }
                }
                foreach (var action in afterChangingActions.Distinct())
                {
                    action.PerformAction(leafGroup, treeData);
                }
            }
            Material[] materials;
            //treeData.root.UpdateFrequency(treeData);
            treeData.UpdateMesh(transformMatrix, out materials);

            //foreach (var complexer in complexers)
            //{
            //    complexer.AddFeature();
            //}
        }

        public TreePrefabComplexer()
        {
            //_branchComplexers.Add(
            //    new TreeGroupFeatureComplexer<double, TreeGroupBranch>("branchFrequencyTrunk",
            //        usagePropability: 0.5f,
            //        valueProvider:
            //            FeatureValueProviderUtils.Create<double>(
            //                oldValue => 1 + 
            //                    Math.Abs(FeatureValueProviderUtils.Gaussian(0, 0.5f).ProvideValue(oldValue))),
            //        featureChangingAction: (group, provider) => group.distributionFrequency = Mathf.RoundToInt((float)provider.ProvideValue(group.distributionFrequency)),
            //        afterModificationsActions: new[] { AfterFeatureModificationAction.UpdateFrequency() },//should be udate frequency, but it results in null pointer in library code
            //        groupFilter: TreeGroupFilter<TreeGroupBranch>.OnlyTrunk()));
            _branchComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupBranch>("branchFrequencyBranch",
                    usagePropability: 0.9f,
                    valueProvider:
                    FeatureValueProviderUtils.Create<double>(
                        oldValue => oldValue *
                                    FeatureValueProviderUtils.Gaussian(1, 0.35f).ProvideValue(oldValue)),
                    featureChangingAction: (group, provider) => group.distributionFrequency =
                        Mathf.RoundToInt((float) provider.ProvideValue(group.distributionFrequency)),
                    afterModificationsActions: new[]
                    {
                        AfterFeatureModificationAction.UpdateFrequency()
                    }, //should be udate frequency, but it results in null pointer in library code
                    groupFilter: TreeGroupFilter<TreeGroupBranch>.EverythingButTrunk()));
            _branchComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupBranch>("growthAngleTrunk",
                    usagePropability: 0.7f,
                    valueProvider:
                    FeatureValueProviderUtils.Clamp01(
                        FeatureValueProviderUtils.MirrorToPositive(
                            FeatureValueProviderUtils.Gaussian(0, 0.2f))),
                    featureChangingAction: (branch, provider) => branch.distributionPitch =
                        (float) provider.ProvideValue(branch.distributionPitch),
                    afterModificationsActions: new[] {AfterFeatureModificationAction.UpdateDistribution()},
                    groupFilter: TreeGroupFilter<TreeGroupBranch>.OnlyTrunk()));
            _branchComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupBranch>("growthAngleNotTrunk",
                    usagePropability: 0.2f,
                    valueProvider: FeatureValueProviderUtils.Clamp01(
                        FeatureValueProviderUtils.Create<double>(
                            oldValue => oldValue *
                                        FeatureValueProviderUtils.Gaussian(1, 0.2f).ProvideValue(oldValue))),
                    featureChangingAction: (branch, provider) => branch.distributionPitch =
                        (float) provider.ProvideValue(branch.distributionPitch),
                    afterModificationsActions: new[] {AfterFeatureModificationAction.UpdateDistribution()},
                    groupFilter: TreeGroupFilter<TreeGroupBranch>.EverythingButTrunk()));
            _branchComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupBranch>("radius",
                    usagePropability: 0.5f,
                    valueProvider: FeatureValueProviderUtils.Clamp(0.15, 1,
                        FeatureValueProviderUtils.Create<double>(
                            oldValue => oldValue *
                                        FeatureValueProviderUtils.Gaussian(1, 0.5f).ProvideValue(oldValue))),
                    featureChangingAction: (branch, provider) => branch.distributionPitch =
                        (float) provider.ProvideValue(branch.distributionPitch),
                    afterModificationsActions: new[] {AfterFeatureModificationAction.UpdateDistribution()},
                    groupFilter: TreeGroupFilter<TreeGroupBranch>.AllwaysTrue()));
            _branchComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupBranch>("crinkliness",
                    usagePropability: 0.3f,
                    valueProvider:
                    FeatureValueProviderUtils.Clamp01(
                        FeatureValueProviderUtils.MirrorToPositive(
                            FeatureValueProviderUtils.Gaussian(0, 0.3f))),
                    featureChangingAction: (branch, provider) => branch.crinklyness =
                        (float) provider.ProvideValue(branch.crinklyness),
                    afterModificationsActions: null,
                    groupFilter: TreeGroupFilter<TreeGroupBranch>.AllwaysTrue()));

            _branchComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupBranch>("flareRadius",
                    usagePropability: 0.25f,
                    valueProvider:
                    FeatureValueProviderUtils.Clamp01(
                        FeatureValueProviderUtils.MirrorToPositive(
                            FeatureValueProviderUtils.Gaussian(0, 0.5f))),
                    featureChangingAction: (branch, provider) => branch.flareSize =
                        (float) provider.ProvideValue(branch.flareSize),
                    afterModificationsActions: null,
                    groupFilter: TreeGroupFilter<TreeGroupBranch>.AllwaysTrue()));
            _branchComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupBranch>("flareHeight",
                    usagePropability: 0.4f,
                    valueProvider: FeatureValueProviderUtils.Clamp01(
                        FeatureValueProviderUtils.Create<double>(
                            oldValue => oldValue *
                                        FeatureValueProviderUtils.Gaussian(1, 0.25f).ProvideValue(oldValue))),
                    featureChangingAction: (branch, provider) => branch.flareHeight =
                        (float) provider.ProvideValue(branch.flareHeight),
                    afterModificationsActions: null,
                    groupFilter: TreeGroupFilter<TreeGroupBranch>.AllwaysTrue()));

            _branchComplexers.Add(
                new TreeGroupFeatureComplexer<Vector2, TreeGroupBranch>("length",
                    usagePropability: 0.6f,
                    valueProvider:
                    FeatureValueProviderUtils.GenerateRange(
                        //center
                        FeatureValueProviderUtils.Create<double>(
                            oldValue => oldValue *
                                        FeatureValueProviderUtils.Gaussian(1, 0.5f).ProvideValue(oldValue)),
                        //length
                        FeatureValueProviderUtils.Create<double>(
                            oldValue => oldValue *
                                        FeatureValueProviderUtils.Gaussian(1, 0.6f).ProvideValue(oldValue))
                    ),
                    featureChangingAction: (branch, provider) => branch.height = provider.ProvideValue(branch.height),
                    afterModificationsActions: new[] {AfterFeatureModificationAction.UpdateDistribution()},
                    groupFilter: TreeGroupFilter<TreeGroupBranch>.AllwaysTrue()));

            _branchComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupBranch>("breakChance",
                    usagePropability: 0.1f,
                    valueProvider:
                    FeatureValueProviderUtils.Clamp01(
                        FeatureValueProviderUtils.MirrorToPositive(
                            FeatureValueProviderUtils.Gaussian(0.5f, 0.2f))),
                    featureChangingAction: (branch, provider) => branch.breakingChance =
                        (float) provider.ProvideValue(branch.breakingChance),
                    afterModificationsActions: null,
                    groupFilter: TreeGroupFilter<TreeGroupBranch>.AllwaysTrue()));

            _branchComplexers.Add(
                new TreeGroupFeatureComplexer<Vector2, TreeGroupBranch>("breakLocation",
                    usagePropability: 0.8f,
                    valueProvider:
                    FeatureValueProviderUtils.GenerateRange(
                        //center
                        FeatureValueProviderUtils.Create<double>(
                            oldValue => oldValue *
                                        FeatureValueProviderUtils.Gaussian(1, 0.5f).ProvideValue(oldValue)),
                        //length
                        FeatureValueProviderUtils.Create<double>(
                            oldValue => oldValue *
                                        FeatureValueProviderUtils.Gaussian(1, 0.5f).ProvideValue(oldValue))
                    ),
                    featureChangingAction: (branch, provider) => branch.breakingSpot =
                        provider.ProvideValue(branch.breakingSpot),
                    afterModificationsActions: null,
                    groupFilter: TreeGroupFilter<TreeGroupBranch>.AllwaysTrue()));

            //////////// LEAFS
            _leafComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupLeaf>("leafFrequency",
                    usagePropability: 1f,
                    valueProvider:
                    FeatureValueProviderUtils.Create<double>(
                        oldValue => oldValue *
                                    FeatureValueProviderUtils.Gaussian(1, 0.15f).ProvideValue(oldValue)),
                    featureChangingAction: (group, provider) => group.distributionFrequency =
                        (int) provider.ProvideValue(group.distributionFrequency),
                    afterModificationsActions: new[] {AfterFeatureModificationAction.UpdateFrequency()},
                    groupFilter: TreeGroupFilter<TreeGroupLeaf>.AllwaysTrue()));
            _leafComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupLeaf>("leafGrowthScale",
                    usagePropability: 1f,
                    valueProvider:
                    FeatureValueProviderUtils.Clamp01(
                        FeatureValueProviderUtils.Gaussian(0.5f, 0.2f)),
                    featureChangingAction: (group, provider) => group.distributionScale =
                        (float) provider.ProvideValue(group.distributionScale),
                    afterModificationsActions: new[] {AfterFeatureModificationAction.UpdateDistribution()},
                    groupFilter: TreeGroupFilter<TreeGroupLeaf>.AllwaysTrue()));
            _leafComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupLeaf>("leafGrowthTwirl",
                    usagePropability: 0.6f,
                    valueProvider:
                    FeatureValueProviderUtils.Clamp01(
                        FeatureValueProviderUtils.Gaussian(0.5f, 0.2f)),
                    featureChangingAction: (group, provider) => group.distributionTwirl =
                        (float) provider.ProvideValue(group.distributionTwirl),
                    afterModificationsActions: new[] {AfterFeatureModificationAction.UpdateDistribution()},
                    groupFilter: TreeGroupFilter<TreeGroupLeaf>.AllwaysTrue()));
            _leafComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupLeaf>("leafGrowthPitch",
                    usagePropability: 0.6f,
                    valueProvider:
                    FeatureValueProviderUtils.Clamp01(
                        FeatureValueProviderUtils.Gaussian(0.5f, 0.2f)),
                    featureChangingAction: (group, provider) => group.distributionPitch =
                        (float) provider.ProvideValue(group.distributionPitch),
                    afterModificationsActions: new[] {AfterFeatureModificationAction.UpdateDistribution()},
                    groupFilter: TreeGroupFilter<TreeGroupLeaf>.AllwaysTrue()));
            _leafComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupLeaf>("leafPerpendicularAlign",
                    usagePropability: 0.4f,
                    valueProvider: FeatureValueProviderUtils.Clamp01(
                        FeatureValueProviderUtils.Create<double>(
                            oldValue => oldValue *
                                        FeatureValueProviderUtils.Gaussian(1, 0.25f).ProvideValue(oldValue))),
                    featureChangingAction: (group, provider) => group.perpendicularAlign =
                        (float) provider.ProvideValue(group.perpendicularAlign),
                    afterModificationsActions: null,
                    groupFilter: TreeGroupFilter<TreeGroupLeaf>.AllwaysTrue()));
            _leafComplexers.Add(
                new TreeGroupFeatureComplexer<double, TreeGroupLeaf>("leafHorizontalAlign",
                    usagePropability: 0.4f,
                    valueProvider: FeatureValueProviderUtils.Clamp01(
                        FeatureValueProviderUtils.Create<double>(
                            oldValue => oldValue *
                                        FeatureValueProviderUtils.Gaussian(1, 0.25f).ProvideValue(oldValue))),
                    featureChangingAction: (group, provider) => group.horizontalAlign =
                        (float) provider.ProvideValue(group.horizontalAlign),
                    afterModificationsActions: null,
                    groupFilter: TreeGroupFilter<TreeGroupLeaf>.AllwaysTrue()));
        }
    }

    public abstract class BaseTreeGroupFeatureComplexer<T>
    {
        protected String _name;
        private float _usagePropability;
        private List<AfterFeatureModificationAction> _afterModificationsActions;
        private TreeGroupFilter<T> _filter;

        protected BaseTreeGroupFeatureComplexer(string name, float usagePropability,
            AfterFeatureModificationAction[] afterModificationsActions,
            TreeGroupFilter<T> filter)
        {
            _name = name;
            _usagePropability = usagePropability;
            if (afterModificationsActions == null)
            {
                _afterModificationsActions = new List<AfterFeatureModificationAction>();
            }
            else
            {
                _afterModificationsActions = afterModificationsActions.ToList();
            }
            _filter = filter;
        }

        public abstract void AddFeature(T groupBranch);

        public float UsagePropability
        {
            get { return _usagePropability; }
        }

        public List<AfterFeatureModificationAction> AfterModificationsActions
        {
            get { return _afterModificationsActions; }
        }

        public bool CheckFiltering(TreeData data, T branch)
        {
            return _filter.Check(branch, data);
        }
    }

    public class TreeGroupFeatureComplexer<T, X> : BaseTreeGroupFeatureComplexer<X>
    {
        private FeatureValueProvider<T> _valueProvider;
        private Action<X, FeatureValueProvider<T>> _featureChangingAction;

        public TreeGroupFeatureComplexer(String name,
            float usagePropability,
            FeatureValueProvider<T> valueProvider,
            Action<X, FeatureValueProvider<T>> featureChangingAction,
            AfterFeatureModificationAction[] afterModificationsActions,
            TreeGroupFilter<X> groupFilter) : base(name, usagePropability, afterModificationsActions, groupFilter)
        {
            _valueProvider = valueProvider;
            _featureChangingAction = featureChangingAction;
        }

        public override void AddFeature(X groupBranch)
        {
            _featureChangingAction(groupBranch, _valueProvider);
        }
    }

    public class TreeGroupFilter<T>
    {
        private Func<T, TreeData, bool> _predicate;

        public TreeGroupFilter(Func<T, TreeData, bool> predicate)
        {
            _predicate = predicate;
        }

        public bool Check(T branch, TreeData data)
        {
            return _predicate(branch, data);
        }

        public static TreeGroupFilter<T> AllwaysTrue()
        {
            return new TreeGroupFilter<T>((group, data) => true);
        }

        public static TreeGroupFilter<TreeGroupBranch> OnlyTrunk()
        {
            return new TreeGroupFilter<TreeGroupBranch>(
                (group, data) => TreeDataUtils.GetLevelOfGroup(data, group) == 0);
        }

        public static TreeGroupFilter<TreeGroupBranch> EverythingButTrunk()
        {
            return new TreeGroupFilter<TreeGroupBranch>(
                (group, data) => TreeDataUtils.GetLevelOfGroup(data, group) != 0);
        }
    }

    public static class TreeDataUtils
    {
        public static int GetLevelOfGroup(TreeData data, TreeGroup groupToLookFor)
        {
            int groupIdToLookFor = groupToLookFor.uniqueID;
            int currentLevel = 0;
            var currentChildIds = data.root.childGroupIDs.ToList();
            var newChildIds = new List<int>();
            while (currentChildIds.Count != 0)
            {
                foreach (var id in currentChildIds)
                {
                    var group = data.GetGroup(id);
                    if (group.uniqueID == groupIdToLookFor)
                    {
                        return currentLevel;
                    }
                    else
                    {
                        newChildIds.AddRange(group.childGroupIDs.ToList());
                    }
                }
                currentChildIds = newChildIds.ToList();
                newChildIds = new List<int>();
                currentLevel++;
            }
            Preconditions.Fail("Cannot find group of id " + groupIdToLookFor);
            return -1;
        }
    }

    public class FeatureValueProvider<T>
    {
        private Func<T, T> _valueGenerator;

        public FeatureValueProvider(Func<T, T> valueGenerator)
        {
            _valueGenerator = valueGenerator;
        }

        public FeatureValueProvider(Func<T> valueGenerator) : this((ignored) => valueGenerator())
        {
        }

        public T ProvideValue(T oldValue)
        {
            var newValue = _valueGenerator(oldValue);
            return newValue;
        }
    }

    public static class FeatureValueProviderUtils
    {
        public static FeatureValueProvider<T> Create<T>(Func<T, T> action)
        {
            return new FeatureValueProvider<T>(action);
        }

        public static FeatureValueProvider<double> Gaussian(float mean, float stdDev)
        {
            return new FeatureValueProvider<double>(() => RandomUtils.RandomGaussian(mean, stdDev));
        }

        public static FeatureValueProvider<double> MirrorToPositive(FeatureValueProvider<double> oldProvider)
        {
            return new FeatureValueProvider<double>((oldValue) => Math.Abs(oldProvider.ProvideValue(oldValue)));
        }

        public static FeatureValueProvider<double> Clamp01(FeatureValueProvider<double> oldProvider)
        {
            Func<double, double> newFunc = (oldValue) =>
            {
                var value = (oldProvider.ProvideValue(oldValue));
                if (value > 1)
                {
                    return 1;
                }
                else if (value < 0)
                {
                    return 0;
                }
                return value;
            };
            return new FeatureValueProvider<double>(newFunc);
        }

        public static FeatureValueProvider<Vector2> GenerateRange(FeatureValueProvider<double> centerValueProvider,
            FeatureValueProvider<double> rangeValueProvider)
        {
            return new FeatureValueProvider<Vector2>((oldRange) =>
            {
                var oldCenter = oldRange[0] + (oldRange[1] - oldRange[0]) / 2;
                var newCenter = centerValueProvider.ProvideValue(oldCenter);

                var oldLength = oldRange[1] - oldRange[0];
                var newLength = centerValueProvider.ProvideValue(oldLength);

                var start = ((float) (newCenter - newLength / 2f));
                var end = ((float) (newCenter + newLength / 2f));
                return new Vector2(start, end);
            });
        }

        public static FeatureValueProvider<double> Clamp(double min, double max,
            FeatureValueProvider<double> oldProvider)
        {
            Func<double, double> newFunc = (oldValue) =>
            {
                var value = (oldProvider.ProvideValue(oldValue));
                if (value > max)
                {
                    return max;
                }
                else if (value < min)
                {
                    return min;
                }
                return value;
            };
            return new FeatureValueProvider<double>(newFunc);
        }
    }

    public class AfterFeatureModificationAction
    {
        private Action<TreeGroup, TreeData> _actionToDo;

        public AfterFeatureModificationAction(Action<TreeGroup, TreeData> actionToDo)
        {
            _actionToDo = actionToDo;
        }

        public void PerformAction(TreeGroup group, TreeData treeData)
        {
            _actionToDo(group, treeData);
        }

        public static AfterFeatureModificationAction UpdateDistribution()
        {
            return new AfterFeatureModificationAction((b, treeData) => b.UpdateDistribution(true, true));
        }

        public static AfterFeatureModificationAction UpdateFrequency()
        {
            return new AfterFeatureModificationAction((b, treeData) => treeData.UpdateFrequency(b.uniqueID));
        }
    }
#endif



    public class BillboardCollageTexture
    {
        private int _subTexturesCount;
        private int _linesCount;
        private int _columnsCount;
        private Texture2D _collage;
        private readonly Vector3 _scaleOffsets;

        public BillboardCollageTexture(int subTexturesCount, int linesCount, int columnsCount, Texture2D collage,
            Vector3 scaleOffsets)
        {
            _subTexturesCount = subTexturesCount;
            _linesCount = linesCount;
            _columnsCount = columnsCount;
            _collage = collage;
            _scaleOffsets = scaleOffsets;
        }

        public int SubTexturesCount
        {
            get { return _subTexturesCount; }
        }

        public int LinesCount
        {
            get { return _linesCount; }
        }

        public int ColumnsCount
        {
            get { return _columnsCount; }
        }

        public Texture2D Collage
        {
            get { return _collage; }
        }

        public Vector3 ScaleOffsets
        {
            get { return _scaleOffsets; }
        }
    }

    public class TreeClanTemplate
    {
        private List<TreePyramidTemplate> _treePyramids;

        public TreeClanTemplate(List<TreePyramidTemplate> treePyramids)
        {
            _treePyramids = treePyramids;
        }

        public List<TreePyramidTemplate> TreePyramids
        {
            get { return _treePyramids; }
        }
    }

    public class TreePyramidTemplate
    {
        private readonly BillboardCollageTexture _collageBillboardTexture;
        private readonly Tree _fullDetailTree;
        private readonly Tree _simplifiedTree;

        public TreePyramidTemplate(BillboardCollageTexture collageBillboardTexture, Tree fullDetailTree,
            Tree simplifiedTree)
        {
            _collageBillboardTexture = collageBillboardTexture;
            _fullDetailTree = fullDetailTree;
            _simplifiedTree = simplifiedTree;
        }

        public BillboardCollageTexture CollageBillboardTexture
        {
            get { return _collageBillboardTexture; }
        }

        public Tree FullDetailTree
        {
            get { return _fullDetailTree; }
        }

        public Tree SimplifiedTree
        {
            get { return _simplifiedTree; }
        }
    }
}