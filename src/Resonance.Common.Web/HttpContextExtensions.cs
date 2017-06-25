using Microsoft.AspNetCore.Http;
using System.Threading;

namespace Resonance.Common.Web
{
	public static class HttpContextExtensions
	{
		public static CancellationToken GetCancellationToken(this HttpContext httpContext)
		{
			var tokenSource = new CancellationTokenSource();
			var cancellationToken = tokenSource.Token;

			var disconnectedToken = httpContext.RequestAborted;
			return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, disconnectedToken).Token;
		}
	}
}