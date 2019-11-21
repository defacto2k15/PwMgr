using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.ETerrain
{
    public class TravellerMovementCustodian 
    {
        private readonly GameObject _traveller;
        private List<Func<MovementBlockingProcess>> _travelLimiters;
        private Vector3? _lastFramePositon;
        private List<MovementBlockingProcess> _thisFrameBlockingProcesses;

        public TravellerMovementCustodian(GameObject traveller)
        {
            _traveller = traveller;
            _travelLimiters = new List<Func<MovementBlockingProcess>>();
        }

        public void AddLimiter(Func<MovementBlockingProcess> limiter)
        {
            _travelLimiters.Add(limiter);
        }

        public bool IsMovementPossible()
        {
            return _thisFrameBlockingProcesses.Count == 0;
        }

        public void Update()
        {
            _thisFrameBlockingProcesses = _travelLimiters.Select(c => c()).Where(c => c.BlockCount != 0).ToList();
            if (!_lastFramePositon.HasValue)
            {
                _lastFramePositon = _traveller.transform.position;
            }
            else
            {
                if (IsMovementPossible())
                {
                    _lastFramePositon = _traveller.transform.position;
                }
                else
                {
                    _traveller.transform.position = _lastFramePositon.Value;
                }
            }
        }

        public List<MovementBlockingProcess> ThisFrameBlockingProcesses => _thisFrameBlockingProcesses;
    }

    public class MovementBlockingProcess
    {
        public string ProcessName;
        public int BlockCount;
    }
}
