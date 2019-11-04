using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.Ring2.Devising;

namespace Assets.Ring2.RuntimeManagementOtherThread.Finalizer
{
    public interface IRing2PatchesOverseerFinalizer
    {
        Task<List<Ring2PatchDevised>> FinalizePatchesCreation(List<Ring2PatchDevised> devisedPatches);
    }
}