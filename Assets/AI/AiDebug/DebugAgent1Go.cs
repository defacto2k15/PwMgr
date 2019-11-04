using System;
using System.Collections.Generic;
using System.Linq;
using Assets.AI.Bot;
using Assets.AI.Bot.Navigating;
using Assets.AI.Stories.MultipointNavigation;
using Assets.Utils;
using UnityEngine;

namespace Assets.AI.AiDebug
{
    public class DebugAgent1Go : MonoBehaviour
    {
        public List<GameObject> NavigationTargetsList;
        public int TestIndex;
        private RunOnceBox _runOnce;
        private float _successDistance = 2f;

        public void Start()
        {
            //_runOnce = new RunOnceBox(() =>
            //{
            //    var runner = GetComponent<BehaviourTreeRunnerOC>();
            //    var bot = GetComponent<AIBotOC>();

            //    if (TestIndex == 0)
            //    {
            //        var navigateToAiTask = new NavigateToAiTask();
            //        bot.AddKnowledgeBox(new NavigationKnowledgeBox()
            //        {
            //            SuccessDistance = _successDistance,
            //            PositionTarget = NavigationTargetsList[0].transform.position
            //        }, navigateToAiTask );
            //        runner.Root = new BehaviourTreeRoot(navigateToAiTask, bot);
            //    }
            //    else if (TestIndex == 1)
            //    {
            //        var story = new MultipointNavigationAIStory(NavigationTargetsList.Select(c => c.transform.position).ToList(), _successDistance);
            //        runner.Root = new BehaviourTreeRoot(story,bot);
            //    }
            //});
        }

        public void Update()
        {
            _runOnce.Update();
        }

        private  T GetAndInitializeComponent<T>( Action<T> initializer)
        {
            var component = GetComponent<T>();
            initializer(component);
            return component;
        }
    }
}
