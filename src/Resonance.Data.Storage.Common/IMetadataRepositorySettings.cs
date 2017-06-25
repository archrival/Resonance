namespace Resonance.Data.Storage
{
    public interface IMetadataRepositorySettings
    {
        string AssemblyName { get; set; }
        string Parameters { get; set; }
        string ResonancePath { get; set; }
        string TypeName { get; set; }
    }
}