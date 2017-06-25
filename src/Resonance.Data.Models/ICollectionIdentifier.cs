using System;

namespace Resonance.Data.Models
{
    public interface ICollectionIdentifier
    {
        Guid CollectionId { get; set; }
    }
}