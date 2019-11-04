using System.Collections.Generic;
using System.Linq;
using Assets.Utils;

namespace Assets.AI
{
    public class SequenceTask : AITask
    {
        private int _currentChildIndex;
        public SequenceTask(List<AITask> children) : base(children)
        {
            Preconditions.Assert(children.Any(), "There are nop children in this sequence");
            _currentChildIndex = 0;
        }

        protected override AIOneRunStatus InternalRun()
        {
            var child = Children[_currentChildIndex];
            var result = child.Run();
            switch (result)
            {
                case AIOneRunStatus.Running:
                    return AIOneRunStatus.Running;
                case AIOneRunStatus.Succeded:
                    if (_currentChildIndex < Children.Count - 1)
                    {
                        _currentChildIndex++;
                        return AIOneRunStatus.Running;
                    }
                    else
                    {
                        return AIOneRunStatus.Succeded;
                    }
            }
            return AIOneRunStatus.Failed;
        }
    }
}