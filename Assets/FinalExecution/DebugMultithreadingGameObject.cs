using System.Threading;
using System.Threading.Tasks;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.FinalExecution
{
    public class DebugMultithreadingGameObject : MonoBehaviour
    {
        public void Start()
        {
            TaskUtils.SetGlobalMultithreading(true);
            var ot = new DebugOtherThread();
            ot.StartThreading();

            ot.PostAction(async () =>
            {
                Debug.Log("R1");
                await Wait3Seconds().Task;
                Debug.Log("R2");
                await Wait3Seconds().Task;
                Debug.Log("R3");
            });

            //ot.PostAction(async () =>
            //{
            //    Debug.Log("L1");
            //    await Wait3Seconds().Task;
            //    Debug.Log("L2");
            //    await Wait3Seconds().Task;
            //    Debug.Log("L3");
            //}, false);
        }

        private TaskCompletionSource<object> Wait3Seconds()
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                System.Threading.Thread.SpinWait(100000000);
                tcs.SetResult(null);
            });
            thread.Start();
            return tcs;
        }
    }

    public class DebugOtherThread : BaseOtherThreadProxy
    {
        public DebugOtherThread() : base("Deb1", false)
        {
        }
    }
}
