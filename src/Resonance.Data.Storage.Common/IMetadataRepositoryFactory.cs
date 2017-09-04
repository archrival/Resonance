namespace Resonance.Data.Storage
{
    public interface IMetadataRepositoryFactory
    {
        IMetadataRepository CreateMetadataRepository();
    }
}