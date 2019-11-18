using System;

namespace Assets.ETerrain
{
    public class InitialSegmentsGenerationInspector
    {
        private Func<bool> _conditionToCheck;
        private Action _actionToPerformWhenConditionWasMet;
        private bool _actionWasPerformed;

        public InitialSegmentsGenerationInspector( Action actionToPerformWhenConditionWasMet)
        {
            _actionToPerformWhenConditionWasMet = actionToPerformWhenConditionWasMet;
            _actionWasPerformed = false;
        }

        public void Update()
        {
            if (!_actionWasPerformed)
            {
                if (_conditionToCheck())
                {
                    _actionToPerformWhenConditionWasMet();
                    _actionWasPerformed = true;
                }
            }
        }

        public void SetConditionToCheck(Func<bool> conditionToCheck)
        {
            _conditionToCheck = conditionToCheck;
        }
    }
}