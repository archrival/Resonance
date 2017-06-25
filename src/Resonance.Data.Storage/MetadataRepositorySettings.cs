namespace Resonance.Data.Storage
{
    public class MetadataRepositorySettings : IMetadataRepositorySettings
    {
        public string AssemblyName { get; set; }
        public string Parameters { get; set; }
        public string ResonancePath { get; set; }
        public string TypeName { get; set; }
    }
}