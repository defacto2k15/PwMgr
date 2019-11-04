namespace Assets.Trees.DesignBodyDetails.DesignBodyCharacteristics
{
    public struct RepresentationCombinationId
    {
        public DesignBodyRepresentationQualifier RepresentationQualifier;
        public int RepresentationIndex;

        public RepresentationCombinationId(DesignBodyRepresentationQualifier representationQualifier,
            int representationIndex)
        {
            RepresentationQualifier = representationQualifier;
            RepresentationIndex = representationIndex;
        }
    }
}