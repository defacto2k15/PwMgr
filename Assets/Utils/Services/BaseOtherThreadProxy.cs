using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Utils.Services
{
    public interface IOtherThreadProxy
    {
        void StartThreading(Action perEveryPostAction);
        void ExecuteAction(Func<Task> actionToPreform);
    }
}