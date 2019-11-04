using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ActorSystem;
using UnityEngine;

namespace Assets.AI.Bot.Aspects
{
    public class PickableBotAspectOC : MonoBehaviour, IBotAspect
    {
        private AIBotOC _parentBot;

        public void Start()
        {
            _parentBot = GetComponent<AIBotOC>();
        }

        public bool SupportsMessage(Type messageType)
        {
            throw new NotImplementedException();
            return false;
        }

        public void ReactToTelegram(AsTelegram telegram)
        {
            throw new NotImplementedException();
        }
    }
}
