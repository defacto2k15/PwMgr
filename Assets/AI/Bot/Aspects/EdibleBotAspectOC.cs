using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ActorSystem;

namespace Assets.AI.Bot.Aspects
{
    public class EdibleBotAspectOC : IBotAspect
    {
        public bool SupportsMessage(Type messageType)
        {
            throw new NotImplementedException();
        }

        public void ReactToTelegram(AsTelegram telegram)
        {
            throw new NotImplementedException();
        }
    }
}
