using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1;
using Assets.Utils;
using Assets.Utils.MT;
using Assets.Utils.Services;

namespace Assets.Utils.MT
{
    public class GenericAsyncExecutionThreadProxy : BaseOtherThreadProxy
    {
        public GenericAsyncExecutionThreadProxy(string threadProxyName) : base(threadProxyName, false)
        {
        }
    }
}