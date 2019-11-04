using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.Ring1.treeNodeListener;
using Assets.Heightmaps.Ring1.valTypes;
using Assets.Utils;
using UnityEngine;

namespace Assets.Heightmaps.Ring1
{
    class Ring1NodeObjects : IRing1NodeListener //todo dolete
    {
        //todo delete
        readonly Dictionary<MyRectangle, List<IRing1NodeListener>> _terrains =
            new Dictionary<MyRectangle, List<IRing1NodeListener>>();

        private readonly Func<Ring1Node, List<IRing1NodeListener>> _newListenersCreator;

        public Ring1NodeObjects(Func<Ring1Node, List<IRing1NodeListener>> newListenersCreator)
        {
            _newListenersCreator = newListenersCreator;
        }

        public void CreatedNewNode(Ring1Node ring1Node)
        {
            Preconditions.Assert(!_terrains.ContainsKey(ring1Node.Ring1Position),
                "Terrain was not created for this node");
            _terrains[ring1Node.Ring1Position] = _newListenersCreator.Invoke(ring1Node);
        }

        public void DoNotDisplay(Ring1Node ring1Node)
        {
            if (_terrains.ContainsKey(ring1Node.Ring1Position))
            {
                foreach (var listener in _terrains[ring1Node.Ring1Position])
                {
                    listener.DoNotDisplay(ring1Node);
                }
            }
        }

        public void Update(Ring1Node ring1Node, Vector3 cameraPosition)
        {
            foreach (var listener in _terrains[ring1Node.Ring1Position])
            {
                listener.Update(ring1Node, cameraPosition);
            }
        }

        public void EndBatch()
        {
            throw new NotImplementedException();
        }
    }
}