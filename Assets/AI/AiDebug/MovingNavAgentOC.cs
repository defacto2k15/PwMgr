using UnityEngine;
using UnityEngine.AI;

namespace Assets.AI.AiDebug
{

    public class MovingNavAgentOC : MonoBehaviour
    {
        public GameObject Target;

        public void Start()
        {
            var agent = GetComponent<NavMeshAgent>();
            agent.destination = Target.transform.position;
        }
    }
}
