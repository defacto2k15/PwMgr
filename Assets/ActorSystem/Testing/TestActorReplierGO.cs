using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ActorSystem.Registry;
using UnityEngine;

namespace Assets.ActorSystem.Testing
{
    public class TestActorReplierGO : MonoBehaviour
    {
        private TestActorReplier _replier;
        
        public void Start()
        {
            _replier = new TestActorReplier(gameObject, FindObjectOfType<AsTelegramRegistryGO>().Registry);
        }

        public void Update()
        {
            _replier.Update();
        }

        public TestActorReplier Replier => _replier;
    }

    [Serializable]
    public class TestActorReplier : AsAbstractActor
    {
        public TestActorReplier(GameObject owner = null, AsTelegramRegistry registry = null) : base(owner, registry)
        {
        }

        protected override AsReciever BuildRecieve(AsRecieverBuilder builder)
        {
            return builder
                .Match<GreetingMessage>(c => SendTelegram(c.Sender, c.GetPayload<GreetingMessage>()))
                .Build();
        }
    }
}
