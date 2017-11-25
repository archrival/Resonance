using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Resonance.Common.Web;
using Resonance.Data.Storage;
using Subsonic.Common.Enums;
using System.Linq;
using System.Threading.Tasks;

namespace Resonance.SubsonicCompat
{
    public class SubsonicAsyncAuthorizationFilter : SubsonicFilter, IAsyncAuthorizationFilter
    {
        private readonly SubsonicAuthorization _subsonicAuthorization;

        public SubsonicAsyncAuthorizationFilter(IMetadataRepository metadataRepository)
        {
            _subsonicAuthorization = new SubsonicAuthorization(metadataRepository);
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var queryParameters = context.HttpContext.Request.GetSubsonicQueryParameters();

            var authorizationContext = await _subsonicAuthorization.AuthorizeRequestAsync(queryParameters, context.HttpContext.GetCancellationToken()).ConfigureAwait(false);

            if (!authorizationContext.IsAuthenticated)
            {
                context.Result = context.GetActionResult(authorizationContext.CreateAuthorizationFailureResponse());
            }
            else
            {
                var authorizeFilter = context.Filters.OfType<AuthorizeFilter>().FirstOrDefault();

                var authorizationRequirement = authorizeFilter?.Policy.Requirements.OfType<RolesAuthorizationRequirement>().FirstOrDefault();

                if (authorizationRequirement != null && authorizationRequirement.AllowedRoles.Any())
                {
                    var inRole = authorizationRequirement.AllowedRoles.Any(r => authorizationContext.IsInRole(r));

                    if (!inRole)
                    {
                        authorizationContext.ErrorCode = (int)ErrorCode.UserNotAuthorized;
                        authorizationContext.Status = SubsonicConstants.UserIsNotAuthorizedForTheGivenOperation;

                        context.Result = context.GetActionResult(authorizationContext.CreateAuthorizationFailureResponse());
                    }
                }

                context.HttpContext.Items.Add(SubsonicConstants.SubsonicQueryParameters, queryParameters);
                context.HttpContext.Items.Add(SubsonicConstants.AuthenticationContext, authorizationContext);
                context.HttpContext.User = authorizationContext;
            }
        }
    }
}