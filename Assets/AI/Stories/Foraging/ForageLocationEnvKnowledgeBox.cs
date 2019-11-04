using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.AI.Areas;
using Assets.AI.Bot;
using Assets.AI.Bot.Aspects;
using Assets.AI.Environment;
using Assets.Utils;

namespace Assets.AI.Stories.Foraging
{
    public class ForageLocationEnvKnowledgeBox : IEnvironmentKnowledgeBox
    {
        private AIArea _supportedArea;
        private List<EdibleBotAspectOC> _forageItems;

        public ForageLocationEnvKnowledgeBox(AIArea supportedArea, List<EdibleBotAspectOC> forageItems)
        {
            _supportedArea = supportedArea;
            _forageItems = forageItems;
        }

        public bool DescribesArea(AIArea area)
        {
            return _supportedArea.Equals(area); //todo;
        }

        public List<EdibleBotAspectOC> ForageItems => _forageItems;
    }
}
