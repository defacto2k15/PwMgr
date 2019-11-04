using System;

namespace Assets.Utils
{
    public class RunEveryNthTimeBox
    {
        private Action _actionToRun;
        private readonly int _updatesBetweenCalls;
        private int _updatesThatWeArleadyWaited;

        public RunEveryNthTimeBox(Action actionToRun, int updatesBetweenCalls)
        {
            _actionToRun = actionToRun;
            _updatesBetweenCalls = updatesBetweenCalls;
            _updatesThatWeArleadyWaited = 0;
        }

        public void Update()
        {
            _updatesThatWeArleadyWaited++;
            if (_updatesThatWeArleadyWaited >= _updatesBetweenCalls)
            {
                _actionToRun();
                _updatesThatWeArleadyWaited = 0;
            }
        }
    }
}