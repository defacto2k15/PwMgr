using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.AI.Recorder
{
    public class AiRecorderGO : MonoBehaviour
    {
        private AiRecorder _recorder;
        public bool ShouldRecord;
        public int MaximumFrame = 1000;

        public void Start()
        {
            if (ShouldRecord)
            {
                _recorder = new AiRecorder();
            }
        }

        public AiRecorder Recorder => _recorder;
    }

    public class AiRecorder
    {
        private Dictionary<int,  List<AiBehaviourTreeFrameSnapshot>> _snapshots;

        public AiRecorder()
        {
            _snapshots = new Dictionary<int, List<AiBehaviourTreeFrameSnapshot>>();
        }

        public void RecordSnapshot(AiBehaviourTreeFrameSnapshot snapshot)
        {
            if (!_snapshots.ContainsKey(Time.frameCount))
            {
                _snapshots[Time.frameCount ] = new List<AiBehaviourTreeFrameSnapshot>();
            }
            _snapshots[Time.frameCount].Add(snapshot);
        }

        public Dictionary<int, List<AiBehaviourTreeFrameSnapshot>> Snapshots => _snapshots;
    }

    public class AiBehaviourTreeFrameSnapshot
    {
        public GameObject Owner;
        public AiRegistryTaskSnapshot RootChild;
        public AiRegistryChildCall RootCall;
    }

    public class AiRegistryChildCall
    {
        public AiRegistryTaskWithStatus TaskAfterCall;
        public AIOneRunStatus CallStatus;
    }

    public class AiRegistryTaskSnapshot
    {
        public AiRegistryTaskWithStatus TaskWithStatus;
        public List<AiRegistryTaskSnapshot> Children;
        public List<AiRegistryChildCall> ChildCalls;
    }

    public class AiRegistryTaskWithStatus
    {
        public AITask Task;
        public AITaskStatus Status;
    }

    public class RecordingTaskListener : AITaskListener
    {
        private readonly AiRecorder _recorder;
        private GameObject _owner;

        private AiRegistryTaskSnapshot _rootSnapshot;
        private Stack<AiRegistryTaskSnapshot> _snapshotsStack;

        public RecordingTaskListener(AiRecorder recorder, GameObject owner)
        {
            _recorder = recorder;
            _owner = owner;
        }

        public override void StartBehaviourTreeUpdate(BehaviourTreeRoot root)
        {
            Preconditions.Assert(_rootSnapshot==null, "RootSnapshotNotNull");
            Preconditions.Assert(_snapshotsStack==null, "SnapshotStack not null");
            _rootSnapshot = CreateBaseTaskSnapshot(root.Child);
            _snapshotsStack = new Stack<AiRegistryTaskSnapshot>();
        }

        private AiRegistryTaskSnapshot CreateBaseTaskSnapshot(AITask task)
        {
            var snapshot = new AiRegistryTaskSnapshot()
            {
                TaskWithStatus = new AiRegistryTaskWithStatus()
                {
                    Status = task.TaskStatus,
                    Task = task
                }
            };
            snapshot.Children = task.Children.Select(CreateBaseTaskSnapshot).ToList();
            snapshot.ChildCalls = new List<AiRegistryChildCall>();
            return snapshot;
        }

        public override void OnRunStarted(AITask aiTask)
        {
            if (!_snapshotsStack.Any())
            {
                _snapshotsStack.Push(_rootSnapshot);
            }
            else
            {
                _snapshotsStack.Push(_snapshotsStack.Peek().Children.First(c => c.TaskWithStatus.Task == aiTask));
            }
        }

        public override void OnRunCompleted(AITask aiTask, AIOneRunStatus result)
        {
            _snapshotsStack.Pop();
            if (_snapshotsStack.Any())
            {
                var parent = _snapshotsStack.Peek();
                parent.ChildCalls.Add(new AiRegistryChildCall()
                {
                    CallStatus = result,
                    TaskAfterCall = new AiRegistryTaskWithStatus()
                    {
                        Status = aiTask.TaskStatus,
                        Task = aiTask
                    }
                });
            }
            else
            {
                _recorder.RecordSnapshot(new AiBehaviourTreeFrameSnapshot()
                {
                    Owner = _owner,
                    RootCall = new AiRegistryChildCall()
                    {
                        TaskAfterCall = new AiRegistryTaskWithStatus()
                        {
                            Task = aiTask,
                            Status = aiTask.TaskStatus
                        },
                        CallStatus = result
                    },
                    RootChild = _rootSnapshot
                });
                _snapshotsStack = null;
                _rootSnapshot = null;
            }
        }
    }
}
