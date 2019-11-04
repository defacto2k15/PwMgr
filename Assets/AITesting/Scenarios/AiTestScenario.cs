using System.Collections.Generic;
using System.Linq;
using Assets.AI;
using Assets.AI.Bot;
using Assets.AITesting.CoAsserts;
using Assets.Utils;
using UnityEngine;

namespace Assets.AITesting.Scenarios
{
    public abstract class AiTestScenario : MonoBehaviour
    {
        private Dictionary<AIBotOC, List<AIBotTestingAssert>> _perBotAsserts = new Dictionary<AIBotOC, List<AIBotTestingAssert>>();
        private RunOnceBox _defineTestRunOnce;
        private int _botsToWaitForCompletion;

        public void Start()
        {
            _defineTestRunOnce = new RunOnceBox(DefineTest);
            _botsToWaitForCompletion = 0;
        }


        public void Update()
        {
            _defineTestRunOnce.Update();
            foreach (var assert in _perBotAsserts.Values.ToList().SelectMany(c => c))
            {
                assert.Update();
            }
        }

        protected abstract void DefineTest();

        protected void AddAssertAtEndOfTree( AIBotTestingAssert assert)
        {
            var bot = assert.Bot;
            if (!_perBotAsserts.ContainsKey(bot))
            {
                _perBotAsserts[bot] = new List<AIBotTestingAssert>();
            }
        }

        public void FinalizeStart()
        {
            foreach (var pair in _perBotAsserts)
            {
                _botsToWaitForCompletion++;
                pair.Key.GetComponent<BehaviourTreeRunnerOC>().TreeCreatedCallback = root => root.AddListener(new BehaviourTreeCompletionListener((status) =>
                {
                    CheckAssertions(status, pair.Value);
                    _botsToWaitForCompletion--;
                    if (_botsToWaitForCompletion == 0)
                    {
                        EndTest();
                    }
                }));
            }
        }

        private void CheckAssertions(AIOneRunStatus status, List<AIBotTestingAssert> asserts)
        {
            foreach (var assert in asserts)
            {
                try
                {
                    assert.CheckSuccess();
                }
                catch (MyAssertException e)
                {
                    Debug.LogError($"Assert '{assert.GetDescription()}' for bot '{assert.Bot.name}' failed with message '{e.Message}'");
                }
            }
        }

        private void EndTest()
        {
            Debug.Log("Test ended");
        }
    }
}