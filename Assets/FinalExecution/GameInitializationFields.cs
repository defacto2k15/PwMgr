using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Heightmaps.GRing;
using Assets.Trees.SpotUpdating;
using Assets.Utils;
using JetBrains.Annotations;

namespace Assets.FinalExecution
{
    public class GameInitializationFields
    {
        private Dictionary<Type, object> _gameFields = new Dictionary<Type, object>();

        public void SetField<T>(T value)
        {
            _gameFields[typeof(T)] = value;
        }

        public T Retrive<T>()
        {
            var queryType = typeof(T);
            var goodTypes = _gameFields.Keys.Where(c => queryType.IsAssignableFrom(c)).ToList();
            Preconditions.Assert(goodTypes.Count != 0, $"Field of type {queryType} is not found");
            Preconditions.Assert(goodTypes.Count < 2,
                $"Found more than 1 deriving type: {StringUtils.ToString(goodTypes)}");
            return (T) _gameFields[goodTypes[0]];
        }

        public bool HasField<T>()
        {
            var queryType = typeof(T);
            return _gameFields.Keys.Any(c => queryType.IsAssignableFrom(c));
        }
    }
}