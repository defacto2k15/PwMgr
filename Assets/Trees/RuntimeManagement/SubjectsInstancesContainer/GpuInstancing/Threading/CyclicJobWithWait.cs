using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Assets.Trees.RuntimeManagement.SubjectsInstancesContainer.GpuInstancing.Threading
{
    public class CyclicJobWithWait
    {
        public Action PerCycleAction;
        public AutoResetEvent CanExecuteEvent;

        public CyclicJobWithWait(Action perCycleAction, AutoResetEvent canExecuteEvent)
        {
            PerCycleAction = perCycleAction;
            CanExecuteEvent = canExecuteEvent;
        }
    }
}