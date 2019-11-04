using System.Collections.Generic;
using System.Linq;

namespace Assets.NPRResources.TonalArtMap
{
    public class TAMTemplateGenerator
    {
        private ITAMImageDiagramGenerator _imageDiagramGenerator;
        private TAMTemplateSpecification _specification;
        private Dictionary<TAMTone, Dictionary<TAMMipmapLevel, TAMImageDiagram>> _columns;

        public TAMTemplateGenerator(ITAMImageDiagramGenerator imageDiagramGenerator)
        {
            _imageDiagramGenerator = imageDiagramGenerator;
        }

        public TAMTemplate Generate(TAMTemplateSpecification specification)
        {
            _specification = specification;
            _columns = new Dictionary<TAMTone, Dictionary<TAMMipmapLevel, TAMImageDiagram>>();

            var seed = 0; //TODO
            foreach (var tone in _specification.Tones)
            {
                _columns[tone] = new Dictionary<TAMMipmapLevel, TAMImageDiagram>();
                foreach (var mipmapLevel in _specification.MipmapLevels)
                {
                    var previousToneImageDiagram = GetPreviousToneImageDiagram(tone, mipmapLevel);
                    var previousLevelImageDiagram = GetPreviousLevelImageDiagram(tone, mipmapLevel);
                    var sumImageDiagram = TAMImageDiagram.Combine(previousLevelImageDiagram, previousToneImageDiagram);
                    var newImageDiagram = GenerateNewImageDiagram(sumImageDiagram, tone, mipmapLevel, seed);
                    AddNewImageDiagram(newImageDiagram, tone, mipmapLevel);
                    seed++;
                }
            }

            UpdatePerMipmapLevelStrokeParameters();
            return new TAMTemplate(_columns);
        }

        private void UpdatePerMipmapLevelStrokeParameters()
        {
            _columns = _columns.ToDictionary(c => c.Key,
                c => c.Value.ToDictionary(k => k.Key, k => _imageDiagramGenerator.UpdatePerMipmapLevelStrokeParameters(k.Value, k.Key)));
        }

        private TAMImageDiagram GetPreviousLevelImageDiagram(TAMTone tone, TAMMipmapLevel mipmapLevel)
        {
            if (mipmapLevel.IsLowestLevel)
            {
                return TAMImageDiagram.CreateEmpty();
            }
            else
            {
                return _columns[tone][mipmapLevel.LowerLevel];
            }
        }

        private TAMImageDiagram GetPreviousToneImageDiagram(TAMTone tone, TAMMipmapLevel mipmapLevel)
        {
            if (tone.IsLowestTone)
            {
                return TAMImageDiagram.CreateEmpty();
            }
            else
            {
                return _columns[tone.LowerTone][mipmapLevel];
            }
        }

        private TAMImageDiagram GenerateNewImageDiagram(TAMImageDiagram baseImageDiagram, TAMTone tone, TAMMipmapLevel mipmapLevel, int seed)
        {
            return _imageDiagramGenerator.Generate(baseImageDiagram, tone, mipmapLevel, seed);
        }

        private void AddNewImageDiagram(TAMImageDiagram template, TAMTone tone, TAMMipmapLevel mipmapLevel)
        {
            _columns[tone][mipmapLevel] = template;
        }
    }
}