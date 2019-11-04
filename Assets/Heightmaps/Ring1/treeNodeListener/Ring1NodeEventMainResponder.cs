using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Utils;
using Assets.Utils.MT;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.treeNodeListener
{
    public class Ring1NodeEventMainResponder
    {
        private Dictionary<int, IAsyncRing1NodeListener> _listeners = new Dictionary<int, IAsyncRing1NodeListener>();

        public async Task ProcessOrderAsync(Queue<Ring1NodeEventMainRespondingOrder> orders)
        {
            foreach (var order in orders)
            {
                var actionsToDoNow = new List<ListenerWithActions>();
                var actionsOfNonExistentNodes = new List<KeyValuePair<int, List<PrioritisedRing1ListenerAction>>>();
                foreach (var pair in order.ListenerActions)
                {
                    if (_listeners.ContainsKey(pair.Key))
                    {
                        var listener = _listeners[pair.Key];
                        actionsToDoNow.Add(new ListenerWithActions()
                        {
                            Actions = pair.Value,
                            Listener = listener
                        });
                    }
                    else
                    {
                        actionsOfNonExistentNodes.Add(pair);
                    }
                }
                await ExecuteActionsAsync(actionsToDoNow);
                actionsToDoNow.Clear();

                foreach (var pair in order.NewListenersGenerator)
                {
                    Preconditions.Assert(!_listeners.ContainsKey(pair.Key),
                        "There arleady is a listener for id " + pair.Key);
                    _listeners[pair.Key] = pair.Value();
                }

                actionsToDoNow.Clear();
                foreach (var pair in actionsOfNonExistentNodes)
                {
                    Preconditions.Assert(_listeners.ContainsKey(pair.Key), "There is no listener for id " + pair.Key);
                    var listener = _listeners[pair.Key];
                    actionsToDoNow.Add(new ListenerWithActions()
                    {
                        Actions = pair.Value,
                        Listener = listener
                    });
                }
                await ExecuteActionsAsync(actionsToDoNow);
            }
        }

        private async Task ExecuteActionsAsync(List<ListenerWithActions> actions)
        {
            var byPriorityLists = actions.SelectMany(c => c.Actions.Select(k => new
                {
                    c.Listener,
                    k
                })).GroupBy(
                    c => c.k.Priority,
                    c => new {listener = c.Listener, action = c.k.Action},
                    (priority, listenerWithAction) => new {priority, listenerWithAction})
                .OrderByDescending(k => (int) k.priority);

            foreach (var listenerWithActionList in byPriorityLists.Select(c => c.listenerWithAction.ToList()).ToList())
            {
                await TaskUtils.WhenAll(listenerWithActionList.Select(c => c.action(c.listener)));
            }
        }

        private class ListenerWithActions
        {
            public List<PrioritisedRing1ListenerAction> Actions;
            public IAsyncRing1NodeListener Listener;
        }
    }

    public class Ring1NodeEventMainRespondingOrder
    {
        public Dictionary<int, Func<IAsyncRing1NodeListener>> NewListenersGenerator =
            new Dictionary<int, Func<IAsyncRing1NodeListener>>();

        public Dictionary<int, List<PrioritisedRing1ListenerAction> >ListenerActions = new Dictionary<int, List<PrioritisedRing1ListenerAction>>();

        public bool Any
        {
            get { return NewListenersGenerator.Any() || ListenerActions.Any(); }
        }

        public void AddAction(int id, PrioritisedRing1ListenerAction action)
        {
            if (!ListenerActions.ContainsKey(id))
            {
                ListenerActions[id] = new List<PrioritisedRing1ListenerAction>();
            }
            ListenerActions[id].Add(action);
        }
    }

    public class PrioritisedRing1ListenerAction
    {
        public Func<IAsyncRing1NodeListener, Task> Action;
        public Ring1ListenersActionPriority Priority;
    }

    public enum Ring1ListenersActionPriority
    {
        Update_Priority=0,
        Hiding_Priority=1
    }
}