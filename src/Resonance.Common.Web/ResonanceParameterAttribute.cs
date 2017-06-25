using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Resonance.Common.Web
{
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class ResonanceParameterAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider
	{
		public BindingSource BindingSource => new BindingSource("resonance", "Resonance", IsGreedy, IsFromRequest);

		public bool IsFromRequest { get; set; } = true;
		public bool IsGreedy { get; set; } = true;
		public string Name { get; set; }
	}
}