using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Assets.AI.Areas;
using Assets.AI.Bot;
using Assets.AI.Bot.Navigating;
using Assets.AI.Environment;
using Assets.Utils;
using UnityEngine;

namespace Assets.AI
{
    public abstract class AITask
    {
        private List<AITask> _children;
        private AIBotOC _owningBot;
        private AITask _parentTask;
        private EnvironmentKnowledgeBase _knowledgeBase;

        private AITaskStatus _taskStatus;
        private List<AITaskListener> _listeners;
        private bool _wasStarted = false;

        protected AITask(List<AITask> children)
        {
            _children = children;
            _listeners = new List<AITaskListener>();
            Name = "?";
            _knowledgeBase = new EnvironmentKnowledgeBase();
        }

        public AIOneRunStatus Run()
        {
            if (_taskStatus == AITaskStatus.Succeded || _taskStatus == AITaskStatus.Failed)
            {
                Preconditions.Fail("Task was arleady completed, its state is "+_taskStatus);
                return AIOneRunStatus.Succeded;
            }

            if (!_wasStarted)
            {
                _wasStarted = true;
                InternalStart();
            }
            _listeners.ForEach(c => c.OnRunStarted(this));
            var result = InternalRun();

            if (result == AIOneRunStatus.Failed)
            {
                _taskStatus=AITaskStatus.Failed;
            }
            else if (result == AIOneRunStatus.Succeded)
            {
                _taskStatus = AITaskStatus.Succeded;
            }
            else
            {
                _taskStatus = AITaskStatus.Running;
            }
            _listeners.ForEach(c => c.OnRunCompleted(this, result));
            return result;
        }
        protected abstract AIOneRunStatus InternalRun();

        protected virtual void InternalStart() {
        }

        protected virtual void InternalEarlyStart()
        {
            
        }

        public void EarlyStart()
        {
            InternalEarlyStart();
            Children.ForEach(c => c.EarlyStart());
        }

        public void Reset()
        {
            _taskStatus = AITaskStatus.Fresh;
            _wasStarted = false;
            Debug.Log("RESET TASKA< DODAJ DO UI");
        }

        public List<AITaskListener> Listeners => _listeners;

        public void AddListener(AITaskListener listener)
        {
            _listeners.Add(listener);
            _children.ForEach(c => c.AddListener(listener));
        }

        public AITaskStatus TaskStatus => _taskStatus;

        public List<AITask> Children
        {
            get { return _children; }
            set { _children = value; _children.ForEach(c => c.ParentTask=this); }
        }

        public AIBotOC OwningBot
        {
            get { return _owningBot;}
            set { _owningBot = value;  _children.ForEach(c => c.OwningBot = value);}
        }

        public AITask ParentTask
        {
            get => _parentTask;
            private set => _parentTask = value;
        }

        public T GetKnowledgeBox<T>(AIArea area) where T : IEnvironmentKnowledgeBox
        {
            if (_knowledgeBase.HasKnowledgeBox<T>(area))
            {
                return _knowledgeBase.GetKnowledgeBox<T>(area);
            }
            else
            {
                Preconditions.Assert(_parentTask != null, $"Cannot find knowledge box for area {area} and type {typeof(T)}");
                return _parentTask.GetKnowledgeBox<T>(area);
            }
        }

        public EnvironmentKnowledgeBase KnowledgeBase => _knowledgeBase;

        public string Name;
    }

    public class AITaskListener
    {
        public virtual void StartBehaviourTreeUpdate(BehaviourTreeRoot root)
        {
        }

        public virtual  void OnRunStarted(AITask aiTask)
        {
        }

        public virtual void OnRunCompleted(AITask aiTask, AIOneRunStatus result)
        {
        }

        public virtual void OnBehaviourTreeCompleted(BehaviourTreeRoot root, AIOneRunStatus status)
        {
        }
    }

    public enum AITaskStatus
    {
        Fresh, Running, Failed, Succeded, Cancelled
    }

    public enum AIOneRunStatus
    {
        Running, Failed, Succeded
    }


    public class NavigateToAiTask : AITask
    {
        private MyStaticTargetNavigationComponentOC _navigationComponent;

        public NavigateToAiTask() : base(new List<AITask>())
        {
        }

        protected override AIOneRunStatus InternalRun()
        {
            switch (_navigationComponent.State)
            {
                    case MyNavigationState.Failure: return AIOneRunStatus.Failed;
                    case MyNavigationState.Moving: return AIOneRunStatus.Running;
                    case MyNavigationState.Success: return AIOneRunStatus.Succeded;
                default: Preconditions.Fail("Not supported state "+_navigationComponent.State); return AIOneRunStatus.Failed;
            }
        }

        protected override void InternalStart()
        {
            _navigationComponent = OwningBot.GetComponent<MyStaticTargetNavigationComponentOC>();

            var box = OwningBot.GetKnowledgeBox<NavigationKnowledgeBox>(this);
            _navigationComponent.Initialize(new NavigationOrder()
            {
                SucceessDistance = box.SuccessDistance,
                Target = box.PositionTarget
            });
        }
    }
    //public class NavigateToGameObjectTask : AITask
    //{
    //    private NavigateToAiTask _child;
    //    private readonly MyStaticTargetNavigationComponentOC _navigationComponent;
    //    private GameObject _go;
    //    private float _successDistance;

    //    public NavigateToGameObjectTask(MyStaticTargetNavigationComponentOC navigationComponent, GameObject go, float successDistance) : base(new List<AITask>() )
    //    {
    //        _navigationComponent = navigationComponent;
    //        _navigationComponent = navigationComponent;
    //        _go = go;
    //        _successDistance = successDistance;
    //    }

    //    protected override AIOneRunStatus InternalRun()
    //    {
    //        switch (_navigationComponent.State)
    //        {
    //                case MyNavigationState.Failure: return AIOneRunStatus.Failed;
    //                case MyNavigationState.Moving: return AIOneRunStatus.Running;
    //                case MyNavigationState.Success: return AIOneRunStatus.Succeded;
    //            default: Preconditions.Fail("Not supported state "+_navigationComponent.State); return AIOneRunStatus.Failed;
    //        }
    //    }

    //    protected override void InternalStart()
    //    {
    //        _child = new NavigateToAiTask(_navigationComponent, CreateNavigationOrder()));

    //        _navigationComponent.Initialize(_navigationOrder);
    //    }

    //    private NavigationOrder CreateNavigationOrder()
    //    {
    //        return new NavigationOrder()
    //        {
    //            Target = _go.transform.position,
    //            SucceessDistance = _successDistance
    //        };
    //    }
    //}
}
