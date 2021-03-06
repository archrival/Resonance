﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Resonance.Common
{
    public static class XmlExtensions
    {
        private static readonly XmlQualifiedName EmptyNamespace = new XmlQualifiedName(string.Empty, string.Empty);
        private static readonly XmlSerializerNamespaces IgnoredXmlSerializerNamespaces = new XmlSerializerNamespaces(new[] { EmptyNamespace });
        private static readonly XmlSerializerNamespaces XmlSerializerNamespaces = new XmlSerializerNamespaces();
        private static readonly Lazy<ConcurrentDictionary<Type, XmlSerializer>> XmlSerializersLazy = new Lazy<ConcurrentDictionary<Type, XmlSerializer>>();

        private static readonly XmlWriterSettings XmlWriterSettings = new XmlWriterSettings
        {
            Async = true,
            ConformanceLevel = ConformanceLevel.Document,
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = " ",
            NamespaceHandling = NamespaceHandling.OmitDuplicates
        };

        private static ConcurrentDictionary<Type, XmlSerializer> XmlSerializers => XmlSerializersLazy.Value;

        public static string SerializeToXml<T>(this T graph, bool ignoreNamespace = true) where T : class, new()
        {
            if (graph == null)
            {
                return null;
            }

            var xmlSerializer = GetXmlSerializer(graph.GetType());
            var xmlSerializerNamespaces = ignoreNamespace ? IgnoredXmlSerializerNamespaces : XmlSerializerNamespaces;

            var stringBuilder = new StringBuilder();

            using (var textWriter = new Utf8StringWriter(stringBuilder))
            using (var xmlWriter = XmlWriter.Create(textWriter, XmlWriterSettings))
            {
                xmlSerializer.Serialize(xmlWriter, graph, xmlSerializerNamespaces);
                return stringBuilder.ToString();
            }
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
        public override Encoding Encoding => new UTF8Encoding(false);
    }
}