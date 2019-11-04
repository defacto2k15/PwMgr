using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Utils
{
    public class UpdaterUntilException
    {
        private bool _exceptionWasThrown = false;

        public void Execute(Action action)
        {
            if (!_exceptionWasThrown)
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    _exceptionWasThrown = true;
                    throw;
                }
            }
        }
    }
}