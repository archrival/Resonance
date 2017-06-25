namespace Resonance.Data.Storage
{
    public interface IMetadataRepositoryFactory
    {
        IMetadataRepository Create(IMetadataRepositorySettings settings);
    }
}