using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Heightmaps.Ring1.valTypes;
using UnityEngine;

namespace Assets.Utils.Spatial
{
    public class StoredPartsRepository<T>
    {
        private Dictionary<MyRectangle, T> _dict = new Dictionary<MyRectangle, T>();

        private Dictionary<MyRectangle, TaskCompletionSource<object>> _promisedPartsDict =
            new Dictionary<MyRectangle, TaskCompletionSource<object>>();


        public async Task<T> TryRetriveAsync(MyRectangle coords)
        {
            if (_promisedPartsDict.ContainsKey(coords))
            {
                var tcs = _promisedPartsDict[coords];
                await tcs.Task;
                return _dict[coords];
            }
            if (_dict.ContainsKey(coords))
            {
                return _dict[coords];
            }
            else
            {
                return default(T);
            }
        }

        public void AddPart(MyRectangle coords, T part)
        {
            Preconditions.Assert(!_dict.ContainsKey(coords), "There arleady is element for coords: " + coords);
            _dict[coords] = part;
            if (_promisedPartsDict.ContainsKey(coords))
            {
                _promisedPartsDict[coords].SetResult(null);
                _promisedPartsDict.Remove(coords);
            }
        }

        public void AddPartsPromise(MyRectangle coords)
        {
            _promisedPartsDict[coords] = new TaskCompletionSource<object>();
        }
    }
}