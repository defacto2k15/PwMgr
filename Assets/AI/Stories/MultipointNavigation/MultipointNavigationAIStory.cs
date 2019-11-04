using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.AI.Bot;
using Assets.AI.Bot.Navigating;
using UnityEngine;

namespace Assets.AI.Stories.MultipointNavigation
{
    public class MultipointNavigationAIStory : AIStory
    {
        private readonly List<Vector3> _navigationPoints;
        private readonly float _successDistance;

        public MultipointNavigationAIStory(List<Vector3> navigationPoints, float successDistance)
        {
            _navigationPoints = navigationPoints;
            _successDistance = successDistance;
        }

        protected override AITask InternalBuildStory()
        {
            var navigationChildren = new List<AITask>();
            foreach (var point in _navigationPoints)
            {
                var newTask = new NavigateToAiTask();
                OwningBot.AddKnowledgeBox(new NavigationKnowledgeBox()
                {
                    PositionTarget = point,
                    SuccessDistance = _successDistance
                },newTask);
                navigationChildren.Add(newTask);
            }

            return new SequenceTask(navigationChildren);
        }
    }
}
