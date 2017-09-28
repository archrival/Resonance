using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Resonance.Common;
using Subsonic.Common.Classes;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Resonance.SubsonicCompat
{
    public class SubsonicFilter
    {
        public async Task<IActionResult> ConvertToResultFormatAsync(Response response, SubsonicQueryParameters queryParameters)
        {
            var xmlString = await response.SerializeToXmlAsync();

            switch (queryParameters.Format)
            {
                case SubsonicReturnFormat.Json:
                    {
                        return CreateContentResult(GetJsonResponse(xmlString), "application/json", Encoding.UTF8);
                    }

                case SubsonicReturnFormat.Jsonp:
                    {
                        return CreateContentResult($"{queryParameters.Callback}({GetJsonResponse(xmlString)});", "text/javascript", Encoding.UTF8);
                    }

                case SubsonicReturnFormat.Xml:
                    {
                        return CreateContentResult(xmlString, "text/xml", Encoding.UTF8);
                    }

                default:
                    {
                        return null;
                    }
            }
        }

        public ContentResult CreateContentResult(string content, string contentType, Encoding contentEncoding)
        {
            var contentResult = new ContentResult { Content = content };
            var mediaTypeHeader = MediaTypeHeaderValue.Parse(contentType);

            mediaTypeHeader.Encoding = contentEncoding ?? mediaTypeHeader.Encoding;
            contentResult.ContentType = mediaTypeHeader.ToString();

            return contentResult;
        }

        private static string GetJsonResponse(string xmlString)
        {
            var xElement = XElement.Parse(xmlString);

            var jsonResponse = xElement.SerializeXObject(Formatting.None, false, false, SubsonicControllerExtensions.GetValueForPropertyName);

            return jsonResponse;
        }
    }
}