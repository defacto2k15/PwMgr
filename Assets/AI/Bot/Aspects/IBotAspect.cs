using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ActorSystem;

namespace Assets.AI.Bot
{
    public interface IBotAspect
    {
        bool SupportsMessage(Type messageType);
        void ReactToTelegram(AsTelegram telegram);
    }
}
