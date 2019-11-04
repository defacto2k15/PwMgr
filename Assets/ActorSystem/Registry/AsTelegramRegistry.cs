using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using UnityEngine;

namespace Assets.ActorSystem.Registry
{
    public class AsTelegramRegistry
    {
        private readonly bool _shouldRegister;

        private Dictionary<int, AsActorSystemFrameSnapshot> _snapshots;

        public AsTelegramRegistry(bool shouldRegister)
        {
            _shouldRegister = shouldRegister;
            _snapshots = new Dictionary<int, AsActorSystemFrameSnapshot>();
        }

        public void RegisterInitiatedMessage(AsAbstractActor actor, AsTelegram telegram)
        {
            if (!_shouldRegister) return;
            GetThisFrameSnapshot().RegisterInitiatedMessage(actor, telegram);
        }

        public void RegisterRecievedMessage(AsAbstractActor actor, AsTelegram recieved)
        {
            if (!_shouldRegister) return;
            GetThisFrameSnapshot().RegisterRecievedMessage(actor, recieved);
        }

        public void RegisterReplyMessage(AsAbstractActor actor, AsTelegram recieved, AsTelegram sent)
        {
            if (!_shouldRegister) return;
            GetThisFrameSnapshot().RegisterReplyMessage(actor, recieved, sent);
        }

        private AsActorSystemFrameSnapshot GetThisFrameSnapshot()
        {
            if (!_snapshots.ContainsKey(Time.frameCount))
            {
                _snapshots[Time.frameCount] = new AsActorSystemFrameSnapshot();
            }
            return _snapshots[Time.frameCount];
        }

        public Dictionary<int, AsActorSystemFrameSnapshot> Snapshots => _snapshots;

        public bool ContainsSnapshot(int frame)
        {
            return _snapshots.ContainsKey(frame);
        }

        public AsActorSystemFrameSnapshot GetSnapshotFromFrame(int frame)
        {
            return _snapshots[frame];
        }

        public void DebugSetDict(Dictionary<int, AsActorSystemFrameSnapshot> snapshots)
        {
            _snapshots = snapshots;
        }

    }

    public class AsActorSystemFrameSnapshot
    {
        private Dictionary<AsAbstractActor, AsActorFrameSnapshot> _actorFrameSnapshots;

        public AsActorSystemFrameSnapshot()
        {
            _actorFrameSnapshots = new Dictionary<AsAbstractActor, AsActorFrameSnapshot>();
        }

        public void RegisterInitiatedMessage(AsAbstractActor actor, AsTelegram telegram)
        {
            GetFrameSnapshot(actor).RegisterInitiatedMessage(telegram);
        }

        private AsActorFrameSnapshot GetFrameSnapshot(AsAbstractActor actor)
        {
            if (!_actorFrameSnapshots.ContainsKey(actor))
            {
                _actorFrameSnapshots[actor] = new AsActorFrameSnapshot();
            }
            return _actorFrameSnapshots[actor];
        }

        public void RegisterReplyMessage(AsAbstractActor actor, AsTelegram recieved, AsTelegram sent)
        {
            GetFrameSnapshot(actor).RegisterReplyMessage(recieved, sent);
        }

        public Dictionary<AsAbstractActor, AsActorFrameSnapshot> ActorFrameSnapshots => _actorFrameSnapshots;

        public void RegisterRecievedMessage(AsAbstractActor actor, AsTelegram recieved)
        {
            GetFrameSnapshot(actor).RegisterRecievedMessage(recieved);
        }
    }

    public class AsActorFrameSnapshot
    {
        private Dictionary<AsTelegram, List<AsTelegram>> _replies;
        private List<AsTelegram> _initiated;

        public AsActorFrameSnapshot()
        {
            _replies = new Dictionary<AsTelegram, List<AsTelegram>>();
            _initiated = new List<AsTelegram>();
        }

        public void RegisterInitiatedMessage(AsTelegram telegram)
        {
            _initiated.Add(telegram);
        }

        public void RegisterReplyMessage(AsTelegram recieved, AsTelegram sent)
        {
            _replies[recieved].Add(sent);
        }

        public Dictionary<AsTelegram, List<AsTelegram>> Replies => _replies;
        public List<AsTelegram> Initiated => _initiated;

        public void RegisterRecievedMessage(AsTelegram recieved)
        {
            Preconditions.Assert(!_replies.ContainsKey(recieved), "There arleady is registered recieved message "+recieved);
            _replies[recieved] = new List<AsTelegram>();
        }
    }

    public class AsReplyInscription
    {
        public AsTelegram Recieved;
        public List<AsTelegram> Sent;
    }

    public class AsInitiatedInscription
    {
        public AsTelegram Sent;
    }
}
