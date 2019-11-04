using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.AI.Bot;
using Assets.AI.Recorder;
using Assets.Utils;
using UnityEngine;

namespace Assets.AI
{
    public class BehaviourTreeRunnerOC : MonoBehaviour
    {
        public bool ShouldRegister;
        private BehaviourTreeRoot _root;

        private bool _started = false;
        private Action<BehaviourTreeRoot> _treeCreatedCallback;

        public void Update()
        {
            if (_root != null)
            {
                if (!_started)
                {
                    if (ShouldRegister)
                    {
                        var recorder = FindObjectOfType<AiRecorderGO>();
                        _root.AddListener(new RecordingTaskListener(recorder.Recorder, gameObject));
                    }
                    _started = true;
                    _treeCreatedCallback(_root);
                }
                _root.Run();
            }
        }

        public BehaviourTreeRoot Root
        {
            get => _root;
            set { _root = value;
                _root.Owner = GetComponent<AIBotOC>();
            }
        }


        public Action<BehaviourTreeRoot> TreeCreatedCallback
        {
            set
            {
                Preconditions.Assert(!_started, "Tree was arleady started");
                _treeCreatedCallback = value;
            }
        }
    }

    public class BehaviourTreeRoot 
    {
        private AITask _child;
        private AIBotOC _owner;

        private bool _completed;
        private bool _started;
        private List<AITaskListener> _listeners;

        public BehaviourTreeRoot(AITask child)
        {
            _child = child;
            _completed = false;
            _started = false;
            _listeners = new List<AITaskListener>();
        }

        public void Run()
        {
            if (!_started)
            {
                _child.OwningBot = _owner;
                _started = true;
            }
            _child.EarlyStart();

            if (_completed)
            {
                return;
            }
            _listeners.ForEach(c => c.StartBehaviourTreeUpdate(this));
            var childStatus = _child.Run();
            if (childStatus != AIOneRunStatus.Running)
            {
                UnityEngine.Debug.Log("Behaviour tree completed with state " + childStatus);
                _completed = true;
            }
        }

        public void AddListener( AITaskListener listener)
        {
            _child.AddListener(listener);
            _listeners.Add(listener);
        }

        public AITask Child => _child;

        public bool Completed => _completed;

        public AIBotOC Owner
        {
            set => _owner = value;
        }
    }
}
