using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Subsonic.Common.Classes;
using System.Threading.Tasks;

namespace Resonance.SubsonicCompat
{
	public class SubsonicAsyncResultFilter : SubsonicFilter, IAsyncResultFilter
	{
		public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
		{
			var result = context.Result as ObjectResult;

			if (result?.Value is Response response)
			{
			    context.Result = await context.GetActionResultAsync(response).ConfigureAwait(false);
			}

			await next().ConfigureAwait(false);
		}
	}
}