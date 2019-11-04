using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils.Services;

namespace Assets.Utils.MT
{
    public class OtherThreadExecutingLocation
    {
        private BaseOtherThreadProxy _executingTarget;

        public void SetExecutingTarget(BaseOtherThreadProxy proxy)
        {
            _executingTarget = proxy;
        }

        public void Execute(Func<Task> action)
        {
            Preconditions.Assert(_executingTarget != null, "Executing target was not set");
            _executingTarget.ExecuteAction(action);
        }
    }
}