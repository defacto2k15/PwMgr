using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ActorSystem.Testing;
using UnityEngine;

namespace Assets.ActorSystem.Registry
{
    [ExecuteInEditMode]
    public class AsTelegramRegistryGO : MonoBehaviour
    {
        public bool ShouldRegister;
        public int MaximumFrame = 1000;
        private AsTelegramRegistry _registry;

        public AsTelegramRegistryGO()
        {
            _registry = new AsTelegramRegistry(ShouldRegister);
        }

        public void DebugFillRegistry()
        {
             Debug.Log("Start");

            var greeterGo = FindObjectOfType<TestActorGreeterGO>();
            var greeter = greeterGo.Greeter;

            var repliersGo = FindObjectsOfType<TestActorReplierGO>();
            var repliers = repliersGo.Select(c => c.Replier).ToList();

            Dictionary<int, AsActorSystemFrameSnapshot> snapshots = new Dictionary<int, AsActorSystemFrameSnapshot>();

            snapshots[1] = new AsActorSystemFrameSnapshot();
            var tel0 = new AsTelegram()
            {
                Payload = new GreetingMessage() {  Message = "Hello0"},
                Sender = greeter,
                Reciever = repliers[0],
                Timestamp = new AsTelegramTimestamp() {  FrameNo = 1, MessageIndex = 0}
            };
            snapshots[1].RegisterInitiatedMessage(greeter, tel0);

            var tel1 = new AsTelegram()
            {
                Payload = new GreetingMessage() {  Message = "Hello1"},
                Sender = greeter,
                Reciever = repliers[0],
                Timestamp = new AsTelegramTimestamp() {  FrameNo = 1, MessageIndex = 1}
            };
            snapshots[1].RegisterInitiatedMessage(greeter, tel1);

            var tel2 = new AsTelegram()
            {
                Payload = new GreetingMessage() {  Message = "Hello2"},
                Sender = greeter,
                Reciever = repliers[0],
                Timestamp = new AsTelegramTimestamp() {  FrameNo = 1, MessageIndex = 2}
            };
            snapshots[1].RegisterInitiatedMessage(greeter, tel2);

            ////////////////////////////////////////////////////
            snapshots[2] = new AsActorSystemFrameSnapshot();

            snapshots[2].RegisterRecievedMessage(repliers[0], tel0);
            var tel0_0 = new AsTelegram()
            {
                Payload = "Reply0",
                Sender = repliers[0],
                Reciever = greeter,
                Timestamp = new AsTelegramTimestamp() {  FrameNo = 2, MessageIndex = 0}
            };
            snapshots[2].RegisterReplyMessage(repliers[0], tel0, tel0_0);

            snapshots[2].RegisterRecievedMessage(repliers[1], tel1);
            var tel1_0 = new AsTelegram()
            {
                Payload = "Reply1",
                Sender = repliers[1],
                Reciever = greeter,
                Timestamp = new AsTelegramTimestamp() {  FrameNo = 2, MessageIndex = 0}
            };
            snapshots[2].RegisterReplyMessage(repliers[1], tel1, tel1_0);

            snapshots[2].RegisterRecievedMessage(repliers[2], tel2);
            var tel2_0 = new AsTelegram()
            {
                Payload = "Reply2",
                Sender = repliers[2],
                Reciever = greeter,
                Timestamp = new AsTelegramTimestamp() {  FrameNo = 2, MessageIndex = 0}
            };
            snapshots[2].RegisterReplyMessage(repliers[2], tel2, tel2_0);
            ////////////////////////////////////////////////////////

            snapshots[3] = new AsActorSystemFrameSnapshot();
            snapshots[3].RegisterRecievedMessage(greeter, tel0_0);
            snapshots[3].RegisterRecievedMessage(greeter, tel1_0);
            snapshots[3].RegisterRecievedMessage(greeter, tel2_0);

            _registry.DebugSetDict(snapshots);           
        }

        public AsTelegramRegistry Registry => _registry;
    }
}
