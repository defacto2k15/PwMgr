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

        //private InitializationField<GRingSpotUpdater> _spotUpdaterField =
        //    new InitializationField<GRingSpotUpdater>("GRingSpotUpdater");
        //public GRingSpotUpdater GRingSpotUpdater
        //{
        //    get { return _spotUpdaterField.RetriveValue(); }
        //    set { _spotUpdaterField.SetValue(value); }
        //}

        //private InitializationField<DesignBodySpotUpdaterProxy> _designBodySpotUpdaterField =
        //    new InitializationField<DesignBodySpotUpdaterProxy>("DesignBodySpotUpdaterProxy");
        //public DesignBodySpotUpdaterProxy DesignBodySpotUpdaterProxy
        //{
        //    get { return _designBodySpotUpdaterField.RetriveValue(); }
        //    set { _designBodySpotUpdaterField.SetValue(value); }
        //}

        //private class InitializationField<T>
        //{
        //    private string _name;
        //    private bool _fieldSet = false;
        //    private T _value;

        //    public InitializationField(string name)
        //    {
        //        _name = name;
        //    }

        //    public void SetValue(T value)
        //    {
        //        _value = value;
        //        _fieldSet = true;
        //    }

        //    public T RetriveValue()
        //    {
        //        Preconditions.Assert(_fieldSet, $"Field {_name} is not set");
        //        return _value;
        //    }
        //}
    }
}