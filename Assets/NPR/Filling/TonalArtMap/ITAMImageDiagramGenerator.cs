namespace Assets.NPRResources.TonalArtMap
{
    public interface ITAMImageDiagramGenerator
    {
        TAMImageDiagram Generate(TAMImageDiagram baseDiagram, TAMTone tone, TAMMipmapLevel mipmapLevel, int seed);
        TAMImageDiagram UpdatePerMipmapLevelStrokeParameters(TAMImageDiagram diagram, TAMMipmapLevel level);
    }
}