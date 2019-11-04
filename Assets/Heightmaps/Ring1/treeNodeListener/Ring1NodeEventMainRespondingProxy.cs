using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;
using UnityEngine;
using BaseOtherThreadProxy = Assets.Utils.MT.BaseOtherThreadProxy;

namespace Assets.Heightmaps.Ring1.treeNodeListener
{
    public class Ring1NodeEventMainRespondingProxy : BaseOtherThreadProxy
    {
        private Ring1NodeEventMainResponder _mainEventMainResponder;

        public Ring1NodeEventMainRespondingProxy(Ring1NodeEventMainResponder mainEventMainResponder) : base(
            "Ring1NodeEventMainRespondingProxyThread", false)
        {
            _mainEventMainResponder = mainEventMainResponder;
        }

        public void AddOrder(Queue<Ring1NodeEventMainRespondingOrder> order)
        {
            PostChainedAction( () =>  _mainEventMainResponder.ProcessOrderAsync(order));
        }
    }
}