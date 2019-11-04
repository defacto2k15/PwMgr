using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ActorSystem.Registry;
using UnityEngine;

namespace Assets.ActorSystem.Testing
{
    public class TestActorGreeterGO : MonoBehaviour
    {
        [SerializeField]
        private TestActorGreeter _greeter;
        private bool _wasInitialized;

        public void Update()
        {
            if (!_wasInitialized)
            {
                _greeter = new TestActorGreeter(
                    GetComponents<TestActorReplierGO>().Select(c => c.Replier).Cast<AsAbstractActor>().ToList(),
                    gameObject,
                    FindObjectOfType<AsTelegramRegistryGO>().Registry);

                _wasInitialized = true;
            }
            else
            {
                _greeter.Update();
            }
        }

        public TestActorGreeter Greeter => _greeter;
    }

    [Serializable]
    public class TestActorGreeter : AsAbstractActor
    {
        [SerializeField]
        private List<AsAbstractActor> _actorsToGreet;

        public TestActorGreeter(List<AsAbstractActor> actorsToGreet, GameObject owner = null, AsTelegramRegistry registry = null) : base(owner, registry)
        {
            _actorsToGreet = actorsToGreet;
        }

        protected override AsReciever BuildRecieve(AsRecieverBuilder builder)
        {
            return builder.Default(c => { Debug.Log("Recieved message: "+c); }).Build();
        }

        protected override void InternalUpdate()
        {
            if (Time.frameCount == 5 || Time.frameCount == 15)
            {
                _actorsToGreet.ForEach(c => SendTelegram(c, new GreetingMessage() { Message = "Hello there at frame "+Time.frameCount}));
            }
        }
    }

    [Serializable]
    public class GreetingMessage
    {
        public string Message;
    }
}
