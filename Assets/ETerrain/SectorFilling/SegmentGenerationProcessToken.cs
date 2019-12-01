namespace Assets.ETerrain.SectorFilling
{
    public class SegmentGenerationProcessToken
    {
        private volatile  SegmentGenerationProcessSituation _currentSituation;
        private volatile  RequiredSegmentSituation _requiredSituation;

        public SegmentGenerationProcessToken(SegmentGenerationProcessSituation currentSituation, RequiredSegmentSituation requiredSituation)
        {
            _currentSituation = currentSituation;
            _requiredSituation = requiredSituation;
        }

        public SegmentGenerationProcessSituation CurrentSituation
        {
            get => _currentSituation;
            set => _currentSituation = value;
        }

        public RequiredSegmentSituation RequiredSituation
        {
            get => _requiredSituation;
            set => _requiredSituation = value;
        }

        public bool ProcessIsOngoing =>
            (_currentSituation == SegmentGenerationProcessSituation.DuringCreation ||
             _currentSituation == SegmentGenerationProcessSituation.DuringFilling);
    }
}