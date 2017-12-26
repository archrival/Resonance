using Resonance.Common;
using Resonance.Common.Web;
using Resonance.Data.Storage;
using Subsonic.Common.Enums;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.SubsonicCompat
{
    public class SubsonicAuthorization
    {
        private readonly IMetadataRepository _metadataRepository;

        public SubsonicAuthorization(IMetadataRepository metadataRepository)
        {
            _metadataRepository = metadataRepository;
        }

        public async Task<AuthorizationContext> AuthorizeRequestAsync(SubsonicQueryParameters parameters, CancellationToken cancellationToken)
        {
            var authorizationContext = new AuthorizationContext();

            if (parameters == null || string.IsNullOrWhiteSpace(parameters.Username) || string.IsNullOrWhiteSpace(parameters.ClientName))
            {
                authorizationContext.IsAuthenticated = false;
                authorizationContext.ErrorCode = (int)ErrorCode.RequiredParameterMissing;
                authorizationContext.Status = SubsonicConstants.RequiredParameterIsMissing;

                return authorizationContext;
            }

            var user = await _metadataRepository.GetUserAsync(parameters.Username, cancellationToken).ConfigureAwait(false);

            if (user == null || !user.Enabled)
            {
                user?.Dispose();

                authorizationContext.IsAuthenticated = false;
                authorizationContext.ErrorCode = (int)ErrorCode.WrongUsernameOrPassword;
                authorizationContext.Status = SubsonicConstants.WrongUsernameOrPassword;

                return authorizationContext;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var authorizationSuccess = false;

            if (parameters.Password != null)
            {
                authorizationSuccess = SubsonicControllerExtensions.ParsePassword(parameters.Password) == user.Password.DecryptToString(Constants.ResonanceKey);
            }
            else if (parameters.AuthenticationToken != null && parameters.Salt != null)
            {
                authorizationSuccess = parameters.AuthenticationToken == $"{user.Password.DecryptToString(Constants.ResonanceKey)}{parameters.Salt}".GetHash(HashType.MD5, Encoding.UTF8);
            }

            user.Password = null;

            authorizationContext.User = user;

            if (!authorizationSuccess)
            {
                authorizationContext.IsAuthenticated = false;
                authorizationContext.ErrorCode = (int)ErrorCode.WrongUsernameOrPassword;
                authorizationContext.Status = SubsonicConstants.WrongUsernameOrPassword;
            }
            else
            {
                authorizationContext.IsAuthenticated = true;

                if (user.Roles == null || !user.Roles.Any())
                {
                    authorizationContext.Roles = await _metadataRepository.GetRolesForUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    authorizationContext.Roles = user.Roles;
                }
            }

            return authorizationContext;
        }
    }
}