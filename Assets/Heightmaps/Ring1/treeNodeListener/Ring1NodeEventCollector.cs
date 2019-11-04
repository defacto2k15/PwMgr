using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.GRing;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.treeNodeListener
{
    public class Ring1NodeEventCollector : IRing1NodeListener
    {
        private readonly INewQuadListenersCreator _newListenersCreator;

        private readonly Dictionary<MyRectangle, int> _ring1NodePositionsToId =
            new Dictionary<MyRectangle, int>();

        private Ring1NodeEventMainRespondingOrder _currentOrder = new Ring1NodeEventMainRespondingOrder();
        private Queue<Ring1NodeEventMainRespondingOrder> _ordersQueue = new Queue<Ring1NodeEventMainRespondingOrder>();

        private int _lastId = 0;

        public Ring1NodeEventCollector(INewQuadListenersCreator newListenersCreator)
        {
            _newListenersCreator = newListenersCreator;
        }

        public bool Any
        {
            get { return _ordersQueue.Any(); }
        }

        public void CreatedNewNode(Ring1Node ring1Node)
        {
            Preconditions.Assert(!_ring1NodePositionsToId.ContainsKey(ring1Node.Ring1Position),
                "Terrain was not created for this node");
            var newId = _lastId++;
            _ring1NodePositionsToId[ring1Node.Ring1Position] = newId;
                _currentOrder.NewListenersGenerator[newId] = () => _newListenersCreator.CreateNewListener(ring1Node);
        }

        public void DoNotDisplay(Ring1Node ring1Node)
        {
            Preconditions.Assert(_ring1NodePositionsToId.ContainsKey(ring1Node.Ring1Position),
                "There is no node of given position");
            var id = _ring1NodePositionsToId[ring1Node.Ring1Position];
            _currentOrder.AddAction(id, new PrioritisedRing1ListenerAction()
            {
                Action = (node) => node.DoNotDisplayAsync(),
                Priority = Ring1ListenersActionPriority.Hiding_Priority
            });
        }

        public void Update(Ring1Node ring1Node, Vector3 cameraPosition)
        {
            Preconditions.Assert(_ring1NodePositionsToId.ContainsKey(ring1Node.Ring1Position),
                "There is no node of given position");
            var id = _ring1NodePositionsToId[ring1Node.Ring1Position];
                _currentOrder.AddAction(id, new PrioritisedRing1ListenerAction()
            {
                Action = (node) => node.UpdateAsync(cameraPosition),
                Priority = Ring1ListenersActionPriority.Update_Priority
            });
        }

        public void EndBatch()
        {
            if (_currentOrder.Any)
            {
                lock (_ordersQueue)
                {
                    _ordersQueue.Enqueue(_currentOrder);
                    _currentOrder = new Ring1NodeEventMainRespondingOrder();
                }
            }
        }

        public Queue<Ring1NodeEventMainRespondingOrder> RetriveOrderAndClear()
        {
            Queue<Ring1NodeEventMainRespondingOrder> toReturn = null;
            lock (_ordersQueue)
            {
                toReturn = _ordersQueue;
                _ordersQueue = new Queue<Ring1NodeEventMainRespondingOrder>();
                return toReturn;
            }
        }


    }

    public interface INewQuadListenersCreator
    {
        IAsyncRing1NodeListener CreateNewListener(Ring1Node node);
    }

    public class FromLambdaListenersCreator : INewQuadListenersCreator
    {
        private readonly Func<Ring1Node, IAsyncRing1NodeListener> _newListenersCreatorFunc;

        public FromLambdaListenersCreator(Func<Ring1Node, IAsyncRing1NodeListener> newListenersCreatorFunc)
        {
            _newListenersCreatorFunc = newListenersCreatorFunc;
        }

        public IAsyncRing1NodeListener CreateNewListener(Ring1Node node)
        {
            return _newListenersCreatorFunc(node);
        }
    }
}