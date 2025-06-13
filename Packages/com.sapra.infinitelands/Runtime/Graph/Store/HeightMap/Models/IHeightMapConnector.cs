namespace sapra.InfiniteLands{
    public interface IHeightMapConnector
    {
        public int ConnectHeightMap(PathData currentBranch, MeshSettings meshSettings, int acomulatedResolution);
    }
}