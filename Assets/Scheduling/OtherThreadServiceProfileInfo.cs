using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scheduling
{
    public class OtherThreadServiceProfileInfo
    {
        public string Name = "?";
        public bool IsWorking;
        public int NewTaskCount;
        public int BlockedTasksCount;
        public int ContinuingTasksCount;
    }
}
