using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.AI.Bot;
using UnityEngine;

namespace Assets.AITesting.CoAsserts
{
    public abstract class AIBotTestingAssert
    {
        private AIBotOC _bot;

        protected AIBotTestingAssert(AIBotOC bot)
        {
            _bot = bot;
        }

        public AIBotOC Bot => _bot;

        public abstract void CheckSuccess();
        public abstract string GetDescription();

        public virtual void Update()
        {
        }
    }
}
