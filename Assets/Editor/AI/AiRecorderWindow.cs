using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.AI;
using Assets.AI.Recorder;
using Assets.Editor.Agents;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Assets.Editor.AI
{
    public class AiRecorderWindow : EditorWindow
    {
        private int _selectedFrame=0;
        private int _maximumFrame = 1000;

        private TreeViewState _treeViewState;
        private AiRecorderTreeView _treeView;

        [MenuItem("Window/AiRecorderWindow")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(AiRecorderWindow));
        }

        public void OnEnable()
        {
            if (_treeViewState == null)
            {
                _treeViewState = new TreeViewState();
            }
            RecreateTree(_selectedFrame);
        }

        void OnGUI()
        {
            DoFrameSelection();
            Rect rect = EditorGUILayout.BeginHorizontal("h");
            _treeView?.OnGUI(rect);
            EditorGUILayout.EndHorizontal();
        }
        private void DoFrameSelection()
        {
            var oldFrame = _selectedFrame;
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            var maximumFrame = _maximumFrame;
            _selectedFrame = EditorGUILayout.IntSlider(_selectedFrame, 0, maximumFrame);

            if (GUILayout.Button("+"))
            {
                _selectedFrame = Mathf.Min(_selectedFrame + 1, maximumFrame);
            }
            if (GUILayout.Button("-"))
            {
                _selectedFrame = Mathf.Max(_selectedFrame - 1, 0);
            }
            if (_selectedFrame != oldFrame)
            {
                RecreateTree(_selectedFrame);
            }
        }

        private void RecreateTree(int frame)
        {
            var recorderActor = FindObjectOfType<AiRecorderGO>();
            _maximumFrame = recorderActor.MaximumFrame;
            var reg = recorderActor.Recorder;
            if (reg != null && reg.Snapshots.ContainsKey(frame))
            {
                _treeView = new AiRecorderTreeView(reg.Snapshots[frame], _treeViewState);
            }
            else
            {
                _treeView = null;
            }
        }

    }

    public class AiRecorderTreeView : TreeView
    {
        private readonly List<AiBehaviourTreeFrameSnapshot> _snapshots;
        private int _lastTreeItemId;

        public AiRecorderTreeView(List<AiBehaviourTreeFrameSnapshot> snapshots, TreeViewState state) : base(state)
        {
            _snapshots = snapshots;
            Reload();
        }
        
        protected override TreeViewItem BuildRoot()
        {
            _lastTreeItemId = 0;
            var root = new TreeViewItem {id = _lastTreeItemId++, depth = -1, displayName = "Root"};

            foreach (var snapshot in _snapshots)
            {
               root.AddChild(CreateBehaviourTreeSnapshotItem(snapshot));
            }

            // Return root of the tree
            return root;
        }

        private TreeViewItem CreateBehaviourTreeSnapshotItem(AiBehaviourTreeFrameSnapshot snapshot)
        {
            var actorRootItem = new TreeViewItem(_lastTreeItemId++, 0, snapshot.Owner.name);
            var childSnapshotItem = CreateTaskSnapshotItem(snapshot.RootChild, 1);
            UpdateTreeViewItemWithCallInfo(childSnapshotItem, snapshot.RootCall );
            actorRootItem.AddChild(childSnapshotItem);
            return actorRootItem;
        }

        private TreeViewItem CreateTaskSnapshotItem(AiRegistryTaskSnapshot snapshot, int depth)
        {
            var item = new TreeViewItem(_lastTreeItemId++, depth, GetTaskName(snapshot.TaskWithStatus.Task)+" InStatus:"+snapshot.TaskWithStatus.Status);
            foreach (var child in snapshot.Children)
            {
                var childItem = CreateTaskSnapshotItem(child, depth + 1);
                var callInfo = snapshot.ChildCalls.FirstOrDefault(c => c.TaskAfterCall.Task == child.TaskWithStatus.Task);
                UpdateTreeViewItemWithCallInfo(childItem, callInfo);
                item.AddChild(childItem);
            }
            return item;
        }

        private void UpdateTreeViewItemWithCallInfo(TreeViewItem item, AiRegistryChildCall callInfo)
        {
            if (callInfo != null)
            {
                item.displayName = $"{item.displayName} || Call {callInfo.CallStatus} Status after {callInfo.TaskAfterCall.Status}";
            }
            else
            {
                item.displayName = $"{item.displayName} Ignored";
            }
        }

        private string GetTaskName(AITask task)
        {
            return task.GetType().Name;
        }
    }
}
