using Microsoft.AspNetCore.Authentication;
using System;

namespace Resonance.Common.Web
{
    public static class ResonanceAuthenticationExtensions
    {
        public static AuthenticationBuilder AddResonanceAuthenticationScheme(this AuthenticationBuilder builder, Action<ResonanceAuthentionSchemeOptions> configureOptions)
        {
            return builder.AddScheme<ResonanceAuthentionSchemeOptions, ResonanceAuthenticationHandler>(ResonanceAuthenticationConstants.ResonanceAuthenticationScheme, ResonanceAuthenticationConstants.ResonanceAuthentication, configureOptions);
        }
    }
}
