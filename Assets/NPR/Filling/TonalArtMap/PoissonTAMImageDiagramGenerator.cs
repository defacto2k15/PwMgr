using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.NPRResources.TonalArtMap
{
    public class PoissonTAMImageDiagramGenerator : ITAMImageDiagramGenerator
    {
        private TAMPoissonDiskSampler _sampler;
        private PoissonTAMImageDiagramGeneratorConfiguration _configuration;
        private StrokesGenerator _strokesGenerator;

        public PoissonTAMImageDiagramGenerator(TAMPoissonDiskSampler sampler, StrokesGenerator strokesGenerator,  PoissonTAMImageDiagramGeneratorConfiguration configuration)
        {
            _sampler = sampler;
            _configuration = configuration;
            _strokesGenerator = strokesGenerator;
        }

        public TAMImageDiagram Generate(TAMImageDiagram diagram, TAMTone tone, TAMMipmapLevel mipmapLevel, int seed)
        {
            List<Vector2> currentPoints = diagram.Strokes.Select(c => c.Center).ToList();
            var newPoints = _sampler.Generate(_configuration.GenerationCount, _configuration.ExclusionZoneValues[tone][mipmapLevel],
                seed, currentPoints);
            
            return new TAMImageDiagram(diagram.Strokes.Union(newPoints.Select(c => _strokesGenerator.CreateRandomStroke(c, tone))).ToList());

        }

        public TAMImageDiagram UpdatePerMipmapLevelStrokeParameters(TAMImageDiagram diagram, TAMMipmapLevel level)
        {
            return new TAMImageDiagram( diagram.Strokes.Select(c => _strokesGenerator.UpdatePerMipmapLevelStrokeParameters(c,  level)).ToList());
        }
    }
}