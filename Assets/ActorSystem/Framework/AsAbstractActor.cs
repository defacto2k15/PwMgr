using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.ActorSystem.Registry;
using Assets.Utils;
using TMPro;
using UnityEngine;

namespace Assets.ActorSystem
{
    [Serializable]
    public abstract class AsAbstractActor
    {
        private Queue<AsTelegram> _queue;
        private int _lastMessageIndex;
        private GameObject _owner;
        private AsTelegramRegistry _registry;

        private AsReciever _reciever;

        private AsTelegram _messageBeingRepliedTo;

        protected AsAbstractActor(GameObject owner=null, AsTelegramRegistry registry=null)
        {
            _owner = owner;
            _registry = registry;

            _queue = new Queue<AsTelegram>();
            _lastMessageIndex = 0;
        }

        protected abstract AsReciever BuildRecieve(AsRecieverBuilder builder);
        protected virtual void InternalUpdate() { }

        public void Update()
        {
            if (_reciever == null)
            {
                _reciever = BuildRecieve(new AsRecieverBuilder());
            }
            var thisFrameIndex = Time.frameCount;
            var messagesToProcess = _queue.Where(c => c.Timestamp.FrameNo < thisFrameIndex).ToList();
            _queue = new Queue<AsTelegram>(_queue.Where(c => c.Timestamp.FrameNo >= thisFrameIndex).ToList());

            messagesToProcess.ForEach(c =>
            {
                 _registry?.RegisterRecievedMessage(this, c);
                _messageBeingRepliedTo = c;
                _reciever.Process(c);
            });

            _messageBeingRepliedTo = null;
            InternalUpdate();
        }

        protected void SendTelegram(AsAbstractActor destinationActor, object payload)
        {
            var telegram = new AsTelegram()
            {
                Payload =  payload,
                Sender = this,
                Reciever = destinationActor,
                Timestamp = new AsTelegramTimestamp()
                {
                    FrameNo = Time.frameCount,
                    MessageIndex = _lastMessageIndex++
                }
            };
            destinationActor.RecieveTelegram(telegram);
            if (_messageBeingRepliedTo == null)
            {
                _registry?.RegisterInitiatedMessage(this, telegram);
            }
            else
            {
                _registry?.RegisterReplyMessage(this, _messageBeingRepliedTo, telegram);
            }
        }

        private void RecieveTelegram(AsTelegram telegram)
        {
            _queue.Enqueue(telegram);
        }

        public GameObject Owner => _owner;
    }

    public class AsRecieverBuilder
    {
        private List<AsMatchActionPair> _pairs;
        private Action<AsTelegram> _default;

        public AsRecieverBuilder()
        {
            _pairs = new List<AsMatchActionPair>();
        }

        public AsRecieverBuilder Match<T>(Action<AsTelegram> action)
        {
            _pairs.Add(new AsMatchActionPair(new AsClassMatcher<T>(), action));
            return this;
        }

        public AsRecieverBuilder Default(Action<AsTelegram> action)
        {
            Preconditions.Assert(_default==null, "There arleady is default action");
            _default = action;
            return this;
        }

        public AsReciever Build()
        {
            var pairs = _pairs.ToList();
            pairs.Add(new AsMatchActionPair(new AllwaysTrueMatcher(), _default));
            return new AsReciever(pairs);
        }
    }

    public class AsReciever
    {
        private List<AsMatchActionPair> _pairs;

        public AsReciever(List<AsMatchActionPair> pairs)
        {
            _pairs = pairs;
        }

        public void Process(AsTelegram telegram)
        {
            foreach (var pair in _pairs)
            {
                if (pair.DoesMatch(telegram))
                {
                    pair.Action(telegram);
                    return;
                }
            }
            Preconditions.Fail("Cannot match telegram "+telegram);
        }
    }

    public class AsMatchActionPair
    {
        private AsMatcher _matcher;
        private Action<AsTelegram> _action;

        public AsMatchActionPair(AsMatcher matcher, Action<AsTelegram> action)
        {
            _matcher = matcher;
            _action = action;
        }

        public bool DoesMatch(AsTelegram telegram)
        {
            return _matcher.DoesMatch(telegram);
        }

        public Action<AsTelegram> Action => _action;
    }

    public abstract class AsMatcher
    {
        public abstract bool DoesMatch(AsTelegram telegram);
    }

    public class AsClassMatcher<T> : AsMatcher
    {
        public override bool DoesMatch(AsTelegram telegram)
        {
            return telegram.CanCastPayload<T>();
        }
    }

    public class AllwaysTrueMatcher : AsMatcher
    {
        public override bool DoesMatch(AsTelegram telegram)
        {
            return true;
        }
    }

    [Serializable]
    public class AsTelegram
    {
        public AsAbstractActor Sender;
        public AsAbstractActor Reciever;
        public AsTelegramTimestamp Timestamp;
        public object Payload;

        public bool CanCastPayload<T>()
        {
            return (Payload is T);
        }

        public T GetPayload<T>()
        {
            Preconditions.Assert(CanCastPayload<T>(), "Cannot cast message to "+typeof(T));
            return (T)Payload;
        }
    }

    [Serializable]
    public class AsTelegramTimestamp
    {
        public int FrameNo;
        public int MessageIndex;
    }
}
