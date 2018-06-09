using Newtonsoft.Json;
using Resonance.Common;
using System;

namespace Resonance.Data.Models
{
    [JsonObject("collection")]
    public class Collection : SettingBase
    {
        [JsonProperty("filter")]
        public string Filter { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        public static Collection FromDynamic(dynamic result)
        {
            var collection = new Collection
            {
                Id = DynamicExtensions.GetGuidFromDynamic(result.Id),
                DateAdded = DynamicExtensions.GetDateTimeFromDynamic(result.DateAdded),
                DateModified = result.DateModified == null ? null : DynamicExtensions.GetDateTimeFromDynamic(result.DateModified),
                Enabled = Convert.ToBoolean(result.Enabled),
                Filter = result.Filter,
                Name = result.Name,
                Path = result.Path
            };

            return collection;
        }

        #region HashCode and Equality Overrides

        private const int HashFactor = 17;
        private const int HashSeed = 73; // Should be prime number

        public static bool operator !=(Collection left, Collection right)
        {
            return !(left == right);
        }

        public static bool operator ==(Collection left, Collection right)
        {
            if (left is null)
                return right is null;

            if (right is null)
                return false;

            if (left.Id.Equals(right.Id))
                if (left.Name == right.Name)
                    if (left.Path == right.Path)
                        return true;

            return false;
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as Collection);
        }

        public override int GetHashCode()
        {
            int hash = HashSeed;
            hash = hash * HashFactor + typeof(Collection).GetHashCode();

            if (Id != null)
                hash = hash * HashFactor + Id.GetHashCode();

            if (Name != null)
                hash = hash * HashFactor + Name.GetHashCode();

            if (Path != null)
                hash = hash * HashFactor + Path.GetHashCode();

            return hash;
        }

        private bool Equals(Collection item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}