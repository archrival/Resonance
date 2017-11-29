using Resonance.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace Resonance.Common.Web
{
    public class AuthorizationContext : ClaimsPrincipal
    {
        public int? ErrorCode { get; set; }
        public override IIdentity Identity => new GenericIdentity(User.Name);
        public bool IsAuthenticated { get; set; }
        public IEnumerable<Role> Roles { get; set; }
        public string Status { get; set; }
        public User User { get; set; }

        public bool IsInRole(Role role, bool administrator = true)
        {
            return Roles.Any(r => r.Equals(role) || administrator ? r.Equals(Role.Administrator) : true);
        }

        public override bool IsInRole(string role)
        {
            return Enum.TryParse(role, out Role roleValue) && IsInRole(roleValue);
        }
    }
}