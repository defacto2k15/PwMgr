using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.Painter;
using Assets.MeshGeneration;
using Assets.Utils;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.Heightmaps.Ring1.MeshGeneration
{
    public class MeshGeneratorUTProxy : BaseUTTransformProxy<Mesh, Func<Mesh>>
    {
        private MeshGeneratorService _service;

        public MeshGeneratorUTProxy(MeshGeneratorService service)
        {
            _service = service;
        }

        public Task<Mesh> AddOrder(Func<Mesh> action)
        {
            return BaseUtAddOrder(action);
        }

        protected override Mesh ExecuteOrder(Func<Mesh> order)
        {
            return order();
        }
    }


    public class MeshGeneratorService
    {
        public void ProcessOrder(MeshGeneratorOrder order)
        {
            var result = order.GenerationFunction();
            order.Tcs.SetResult(result);
        }
    }

    public class MeshGeneratorOrder
    {
        public TaskCompletionSource<Mesh> Tcs;
        public Func<Mesh> GenerationFunction;
    }
}