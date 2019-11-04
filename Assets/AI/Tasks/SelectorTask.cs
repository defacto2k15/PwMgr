using System.Collections.Generic;
using System.Linq;
using Assets.Utils;

namespace Assets.AI
{
    public class SelectorTask : AITask
    {
        private int _currentChildIndex;
        public SelectorTask(List<AITask> children) : base(children)
        {
            Preconditions.Assert(children.Any(), "There are nop children in this selector");
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
                    return AIOneRunStatus.Succeded;
                case AIOneRunStatus.Failed:
                    if (_currentChildIndex < Children.Count - 1)
                    {
                        _currentChildIndex++;
                        return AIOneRunStatus.Running;
                    }
                    else
                    {
                        return AIOneRunStatus.Failed;
                    }
            }
            return AIOneRunStatus.Failed;
        }
    }
}