using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Ring2.Devising;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Utils;
using Assets.Utils.UTUpdating;

namespace Assets.Ring2.Painting
{
    public class Ring2PatchesPainterUTProxy : BaseUTTransformProxy<OverseedPatchId, Ring2PatchesPainterOrder>
    {
        private Ring2PatchesPainter _painter;

        public Ring2PatchesPainterUTProxy(Ring2PatchesPainter painter)
        {
            _painter = painter;
        }

        public Task<OverseedPatchId> AddOrder(Ring2PatchesPainterOrder order)
        {
            return BaseUtAddOrder(order);
        }

        protected override void InternalUpdate()
        {
            _painter.Update();
        }

        protected override OverseedPatchId ExecuteOrder(Ring2PatchesPainterOrder order)
        {
            return FufillOrder(order);
        }

        private OverseedPatchId FufillOrder(Ring2PatchesPainterOrder order)
        {
            if (order.RemovalOrder != null)
            {
                Preconditions.Assert(order.RemovalOrder.IsFilled, "Removal order ids are not filled");
            }

            if (order.RemovalOrder != null)
            {
                _painter.RemovePatch(order.RemovalOrder);
            }

            if (order.CreationOrder != null)
            {
                return _painter.AddPatch(order.CreationOrder);
            }
            else
            {
                return new OverseedPatchId();
            }
        }
    }

    public class Ring2PatchesPainterOrder
    {
        public Ring2PatchesPainterCreationOrder CreationOrder;
        public OverseedPatchId RemovalOrder;
    }

    public class Ring2PatchesPainterOrderWithTcs
    {
        public Ring2PatchesPainterOrder Order;
        public TaskCompletionSource<OverseedPatchId> Tcs;
    }

    public class Ring2PatchesPainterCreationOrder
    {
        public List<Ring2PatchDevised> Patches;
    }
}