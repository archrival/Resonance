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
				var queryParameters = context.GetSubsonicQueryParameters();

				context.Result = await ConvertToResultFormatAsync(response, queryParameters).ConfigureAwait(false);
			}

			await next().ConfigureAwait(false);
		}
	}
}