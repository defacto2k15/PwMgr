using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Assets.Utils.MT
{
    public class MyPatientLock 
    {
        private object _lockObject = new object();
        private MyLockAccess _poorLockAccess;
        private MyLockAccess _masterLockAccess;

        private int _failedMasterTries = 0;
        private int _maxFailedMasterTries = 4;

        public MyPatientLock()
        {
            _poorLockAccess = new MyLockAccess(this);
            _poorLockAccess.HasLock = false;
            _masterLockAccess = new MyLockAccess(this);
            _masterLockAccess.HasLock = false;
        }

        public MyLockAccess EnterImportant()
        {
            var entered = Monitor.TryEnter(_lockObject);
            if (entered)
            {
                _failedMasterTries = 0;
                _masterLockAccess.HasLock = true;
            }
            else
            {
                _failedMasterTries++;
                if (_failedMasterTries > _maxFailedMasterTries)
                {
                    Monitor.Enter(_lockObject);
                    _failedMasterTries = 0;
                    _masterLockAccess.HasLock = true;
                }
                else
                {
                    _masterLockAccess.HasLock = false;
                }
            }
            return _masterLockAccess;
        }

        public MyLockAccess EnterNotImportant()
        {
            Monitor.Enter(_lockObject);
            while ( _failedMasterTries >= _maxFailedMasterTries)
            {
                Monitor.Wait(_lockObject);
            }
            _poorLockAccess.HasLock = true;
            return _poorLockAccess;
        }

        public void FreeLock()
        {
            if (_masterLockAccess.HasLock)
            {
                _masterLockAccess.HasLock = false;
                Monitor.PulseAll(_lockObject);
            }
            else
            {
                _poorLockAccess.HasLock = false;
            }
            Monitor.Exit(_lockObject);
        }
    }

    public class MyLockAccess : IDisposable
    {
        private MyPatientLock _lock;
        private bool _hasLock;

        public MyLockAccess(MyPatientLock aLock)
        {
            _lock = aLock;
        }

        public void Dispose()
        {
            if (_hasLock)
            {
                _lock.FreeLock();
            }
        }

        public bool HasLock
        {
            get { return _hasLock; }
            set { _hasLock = value; }
        }
    }

}
