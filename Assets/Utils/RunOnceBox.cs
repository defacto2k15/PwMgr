﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Utils
{
    public class RunOnceBox
    {
        private bool _wasRun;
        private Action _actionToRunOnce;
        private int _updatesToWait;

        public RunOnceBox(Action actionToRunOnce, int updatesToWait = 0)
        {
            _actionToRunOnce = actionToRunOnce;
            _updatesToWait = updatesToWait;
            _wasRun = false;
        }

        public void Update()
        {
            if (!_wasRun)
            {
                if (_updatesToWait == 0)
                {
                    _wasRun = true;
                    _actionToRunOnce();
                }
                else
                {
                    _updatesToWait--;
                }
            }
        }
    }
}