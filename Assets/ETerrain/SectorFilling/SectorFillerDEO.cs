using System.Collections.Generic;
using Assets.Utils;
using UnityEngine;

namespace Assets.ETerrain.SectorFilling
{
    public class SectorFillerDEO : MonoBehaviour
    {
        public GameObject Traveller;
        private Dictionary<IntVector2, GameObject> _segmentTokens = new Dictionary<IntVector2, GameObject>();
        private Vector2 _sectorSize = new Vector2(6,6);
        private Vector2 _initialTravellerPosition = Vector2.zero;
        private SegmentFiller _segmentFiller;

        public void Start()
        {
            IntVector2 marginsSize = new IntVector2(1,1);
            _segmentFiller = new SegmentFiller(_sectorSize.ToIntVector(), marginsSize, 90, new LambdaSegmentFillingListener(
                (c) => AddToken(c),
                (c) => RemoveToken(c.SegmentAlignedPosition),
                (c) => SetTokenState(c)));
            _segmentFiller.InitializeField(_initialTravellerPosition);

            Traveller.transform.position = new Vector3(_initialTravellerPosition.x, 0, _initialTravellerPosition.y);
        }

        public void Update()
        {
            var flatPosition = new Vector2(Traveller.transform.position.x, Traveller.transform.position.z);
            _segmentFiller.Update(flatPosition);
        }

        private void AddToken(SegmentInformation segmentInfo)
        {
            var pos = segmentInfo.SegmentAlignedPosition;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localPosition = new Vector3(pos.X*_sectorSize.x, 0, pos.Y*_sectorSize.y);
            go.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            if (_segmentTokens.ContainsKey(pos))
            {
                GameObject.Destroy(_segmentTokens[pos]);
            }
            _segmentTokens[pos] = go;

            SetTokenState(segmentInfo);
        }

        private void RemoveToken(IntVector2 pos)
        {
            GameObject.Destroy(_segmentTokens[pos]);
            _segmentTokens.Remove(pos);
        }

        private void SetTokenState(SegmentInformation segmentInfo)
        {
            var go = _segmentTokens[segmentInfo.SegmentAlignedPosition];
            if (segmentInfo.SegmentState == SegmentState.Active)
            {
                go.GetComponent<MeshRenderer>().material.color = Color.yellow;
            }
            else
            {
                go.GetComponent<MeshRenderer>().material.color = Color.green;
            }
        }
    }
}
