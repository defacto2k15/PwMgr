using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assets.AI.Bot;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.AITesting.CoAsserts
{
    public class EndsNearObjectAssert : AIBotTestingAssert
    {
        private GameObject _objectToBeNearOf;
        private float _successDistance;

        public EndsNearObjectAssert(AIBotOC bot, GameObject objectToBeNearOf, float successDistance) : base(bot)
        {
            _objectToBeNearOf = objectToBeNearOf;
            _successDistance = successDistance;
        }

        public override void CheckSuccess()
        {
            var distance = Vector3.Distance(Bot.transform.position, _objectToBeNearOf.transform.position);
            MyAssert.LessThan(distance, _successDistance);
        }

        public override string GetDescription()
        {
            return $"Should be near {_objectToBeNearOf} - its distance must be <= than {_successDistance}";
        }

    }
}
