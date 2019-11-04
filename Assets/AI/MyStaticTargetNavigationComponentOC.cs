using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace Assets.AI
{
    public class MyStaticTargetNavigationComponentOC : MonoBehaviour
    {
        private NavigationOrder _order;
        private NavMeshAgent _agent;
        private bool _pathComputationSucceded = true;

        public void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        public void Initialize(NavigationOrder navigationOrder)
        {
            _order = navigationOrder;
            _pathComputationSucceded = _agent.SetDestination(_order.Target);
            _agent.stoppingDistance = _order.SucceessDistance;
        }

        public MyNavigationState State
        {
            get
            {
                if (!_pathComputationSucceded)
                {
                    return MyNavigationState.Failure;
                }
                if (_agent.pathPending)
                {
                    return MyNavigationState.Moving;
                }
                Preconditions.Assert( _agent.hasPath, "Agent does not have path");
                if (_agent.pathStatus == NavMeshPathStatus.PathInvalid || _agent.pathStatus == NavMeshPathStatus.PathPartial)
                {
                    return MyNavigationState.Failure;
                }
                if (_agent.remainingDistance <= _order.SucceessDistance)
                {
                    return MyNavigationState.Success;
                }
                else
                {
                    return MyNavigationState.Moving;
                }
            }
        }
    }

    public enum MyNavigationState
    {
        Moving, Success, Failure
    }

    public class NavigationOrder
    {
        public Vector3 Target;
        public float SucceessDistance;
    }
}
