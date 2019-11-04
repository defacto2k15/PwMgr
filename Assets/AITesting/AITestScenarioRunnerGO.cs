using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.AI;
using Assets.AI.Bot;
using Assets.AITesting.CoAsserts;
using UnityEngine;

namespace Assets.AITesting
{
    public class AITestScenarioRunnerGO : MonoBehaviour
    {
        public String TestName;

        public void Start()
        {
            //_treeCompletionListener = new BehaviourTreeCompletionListener((status) =>
            //{
            //    TreeCompleted(status);
            //});
            //GetComponent<BehaviourTreeRunnerOC>().TreeCreatedCallback = (root) =>
            //{
            //    root.AddListener(_treeCompletionListener);
            //};
        }

        private void TreeCompleted(AIOneRunStatus status)
        {
            var asserts = GetComponents<AIBotTestingAssert>();
        }
    }

}
