using Newtonsoft.Json;
using System;
using System.Xml.Linq;

namespace Resonance.Common
{
    public static class JsonExtensions
    {
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