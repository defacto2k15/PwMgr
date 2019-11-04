using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Random.Fields;
using Assets.Ring2.RuntimeManagementOtherThread;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.UTUpdating;
using UnityEngine;

namespace Assets.Ring2.Stamping
{
    public class UTRing2PlateStamperProxy : BaseUTTransformProxy<Ring2PlateStamp, Ring2PlateStampTemplate>
    {
        private Ring2PlateStamper _stamper;

        public UTRing2PlateStamperProxy(Ring2PlateStamper stamper) : base(false)
        {
            _stamper = stamper;
        }

        public Task<Ring2PlateStamp> GeneratePlateStampAsync(Ring2PlateStampTemplate template)
        {
            return BaseUtAddOrder(template);
        }

        protected override Ring2PlateStamp ExecuteOrder(Ring2PlateStampTemplate order)
        {
            return _stamper.GeneratePlateStamp(order);
        }

        public override bool InternalHasWorkToDo()
        {
            return _stamper.CurrentlyRendering;
        }

        protected override void InternalUpdate()
        {
            if (TaskUtils.GetGlobalMultithreading())
            {
                bool usingMultistepRendering = true;
                if (usingMultistepRendering)
                {
                    if (_stamper.CurrentlyRendering)
                    {
                        _stamper.Update();
                    }
                    else
                    {
                        // lets add neOrder order;
                        UTProxyTransformOrder<Ring2PlateStamp, Ring2PlateStampTemplate> transformOrder = TryGetNextFromQueue();
                        if (transformOrder == null)
                        {
                            return;
                        }
                        else
                        {
                            _stamper.StartGeneratingPlateStamp(transformOrder.Order,
                                (figure => { transformOrder.Tcs.SetResult(figure); }));
                        }
                    }
                }
                else
                {
                    UTProxyTransformOrder<Ring2PlateStamp, Ring2PlateStampTemplate> transformOrder = TryGetNextFromQueue();
                    if (transformOrder != null)
                    {
                        return;
                    }
                    else
                    {
                        var figure = _stamper.GeneratePlateStamp(transformOrder.Order);
                        transformOrder.Tcs.SetResult(figure);
                    }
                }
            }
        }
    }
}