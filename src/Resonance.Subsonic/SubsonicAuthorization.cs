using Microsoft.Extensions.Options;
using Resonance.Common;
using Resonance.Common.Web;
using Resonance.Data.Storage;
using Subsonic.Common.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.SubsonicCompat
{
    public class SubsonicAuthorization
    {
        private static readonly Lazy<IMetadataRepository> MetadataRepositoryLazy = new Lazy<IMetadataRepository>(() =>
        {
            var metadataRepositoryFactory = new MetadataRepositoryFactory();

            return metadataRepositoryFactory.Create(_settings.Value);
        });

        private static IOptions<MetadataRepositorySettings> _settings;

        public SubsonicAuthorization(IOptions<MetadataRepositorySettings> settings)
        {
            _settings = settings;
        }

        private IMetadataRepository MetadataRepository => MetadataRepositoryLazy.Value;

        public async Task<AuthorizationContext> AuthorizeRequestAsync(SubsonicQueryParameters parameters, CancellationToken cancellationToken)
        {
            var authenticationContext = new AuthorizationContext();

            if (parameters == null || string.IsNullOrWhiteSpace(parameters.Username) || string.IsNullOrWhiteSpace(parameters.ClientName))
            {
                authenticationContext.IsAuthenticated = false;
                authenticationContext.ErrorCode = (int)ErrorCode.RequiredParameterMissing;
                authenticationContext.Status = SubsonicConstants.RequiredParameterIsMissing;

                return authenticationContext;
            }

            var user = await MetadataRepository.GetUserAsync(parameters.Username, cancellationToken);

            if (user == null || !user.Enabled)
            {
                user = null;

                authenticationContext.IsAuthenticated = false;
                authenticationContext.ErrorCode = (int)ErrorCode.WrongUsernameOrPassword;
                authenticationContext.Status = SubsonicConstants.WrongUsernameOrPassword;

                return authenticationContext;
            }

            var authorizationSuccess = false;

            if (parameters.Password != null)
            {
                authorizationSuccess = SubsonicControllerExtensions.ParsePassword(parameters.Password) == user.Password.DecryptToString(Constants.ResonanceKey);
            }
            else if (parameters.AuthenticationToken != null && parameters.Salt != null)
            {
                authorizationSuccess = parameters.AuthenticationToken == $"{user.Password.DecryptToString(Constants.ResonanceKey)}{parameters.Salt}".GetMd5Hash();
            }

            user.Password = null;

            authenticationContext.User = user;

            if (!authorizationSuccess)
            {
                authenticationContext.IsAuthenticated = false;
                authenticationContext.ErrorCode = (int)ErrorCode.WrongUsernameOrPassword;
                authenticationContext.Status = SubsonicConstants.WrongUsernameOrPassword;
            }
            else
            {
                authenticationContext.IsAuthenticated = true;

                if (user.Roles == null || !user.Roles.Any())
                {
                    authenticationContext.Roles = await MetadataRepository.GetRolesForUserAsync(user.Id, cancellationToken);
                }
                else
                {
                    authenticationContext.Roles = user.Roles;
                }
            }

            return authenticationContext;
        }
    }
}