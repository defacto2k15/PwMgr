using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Assets.Utils.MyAsset
{
    public class MyBufferCacheAssetListingGO : MonoBehaviour
    {
        public List<Object> AssetsList;

        public Object RetriveAssetOfName(string queryName)
        {
            return AssetsList.FirstOrDefault(c => c.name.Equals(queryName));
        }
    }
}
