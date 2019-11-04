using System;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using TreeEditor;
#endif

namespace Assets.Trees.Generation
{
#if UNITY_EDITOR
    public class TreeModifier
    {
        private Dictionary<int, BranchGroupTemplate> _branchGroupTemplates = new Dictionary<int, BranchGroupTemplate>();
        private TreeRootTemplate _rootTemplate = new TreeRootTemplate();

        public BranchGroupTemplate GetBranchGroupTemplate(int branchGroupId)
        {
            if (!_branchGroupTemplates.ContainsKey(branchGroupId))
            {
                _branchGroupTemplates.Add(branchGroupId, new BranchGroupTemplate());
            }
            return _branchGroupTemplates[branchGroupId];
        }

        public TreeRootTemplate GetRootTemplate()
        {
            return _rootTemplate;
        }

        public void ApplyModifications(TreeData treeData, Matrix4x4 matrix)
        {
            foreach (var pair in _branchGroupTemplates)
            {
                var branchIndex = pair.Key;
                var branchGroup = treeData.branchGroups[branchIndex];
                var template = pair.Value;

                template.ApplyChanges(branchGroup, treeData);
            }

            _rootTemplate.ApplyChanges(treeData.root);

            Material[] outMaterial;
            treeData.UpdateMesh(matrix, out outMaterial);
        }


        public void GetAllBranchesTemplate()
        {
            throw new NotImplementedException();
        }
    }

    public class BranchGroupTemplate
    {
        private readonly List<BranchIndexWithAction> _branchActions = new List<BranchIndexWithAction>();

        public BranchGroupTemplate SetBranchDistributionFrequencyValue(int newValue)
        {
            _branchActions.Add(
                new BranchIndexWithAction(
                    (branch) => branch.distributionFrequency = newValue,
                    (treeData, branchId) => treeData.UpdateFrequency(branchId)));
            return this;
        }

        public void ApplyChanges(TreeGroupBranch branchGroup, TreeData treeData)
        {
            foreach (var action in _branchActions)
            {
                action.BranchAction(branchGroup);
            }

            foreach (var action in _branchActions)
            {
                action.DataAction(treeData, branchGroup.uniqueID);
            }
        }
    }

    public class TreeRootTemplate
    {
        private List<Action<TreeGroupRoot>> _rootActions = new List<Action<TreeGroupRoot>>();

        public TreeRootTemplate SetLodQuality(float value)
        {
            value = Mathf.Clamp01(value);
            _rootActions.Add((root) => root.adaptiveLODQuality = value);
            return this;
        }

        public void ApplyChanges(TreeGroupRoot root)
        {
            _rootActions.ForEach(action => action(root));
        }
    }

    public class BranchIndexWithAction
    {
        private readonly Action<TreeGroupBranch> _branchAction;
        private readonly Action<TreeData, int> _dataAction;

        public BranchIndexWithAction(Action<TreeGroupBranch> branchAction, Action<TreeData, int> dataAction)
        {
            this._branchAction = branchAction;
            _dataAction = dataAction;
        }

        public Action<TreeGroupBranch> BranchAction
        {
            get { return _branchAction; }
        }

        public Action<TreeData, int> DataAction
        {
            get { return _dataAction; }
        }
    }
#endif
}