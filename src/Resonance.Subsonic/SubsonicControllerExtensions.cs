using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Resonance.Common;
using Resonance.Common.Web;
using Subsonic.Common.Classes;
using Subsonic.Common.Enums;
using System;
using System.Security;
using System.Text;

namespace Resonance.SubsonicCompat
{
    public static class SubsonicControllerExtensions
    {
        public static Response CreateAuthorizationFailureResponse(this AuthorizationContext authenticationContext)
        {
            var response = CreateResponse();

            response.Status = ResponseStatus.Failed;
            response.ItemElementName = ItemChoiceType.Error;
            response.Item = new Error
            {
                Code = (ErrorCode)authenticationContext.ErrorCode.GetValueOrDefault(),
                Message = authenticationContext.Status
            };

            return response;
        }

        public static Response CreateFailureResponse(ErrorCode errorCode, string message)
        {
            var response = CreateResponse();

            response.Status = ResponseStatus.Failed;
            response.ItemElementName = ItemChoiceType.Error;
            response.Item = new Error
            {
                Code = errorCode,
                Message = message
            };

            return response;
        }

        public static Response CreateResponse(ItemChoiceType itemChoiceType, object item)
        {
            var response = CreateResponse();

            response.ItemElementName = itemChoiceType;
            response.Item = item;

            return response;
        }

        public static Response CreateResponse()
        {
            return new Response
            {
                Status = ResponseStatus.Ok,
                Version = SubsonicConstants.ServerVersion
            };
        }

        public static AuthorizationContext GetAuthorizationContext(this ActionContext context)
        {
            return context.HttpContext.Items[SubsonicConstants.AuthenticationContext] as AuthorizationContext;
        }

        public static SubsonicQueryParameters GetSubsonicQueryParameters(this ActionContext context)
        {
            return context.HttpContext.Items[SubsonicConstants.SubsonicQueryParameters] as SubsonicQueryParameters;
        }

        public static SubsonicQueryParameters GetSubsonicQueryParameters(this HttpRequest request)
        {
            return request.HasFormContentType ? SubsonicQueryParameters.FromCollection(request.Form) : SubsonicQueryParameters.FromCollection(request.Query);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static object GetValueForPropertyName(string value, string propertyName)
        {
            switch (propertyName)
            {
                case "isDir":
                case "adminRole":
                case "commentRole":
                case "coverArtRole":
                case "downloadRole":
                case "jukeboxRole":
                case "playlistRole":
                case "podcastRole":
                case "scrobblingEnabled":
                case "settingsRole":
                case "shareRole":
                case "streamRole":
                case "uploadRole":
                case "videoConversionRole":
                case "playing":
                case "valid":
                case "public":
                    return bool.Parse(value);

                case "playCount":
                case "userRating":
                case "duration":
                case "year":
                case "songCount":
                case "albumCount":
                case "bitRate":
                case "discNumber":
                case "originalHeight":
                case "originalWidth":
                case "track":
                case "offset":
                case "totalHits":
                case "visitCount":
                case "maxBitrate":
                case "audioTrackId":
                    return int.Parse(value);

                case "averageRating":
                case "gain":
                    return float.Parse(value);

                case "position":
                case "time":
                case "bookmarkPosition":
                case "size":
                    return long.Parse(value);

                default:
                    return value;
            }
        }

        public static string ParsePassword(string password)
        {
            if (!password.StartsWith("enc:", StringComparison.OrdinalIgnoreCase))
            {
                return password;
            }

            var decodedPassword = password.Substring(4, password.Length - 4);

            var bytes = new byte[decodedPassword.Length / 2];

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(decodedPassword.Substring(i * 2, 2), 16);
            }

            return Encoding.UTF8.GetString(bytes);
        }
    }
}