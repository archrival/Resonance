﻿using Newtonsoft.Json;
using Resonance.Common;
using System;

namespace Resonance.Data.Models
{
    public abstract class ModelBase
    {
        protected ModelBase()
        {
            Id = Guid.NewGuid();
        }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        #region HashCode and Equality Overrides

        public static bool operator !=(ModelBase left, ModelBase right)
        {
            return !(left == right);
        }

        public static bool operator ==(ModelBase left, ModelBase right)
        {
            if (left is null)
            {
                return right is null;
            }

            if (right is null)
            {
                return false;
            }

            return left.PropertiesEqual(right, nameof(Id));
        }

        public override bool Equals(object obj)
        {
            return obj != null && Equals(obj as ModelBase);
        }

        public override int GetHashCode()
        {
            return this.GetHashCodeForObject(Id);
        }

        private bool Equals(ModelBase item)
        {
            return item != null && this == item;
        }

        #endregion HashCode and Equality Overrides
    }
}