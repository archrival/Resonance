using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Resonance.Common.Web
{
    internal class ResonanceAuthenticationHandler : AuthenticationHandler<ResonanceAuthentionSchemeOptions>
    {
        public ResonanceAuthenticationHandler(IOptionsMonitor<ResonanceAuthentionSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            await Task.CompletedTask;

            return AuthenticateResult.NoResult();
        }
    }
}
