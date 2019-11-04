using System.Collections.Generic;
using Assets.Scheduling;
using UnityEngine;

namespace Assets.Utils.UTUpdating
{
    public class GlobalServicesProfileInfo 
    {
        private List<UTServiceProfileInfo> _utServicesProfileInfo = new List<UTServiceProfileInfo>();
        private List<OtherThreadServiceProfileInfo> _otServicesProfileInfo = new List<OtherThreadServiceProfileInfo>();

        public void SetProfileInfos(
            List<UTServiceProfileInfo> utServicesProfileInfo,
            List<OtherThreadServiceProfileInfo> otServicesProfileInfo)
        {
            _utServicesProfileInfo = utServicesProfileInfo;
            _otServicesProfileInfo = otServicesProfileInfo;
        }

        public List<UTServiceProfileInfo> UtServicesProfileInfo => _utServicesProfileInfo;

        public List<OtherThreadServiceProfileInfo> OtServicesProfileInfo => _otServicesProfileInfo;
    }
}