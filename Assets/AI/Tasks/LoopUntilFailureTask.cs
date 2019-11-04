using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;

namespace Assets.AI.Tasks
{
    public class LoopUntilFailureTask : AITask
    {
        private AITask _child;

        public LoopUntilFailureTask(AITask child) : base(new List<AITask>() {child})
        {
            _child = child;
        }

        protected override AIOneRunStatus InternalRun()
        {
            var status = _child.Run();
            switch (status)
            {
                case AIOneRunStatus.Running:
                    return AIOneRunStatus.Running;
                case AIOneRunStatus.Succeded:
                    _child.Reset();
                    return AIOneRunStatus.Running;
                default:
                    return AIOneRunStatus.Failed;
            }
        }
    }
}
