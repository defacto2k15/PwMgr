using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.AI.Areas;
using Assets.AI.Bot;
using Assets.AI.Bot.Aspects;
using UnityEngine;

namespace Assets.AI.Stories.Foraging
{
    public class ForagingKnowledgeBox : IBotKnowledgeBox
    {
        public AIArea ForagingArea;
        public Vector2 SearchStartPoint;
        public EdibleBotAspectOC SelectedEdibleItem;
    }
}
