using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.AI.Areas;
using Assets.AI.Bot;

namespace Assets.AI.Environment
{
    public class EnvironmentKnowledgeBase
    {
        private List<IEnvironmentKnowledgeBox> _knowledgeBoxes = new List<IEnvironmentKnowledgeBox>();

        public bool HasKnowledgeBox<T>(AIArea area) where T : IEnvironmentKnowledgeBox
        {
            return _knowledgeBoxes.Where(c => c is T).Cast<T>().Any(c => c.DescribesArea(area));

        }
        public T GetKnowledgeBox<T>(AIArea area) where T : IEnvironmentKnowledgeBox
        {
            return _knowledgeBoxes.Where(c => c is T).Cast<T>().First(c => c.DescribesArea(area));
        }

        public void AddKnowledgeBox(IEnvironmentKnowledgeBox box)
        {
            _knowledgeBoxes.Add(box);
        }
    }

    public interface IEnvironmentKnowledgeBox
    {
        bool DescribesArea(AIArea area);
    }
}
