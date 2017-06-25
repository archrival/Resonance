using Newtonsoft.Json;

namespace Resonance.SubsonicCompat
{
	public enum SubsonicReturnFormat
	{
		[JsonProperty("xml")]
		Xml,

		[JsonProperty("json")]
		Json,

		[JsonProperty("jsonp")]
		Jsonp
	}
}