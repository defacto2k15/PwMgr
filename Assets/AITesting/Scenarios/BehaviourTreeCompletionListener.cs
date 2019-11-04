using System;
using Assets.AI;

namespace Assets.AITesting.Scenarios
{
    public class BehaviourTreeCompletionListener : AITaskListener
    {
        private Action<AIOneRunStatus> _completedCallback;

        public BehaviourTreeCompletionListener(Action<AIOneRunStatus> completedCallback)
        {
            _completedCallback = completedCallback;
        }

        public override void OnBehaviourTreeCompleted(BehaviourTreeRoot root, AIOneRunStatus status)
        {
            _completedCallback(status);
        }
    }
}