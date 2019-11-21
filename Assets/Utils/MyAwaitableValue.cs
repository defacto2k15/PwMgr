using System.Threading.Tasks;
using Assets.Utils.MT;

namespace Assets.Utils
{
    public class MyAwaitableValue<T> where T : class
    {
        private T _value = null;
        private TcsSemaphore _semaphore = new TcsSemaphore();
        private object _lock = new object();

        public async Task<T> RetriveValue()
        {
            await _semaphore.Await();
            lock (_lock)
            {
                _semaphore = new TcsSemaphore(); //TODO use real reset semaphore
                return _value;
            }
        }

        public void SetValue(T value)
        {
            lock (_lock)
            {
                _value = value;
                if (!_semaphore.SemaphoreIsSet())
                {
                    _semaphore.Set();
                }
            }
        }
    }
}