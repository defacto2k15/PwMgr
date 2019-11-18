using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils.UTUpdating;

namespace Assets.ETerrain
{
    public class TravellerMovementCustodian 
    {
        private List<Func<bool>> _travelLimiters;

        public TravellerMovementCustodian()
        {
            _travelLimiters = new List<Func<bool>>();
        }

        public void AddLimiter(Func<bool> limiter)
        {
            _travelLimiters.Add(limiter);
        }

        public bool IsMovementPossible()
        {
            return _travelLimiters.All(c => c());
        }
    }
}
