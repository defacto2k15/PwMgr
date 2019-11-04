using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;

namespace Assets.AI.Bot
{
    public class BotKnowledgeBase
    {
        private List<IBotKnowledgeBox> _knowledgeBoxes = new List<IBotKnowledgeBox>();
        private Dictionary<AITask, BotKnowledgeBase> _perTaskKnowledgeBases = new Dictionary<AITask, BotKnowledgeBase>();

        public T GetKnowledgeBox<T>() where T : IBotKnowledgeBox
        {
            return (T) _knowledgeBoxes.First(c => c is T);
        }

        public T GetKnowledgeBox<T>(AITask recievingTask) where T : IBotKnowledgeBox
        {
            Preconditions.Assert(_perTaskKnowledgeBases.ContainsKey(recievingTask), "There are no knowledge for task "+recievingTask);
            return _perTaskKnowledgeBases[recievingTask].GetKnowledgeBox<T>();
        }


        public void AddKnowledgeBox(IBotKnowledgeBox box)
        {
            Preconditions.Assert(!_knowledgeBoxes.Any(c => box.GetType().IsInstanceOfType(c) ), "There arleady is knowledge box of type "+box.GetType());
            _knowledgeBoxes.Add(box);
        }

        public void AddKnowledgeBox(IBotKnowledgeBox box, AITask recievingTask)
        {
            if (!_perTaskKnowledgeBases.ContainsKey(recievingTask))
            {
                _perTaskKnowledgeBases[recievingTask] = new BotKnowledgeBase();
            }
            _perTaskKnowledgeBases[recievingTask].AddKnowledgeBox(box);
        }

        public void RemoveKnowledgeBox<T>() where T : IBotKnowledgeBox
        {
            Preconditions.Assert(_knowledgeBoxes.Any(c => c is T ), "There is no knowledge box of type "+typeof(T));
            _knowledgeBoxes = _knowledgeBoxes.Where(c => !(c is T)).ToList();
        }

        public void RemoveKnowledgeBox<T>(AITask recievingTask) where T : IBotKnowledgeBox
        {
            Preconditions.Assert(_perTaskKnowledgeBases.ContainsKey(recievingTask), "There are no knowledge for task "+recievingTask);
            _perTaskKnowledgeBases[recievingTask].RemoveKnowledgeBox<T>();
        }
    }

    public interface IBotKnowledgeBox
    {
    }
}
