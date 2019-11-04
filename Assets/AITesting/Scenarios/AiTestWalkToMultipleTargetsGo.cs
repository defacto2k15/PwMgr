using System.Collections.Generic;
using System.Linq;
using Assets.AI;
using Assets.AI.Bot;
using Assets.AI.Stories.MultipointNavigation;
using Assets.AITesting.CoAsserts;
using UnityEngine;

namespace Assets.AITesting.Scenarios
{
    public class AiTestWalkToMultipleTargetsGo : AiTestScenario
    {
        public AIBotOC Bot;
        public List<GameObject> NavigationTargets;
        private float _successDistance = 2f;

        protected override void DefineTest()
        {
            AITestingUtils.RebuildNavSurfacesInScene();

            var multipointNavigationStory = new MultipointNavigationAIStory(NavigationTargets.Select(c => c.transform.position).ToList(), _successDistance);
            Bot.GetComponent<BehaviourTreeRunnerOC>().Root = new BehaviourTreeRoot(multipointNavigationStory);

            NavigationTargets.ForEach(c => AddAssertAtEndOfTree( new EndsNearObjectAssert(Bot, c, _successDistance)));
            FinalizeStart();
        }
    }
}