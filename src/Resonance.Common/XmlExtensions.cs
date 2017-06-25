using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Resonance.Common
{
    public static class XmlExtensions
    {
        private static readonly Lazy<ConcurrentDictionary<Type, XmlSerializer>> _xmlSerializersLazy = new Lazy<ConcurrentDictionary<Type, XmlSerializer>>();

        private static readonly XmlWriterSettings _xmlWriterSettings = new XmlWriterSettings
        {
            Async = true,
            ConformanceLevel = ConformanceLevel.Document,
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = " ",
            NamespaceHandling = NamespaceHandling.OmitDuplicates
        };

        private static readonly XmlQualifiedName _emptyNamespace = new XmlQualifiedName(string.Empty, string.Empty);
        private static readonly XmlSerializerNamespaces _ignoredXmlSerializerNamespaces = new XmlSerializerNamespaces(new XmlQualifiedName[1] { _emptyNamespace });
        private static readonly Regex _ignoreNamespacesRegex = new Regex(@"(xmlns:?[^=]*=[""][^""]*[""])", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static readonly XmlSerializerNamespaces _xmlSerializerNamespaces = new XmlSerializerNamespaces();

        private static ConcurrentDictionary<Type, XmlSerializer> XmlSerializers => _xmlSerializersLazy.Value;

        /// <summary>
        /// Deserialize XML string into object type specified.
        /// </summary>
        /// <typeparam name="T">Object type to deserialize the XML into.</typeparam>
        /// <param name="xml">XML string to deserialize.</param>
        /// <param name="ignoreNamespace"></param>
        /// <returns>Deserialized object T</returns>
        public static T DeserializeFromXml<T>(this string xml, bool ignoreNamespace = true) where T : class, new()
        {
            T result;

            if (ignoreNamespace)
                xml = _ignoreNamespacesRegex.Replace(xml, string.Empty);

            var xmlSerializer = GetXmlSerializer(typeof(T));

            using (var sr = new StringReader(xml))
                result = (T)xmlSerializer.Deserialize(sr);

            return result;
        }

        /// <summary>
        /// Deserialize XML string into object type specified.
        /// </summary>
        /// <typeparam name="T">Object type to deserialize the XML into.</typeparam>
        /// <param name="xml">XML string to deserialize.</param>
        /// <param name="ignoreNamespace"></param>
        /// <returns>Deserialized object T</returns>
        public static async Task<T> DeserializeFromXmlAsync<T>(this string xml, bool ignoreNamespace = true) where T : class, new()
        {
            return await Task.Run(() => xml.DeserializeFromXml<T>(ignoreNamespace)).ConfigureAwait(false);
        }

        public static string SerializeToXml<T>(this T graph, bool ignoreNamespace = true) where T : class, new()
        {
            var xmlSerializer = GetXmlSerializer(graph.GetType());
            XmlSerializerNamespaces ns = ignoreNamespace ? _ignoredXmlSerializerNamespaces : _xmlSerializerNamespaces;

            var sb = new StringBuilder();

            using (var textWriter = new Utf8StringWriter(sb))
            using (var xmlWriter = XmlWriter.Create(textWriter, _xmlWriterSettings))
            {
                xmlSerializer.Serialize(xmlWriter, graph, ns);
                return sb.ToString();
            }
        }

        public static async Task<string> SerializeToXmlAsync<T>(this T xml, bool ignoreNamespace = true) where T : class, new()
        {
            return await Task.Run(() => SerializeToXml(xml, ignoreNamespace)).ConfigureAwait(false);
        }

        private static XmlSerializer GetXmlSerializer(Type type)
        {
            return XmlSerializers.GetOrAdd(type, new XmlSerializer(type));
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public Utf8StringWriter(StringBuilder sb) : base(sb)
        {
        }

        // Use UTF8 encoding but write no BOM
        public override Encoding Encoding
        {
            get { return new UTF8Encoding(false); }
        }
    }
}