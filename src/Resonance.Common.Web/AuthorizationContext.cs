using Resonance.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace Resonance.Common.Web
{
	public class AuthorizationContext
	{
		public int? ErrorCode { get; set; }
		public bool IsAuthenticated { get; set; }
		public IEnumerable<Role> Roles { get; set; }
		public string Status { get; set; }
		public User User { get; set; }

		public bool IsInRole(Role role)
		{
			return Roles.Any(r => r.Equals(Role.Administrator) || r.Equals(role));
		}

		public GenericPrincipal ToClaimsPrincipal()
		{
			var identity = new GenericIdentity(User.Name);
			var principal = new GenericPrincipal(identity, Roles.Select(r => r.ToString()).ToArray());

			return principal;
		}
	}
}