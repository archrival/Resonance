using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Resonance.Common.Web;
using Resonance.Data.Storage;
using System.Threading.Tasks;

namespace Resonance.SubsonicCompat
{
	public class SubsonicAsyncAuthorizationFilter : SubsonicFilter, IAsyncAuthorizationFilter
	{
		private readonly SubsonicAuthorization _subsonicAuthorization;

		public SubsonicAsyncAuthorizationFilter(IOptions<MetadataRepositorySettings> settings)
		{
			_subsonicAuthorization = new SubsonicAuthorization(settings);
		}

		public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
		{
			var queryParameters = context.HttpContext.Request.GetSubsonicQueryParameters();

			var authorizationContext = await _subsonicAuthorization.AuthorizeRequestAsync(queryParameters, context.HttpContext.GetCancellationToken()).ConfigureAwait(false);

			if (!authorizationContext.IsAuthenticated)
			{
				context.Result = await ConvertToResultFormatAsync(authorizationContext.CreateAuthorizationFailureResponse(), queryParameters).ConfigureAwait(false);
			}
			else
			{
				context.HttpContext.Items.Add(SubsonicConstants.SubsonicQueryParameters, queryParameters);
				context.HttpContext.Items.Add(SubsonicConstants.AuthenticationContext, authorizationContext);
				context.HttpContext.User = authorizationContext.ToClaimsPrincipal();
			}
		}
	}
}