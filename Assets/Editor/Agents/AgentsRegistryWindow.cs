using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ActorSystem;
using Assets.ActorSystem.Registry;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Assets.Editor.Agents
{
    public class AgentsRegistryWindow : EditorWindow
    {
        private int _selectedFrame=0;
        private int _maximumFrame = 100;

        private TreeViewState _treeViewState;
        private AsRegistryTreeView _treeView;

        // Add menu item named "My Window" to the Window menu
        [MenuItem("Window/AgentsRegistryWindow")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(AgentsRegistryWindow));
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
            var registryActor = FindObjectOfType<AsTelegramRegistryGO>();
            _maximumFrame = registryActor.MaximumFrame;
            var reg = registryActor?.Registry;
            if (reg != null && reg.ContainsSnapshot(frame))
            {
                _treeView = new AsRegistryTreeView(reg.GetSnapshotFromFrame(frame), _treeViewState);
            }
            else
            {
                _treeView = null;
            }
        }
    }

    public class AsRegistryTreeView : TreeView
    {
        private AsActorSystemFrameSnapshot _frameSnapshot;
        private int _lastTreeItemId;
        public AsRegistryTreeView(AsActorSystemFrameSnapshot frameSnapshot, TreeViewState state) : base(state)
        {
            _frameSnapshot = frameSnapshot;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            _lastTreeItemId = 0;
            var root = new TreeViewItem {id = _lastTreeItemId++, depth = -1, displayName = "Root"};

            foreach (var pair in _frameSnapshot.ActorFrameSnapshots)
            {
               root.AddChild(CreateActorSnapshotItem(pair));
            }

            // Return root of the tree
            return root;
        }

        private TreeViewItem CreateActorSnapshotItem( KeyValuePair<AsAbstractActor, AsActorFrameSnapshot> pair)
        {
            var actorRootItem = new TreeViewItem(_lastTreeItemId++, 0, CreateNameOfActor(pair.Key));

            var replyChainsRoot = new TreeViewItem(_lastTreeItemId++, 1, "Recieved");
            actorRootItem.AddChild(replyChainsRoot);
            foreach (var replyPair in pair.Value.Replies)
            {
                var aRecievedItem = new TreeViewItem(_lastTreeItemId++, 2, "RecievedMessage");
                replyChainsRoot.AddChild(aRecievedItem);
                aRecievedItem.AddChild(CreateTelegramDescribingItem(replyPair.Key, 3));

                if (replyPair.Value.Any())
                {
                    var repliesItem = new TreeViewItem(_lastTreeItemId++, 2, "Replies");
                    replyChainsRoot.AddChild(repliesItem);
                    foreach (var replyTelegram in replyPair.Value)
                    {
                        repliesItem.AddChild(CreateTelegramDescribingItem(replyTelegram, 3));
                    }
                }
            }

            var initiatedMessagesRoot = new TreeViewItem(_lastTreeItemId++, 1, "Initiated");
            actorRootItem.AddChild(initiatedMessagesRoot);
            foreach (var initiatedTelegrams in pair.Value.Initiated)
            {
                initiatedMessagesRoot.AddChild(CreateTelegramDescribingItem(initiatedTelegrams, 2));
            }
            return actorRootItem;
        }

        private TreeViewItem CreateTelegramDescribingItem(AsTelegram telegram, int depth)
        {
            var name = $"Frame:{telegram.Timestamp.FrameNo} Idx:{telegram.Timestamp.MessageIndex}";
            var item = new TreeViewItem(_lastTreeItemId++, depth, name);
            item.AddChild(new TreeViewItem(_lastTreeItemId++, depth+1, "Reciever: "+CreateNameOfActor(telegram.Reciever)));
            item.AddChild(CreatePayloadDescribingItem(telegram.Payload, depth+1));
            return item;
        }

        private TreeViewItem CreatePayloadDescribingItem(object telegramPayload, int depth)
        {
            var name = $"Payload: {telegramPayload.GetType()}";
            var item = new TreeViewItem(_lastTreeItemId++, depth, name);
            foreach (var fi in telegramPayload.GetType().GetFields())
            {
                var fieldName = $"Field {fi.Name} : {fi.GetValue(telegramPayload)}";
                var fieldItem = new TreeViewItem(_lastTreeItemId++, depth+1, fieldName);
                item.AddChild(fieldItem);
            }
            return item;
        }


        private string CreateNameOfActor(AsAbstractActor asActor)
        {
            var ownerName = "NoOwner";
            if (asActor.Owner != null)
            {
                ownerName = asActor.Owner.name;
            }

            return $"{asActor.GetType()}: {ownerName}";
        }
    }
}
