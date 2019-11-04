using System;
using Assets.AI.Bot;
using UnityEngine;

namespace Assets.AITesting.CoAsserts
{
    public class PassesNearObjectAssert : AIBotTestingAssert
    {
        private GameObject _objectToBeNearOf;
        private float _successDistance;
        private float _minimumDistance;

        public PassesNearObjectAssert(AIBotOC bot, GameObject objectToBeNearOf, float successDistance) : base(bot)
        {
            _objectToBeNearOf = objectToBeNearOf;
            _successDistance = successDistance;
            _minimumDistance = float.MaxValue;
        }

        public override void Update()
        {
            var distance = Vector3.Distance(Bot.transform.position, _objectToBeNearOf.transform.position);
            _minimumDistance = Math.Min(_minimumDistance, distance);
        }

        public override void CheckSuccess()
        {
            MyAssert.LessThan(_minimumDistance, _successDistance);
        }

        public override string GetDescription()
        {
            return $"During test at least once should be near {_objectToBeNearOf} - its distance must be <= than {_successDistance}";
        }

    }
}