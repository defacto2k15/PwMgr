using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.AI.Stories
{
    public abstract class AIStory : AITask
    {
        private AITask _rootChild;
        protected AIStory() : base(new List<AITask>() {})
        {
            _rootChild = null;
        }

        protected override void InternalEarlyStart()
        {
            if (_rootChild == null)
            {
                _rootChild = InternalBuildStory();
                Children = new List<AITask>() {_rootChild};
                Listeners.ForEach(c => _rootChild.AddListener(c));
                _rootChild.OwningBot = OwningBot;
            }
        }

        protected abstract AITask InternalBuildStory();

        protected override AIOneRunStatus InternalRun()
        {
            return _rootChild.Run();
        }
    }
}
