using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.AI.Bot.Navigating
{
    public class NavigationKnowledgeBox : IBotKnowledgeBox
    {
        public float SuccessDistance;
        public Vector3 PositionTarget;
    }
}
