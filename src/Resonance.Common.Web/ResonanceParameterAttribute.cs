using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;

namespace Resonance.Common.Web
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class ResonanceParameterAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider
    {
        public BindingSource BindingSource => new BindingSource("resonance", "Resonance", IsGreedy, IsFromRequest);

        public bool IsFromRequest { get; set; } = true;
        public bool IsGreedy { get; set; } = true;
        public string Name { get; set; }
    }
}