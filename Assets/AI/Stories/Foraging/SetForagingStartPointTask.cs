using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.AI.Areas;
using Assets.AI.Bot;
using Assets.Random;
using UnityEngine;

namespace Assets.AI.Stories.Foraging
{
    public class SetForagingStartPointTask : AITask
    {
        public SetForagingStartPointTask( AIBotOC owningBot) : base(new List<AITask>())
        {
        }

        protected override AIOneRunStatus InternalRun()
        {
            var random = new System.Random(213); //todo

            var foragingKnowledgeBox = OwningBot.GetKnowledgeBox<ForagingKnowledgeBox>();
            var foragingArea = foragingKnowledgeBox.ForagingArea;
            foragingKnowledgeBox.SearchStartPoint = foragingArea.RandomPointInRectalngle(random);

            return AIOneRunStatus.Succeded;
        }
    }
}
