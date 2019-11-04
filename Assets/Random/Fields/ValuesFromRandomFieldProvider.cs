using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Random.Fields
{
    public class ValuesFromRandomFieldProvider
    {
        private readonly RandomFieldNature _nature;
        private readonly float _seed;
        private readonly Ring2RandomFieldFigureRepository _randomFieldFigureRepository;

        public ValuesFromRandomFieldProvider(RandomFieldNature nature,
            float seed,
            Ring2RandomFieldFigureRepository randomFieldFigureRepository)
        {
            _nature = nature;
            _seed = seed;
            _randomFieldFigureRepository = randomFieldFigureRepository;
        }

        public Task<List<float>> ComputeValuesAsync(List<Vector2> queryPositions)
        {
            return _randomFieldFigureRepository.GetValuesAsync(_nature, _seed, queryPositions);
        }
    }
}