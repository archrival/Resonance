using Newtonsoft.Json;
using System;
using System.Xml.Linq;

namespace Resonance.Common
{
	public static class JsonExtensions
	{
		private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			TypeNameHandling = TypeNameHandling.None,
			MissingMemberHandling = MissingMemberHandling.Ignore,
			DateFormatHandling = DateFormatHandling.IsoDateFormat,
			DateTimeZoneHandling = DateTimeZoneHandling.Utc
		};

		public static T DeserializeFromJson<T>(this string json) where T : class
		{
			return JsonConvert.DeserializeObject<T>(json, JsonSettings);
		}

		public static string SerializeToJson<T>(this T graph) where T : class
		{
			return JsonConvert.SerializeObject(graph, JsonSettings);
		}

		/// <summary>
		/// Serializes the <see cref="T:System.Xml.Linq.XNode" /> to a JSON string using formatting.
		/// </summary>
		/// <param name="node">The node to convert to JSON.</param>
		/// <param name="formatting">Indicates how the output is formatted.</param>
		/// <returns>A JSON string of the XNode.</returns>
		public static string SerializeXObject(this XObject node, Formatting formatting = Formatting.None)
		{
			return SerializeXObject(node, formatting, false, true, null);
		}

		/// <summary>
		/// Serializes the <see cref="T:System.Xml.Linq.XNode" /> to a JSON string using formatting and omits the root object if <paramref name="omitRootObject" /> is <c>true</c>.
		/// </summary>
		/// <param name="node">The node to serialize.</param>
		/// <param name="formatting">Indicates how the output is formatted.</param>
		/// <param name="omitRootObject">Omits writing the root object.</param>
		/// <param name="prependOutput"></param>
		/// <param name="getValueFunc"></param>
		/// <returns>A JSON string of the XNode.</returns>
		public static string SerializeXObject(this XObject node, Formatting formatting, bool omitRootObject, bool prependOutput, Func<string, string, object> getValueFunc)
		{
			var xmlNodeConverter = new XObjectConverter()
			{
				OmitRootObject = omitRootObject,
				PrependOutput = prependOutput,
				GetValue = getValueFunc
			};

			return JsonConvert.SerializeObject(node, formatting, xmlNodeConverter);
		}
	}
}