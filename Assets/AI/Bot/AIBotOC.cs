using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.AI.Bot
{
    public class AIBotOC : MonoBehaviour
    {
        private BotKnowledgeBase _knowledgeBase;

        public void Start()
        {
            _knowledgeBase = new BotKnowledgeBase();
        }

        public T GetKnowledgeBox<T>(AITask recievingTask = null) where T : IBotKnowledgeBox
        {
            if (recievingTask == null)
            {
                return _knowledgeBase.GetKnowledgeBox<T>();
            }
            else
            {
                return _knowledgeBase.GetKnowledgeBox<T>(recievingTask);
            }
        }

        public void AddKnowledgeBox(IBotKnowledgeBox  box, AITask recievingTask = null)
        {
            if (recievingTask == null)
            {
                _knowledgeBase.AddKnowledgeBox(box);
            }
            else
            {
                _knowledgeBase.AddKnowledgeBox(box, recievingTask);
            }
        }
    }
}
