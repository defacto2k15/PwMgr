using System.Text;
using System.Threading.Tasks;
using Assets.AI;
using Assets.AI.Bot;
using Assets.AI.Bot.Navigating;
using Assets.AITesting.CoAsserts;
using UnityEngine;

namespace Assets.AITesting.Scenarios
{
    public class AITestWalkToTargetGO : AiTestScenario
    {
        public AIBotOC Bot;
        public GameObject NavigationTarget;
        private float _successDistance = 2f;

        protected override void DefineTest()
        {
            AITestingUtils.RebuildNavSurfacesInScene();

            var navigateToAiTask = new NavigateToAiTask();
            Bot.AddKnowledgeBox(new NavigationKnowledgeBox()
            {
                SuccessDistance = _successDistance,
                PositionTarget = NavigationTarget.transform.position
            }, navigateToAiTask);
            Bot.GetComponent<BehaviourTreeRunnerOC>().Root = new BehaviourTreeRoot(navigateToAiTask);

            AddAssertAtEndOfTree( new EndsNearObjectAssert(Bot, NavigationTarget, _successDistance));
            FinalizeStart();
        }
    }
}
