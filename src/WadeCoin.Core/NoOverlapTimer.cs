using System;
using System.Threading;

namespace WadeCoin.Core
{
    public class NoOverlapTimer
    {
        private long _timerInterval;
        private object _locker = new object();
        private Timer _timer;
        private Action _action;

        public NoOverlapTimer(Action action, TimeSpan interval){
            _action = action;
            _timerInterval = (long)interval.TotalMilliseconds;
        }

        public void Start()
        {
            _timer = new Timer(Callback, null, 0, _timerInterval);
        }
    
        public void Stop()
        {
            _timer.Change(Timeout.Infinite, 0);
        }

        public void Dispose(){
            _timer.Dispose();
        }

        public void Callback(object state)
        {
            var hasLock = false;
    
            try
            {
                Monitor.TryEnter(_locker, ref hasLock);
                if (!hasLock)
                {
                    return;
                }
                _timer.Change(Timeout.Infinite, Timeout.Infinite);

                _action.Invoke();
            }
            finally
            {
                if (hasLock)
                {
                    Monitor.Exit(_locker);
                    _timer.Change(_timerInterval, _timerInterval);
                }
            }
        }
    }
}