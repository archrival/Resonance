using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Xml;
using System.Xml.Linq;

namespace Resonance.Common
{
    internal interface IXmlDeclaration : IXmlNode
    {
        string Encoding { get; set; }
        string Standalone { get; set; }
        string Version { get; }
    }

    internal interface IXmlDocument : IXmlNode
    {
        IXmlElement DocumentElement { get; }

        IXmlNode CreateAttribute(string name, string value);

        IXmlNode CreateAttribute(string qualifiedName, string namespaceUri, string value);

        IXmlNode CreateCDataSection(string data);

        IXmlNode CreateComment(string text);

        IXmlElement CreateElement(string elementName);

        IXmlElement CreateElement(string qualifiedName, string namespaceUri);

        IXmlNode CreateProcessingInstruction(string target, string data);

        IXmlNode CreateSignificantWhitespace(string text);

        IXmlNode CreateTextNode(string text);

        IXmlNode CreateWhitespace(string text);

        IXmlNode CreateXmlDeclaration(string version, string encoding, string standalone);

        IXmlNode CreateXmlDocumentType(string name, string publicId, string systemId, string internalSubset);
    }

    internal interface IXmlDocumentType : IXmlNode
    {
        string InternalSubset { get; }
        string Name { get; }
        string Public { get; }
        string System { get; }
    }

    internal interface IXmlElement : IXmlNode
    {
        bool IsEmpty { get; }

        string GetPrefixOfNamespace(string namespaceUri);

        void SetAttributeNode(IXmlNode attribute);
    }

    internal interface IXmlNode
    {
        List<IXmlNode> Attributes { get; }
        List<IXmlNode> ChildNodes { get; }
        string LocalName { get; }
        string NamespaceUri { get; }
        XmlNodeType NodeType { get; }
        IXmlNode ParentNode { get; }
        string Value { get; set; }
        object WrappedNode { get; }

        IXmlNode AppendChild(IXmlNode newChild);
    }

    public static class Utils
    {
        public static ArgumentOutOfRangeException CreateArgumentOutOfRangeException(string paramName, object actualValue, string message)
        {
            var message1 = message + Environment.NewLine + string.Format(CultureInfo.InvariantCulture, "Actual value was {0}.", actualValue);
            return new ArgumentOutOfRangeException(paramName, message1);
        }

        public static string GetLocalName(string qualifiedName)
        {
            GetQualifiedNameParts(qualifiedName, out var prefix, out var localName);
            return localName;
        }

        public static string GetPrefix(string qualifiedName)
        {
            GetQualifiedNameParts(qualifiedName, out var prefix, out var localName);
            return prefix;
        }

        public static void GetQualifiedNameParts(string qualifiedName, out string prefix, out string localName)
        {
            var length = qualifiedName.IndexOf(':');
            switch (length)
            {
                case -1:
                case 0:
                    prefix = null;
                    localName = qualifiedName;
                    break;

                default:
                    if (qualifiedName.Length - 1 != length)
                    {
                        prefix = qualifiedName.Substring(0, length);
                        localName = qualifiedName.Substring(length + 1);
                        break;
                    }
                    goto case -1;
            }
        }

        public static bool IsNullOrEmpty<T>(ICollection<T> collection)
        {
            if (collection != null)
                return collection.Count == 0;
            return true;
        }

        public static string ToDateTimeFormat(DateTimeKind kind)
        {
            switch (kind)
            {
                case DateTimeKind.Unspecified:
                    return "yyyy-MM-ddTHH:mm:ss.FFFFFFF";

                case DateTimeKind.Utc:
                    return "yyyy-MM-ddTHH:mm:ss.FFFFFFFZ";

                case DateTimeKind.Local:
                    return "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";

                default:
                    throw CreateArgumentOutOfRangeException(nameof(kind), kind, "Unexpected DateTimeKind value.");
            }
        }
    }

    /// <inheritdoc />
    /// <summary>Converts XML to and from JSON.</summary>
    public class XObjectConverter : JsonConverter
    {
        private Func<string, string, object> _getValue;

        /// <summary>
        /// Gets or sets the name of the root element to insert when deserializing to XML if the JSON structure has produces multiple root elements.
        /// </summary>
        /// <value>The name of the deserialize root element.</value>
        public string DeserializeRootElementName { get; set; }

        public Func<string, string, object> GetValue
        {
            get
            {
                if (_getValue == null)
                {
                    return (v, p) => v;
                }

                return _getValue;
            }
            set => _getValue = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to write the root JSON object.
        /// </summary>
        /// <value><c>true</c> if the JSON root object is omitted; otherwise, <c>false</c>.</value>
        public bool OmitRootObject { get; set; }

        public bool PrependOutput { get; set; }

        /// <summary>
        /// Gets or sets a flag to indicate whether to write the Json.NET array attribute.
        /// This attribute helps preserve arrays when converting the written XML back to JSON.
        /// </summary>
        /// <value><c>true</c> if the array attibute is written to the XML; otherwise, <c>false</c>.</value>
        public bool WriteArrayAttribute { get; set; }

        /// <inheritdoc />
        /// <summary>
        /// Determines whether this instance can convert the specified value type.
        /// </summary>
        /// <param name="valueType">Type of the value.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified value type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type valueType)
        {
            if (typeof(XObject).IsAssignableFrom(valueType))
            {
                return true;
            }

            if (typeof(XmlNode).IsAssignableFrom(valueType))
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        /// <exception cref="T:Newtonsoft.Json.JsonSerializationException"></exception>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            var manager = new XmlNamespaceManager(new NameTable());
            IXmlDocument document = null;
            IXmlNode currentNode = null;
            if (typeof(XObject).IsAssignableFrom(objectType))
            {
                if (objectType != typeof(XDocument) && objectType != typeof(XElement))
                    throw new JsonSerializationException("XmlNodeConverter only supports deserializing XDocument or XElement.");
                document = new XDocumentWrapper(new XDocument());
                currentNode = document;
            }
            if (document == null || currentNode == null)
                throw new JsonSerializationException("Unexpected type when converting XML: " + objectType);
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException("XmlNodeConverter can only convert JSON that begins with an object.");
            if (!string.IsNullOrEmpty(DeserializeRootElementName))
            {
                ReadElement(reader, document, currentNode, DeserializeRootElementName, manager);
            }
            else
            {
                reader.Read();
                DeserializeNode(reader, document, manager, currentNode);
            }
            if (objectType != typeof(XElement))
                return document.WrappedNode;
            var wrappedNode = (XElement)document.DocumentElement.WrappedNode;
            wrappedNode.Remove();
            return wrappedNode;
        }

        /// <inheritdoc />
        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <param name="value">The value.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var node = WrapXml(value);
            var manager = new XmlNamespaceManager(new NameTable());
            PushParentNamespaces(node, manager);
            if (!OmitRootObject)
                writer.WriteStartObject();
            SerializeNode(writer, node, manager, !OmitRootObject);
            if (OmitRootObject)
                return;
            writer.WriteEndObject();
        }

        private static void AddAttribute(JsonReader reader, IXmlDocument document, IXmlNode currentNode, string attributeName, XmlNamespaceManager manager, string attributePrefix)
        {
            var str1 = XmlConvert.EncodeName(attributeName);
            var str2 = reader.Value.ToString();
            var attribute = !string.IsNullOrEmpty(attributePrefix) ? document.CreateAttribute(str1, manager.LookupNamespace(attributePrefix), str2) : document.CreateAttribute(str1, str2);
            ((IXmlElement)currentNode).SetAttributeNode(attribute);
        }

        private static void AddJsonArrayAttribute(IXmlElement element, IXmlDocument document)
        {
            element.SetAttributeNode(document.CreateAttribute("json:Array", "http://james.newtonking.com/projects/json", "true"));
            if (!(element is XElementWrapper) || element.GetPrefixOfNamespace("http://james.newtonking.com/projects/json") != null)
                return;
            element.SetAttributeNode(document.CreateAttribute("xmlns:json", "http://www.w3.org/2000/xmlns/", "http://james.newtonking.com/projects/json"));
        }

        private static bool AllSameName(IXmlNode node)
        {
            foreach (var childNode in node.ChildNodes)
            {
                if (childNode.LocalName != node.LocalName)
                    return false;
            }
            return true;
        }

        private static string ConvertTokenToXmlValue(JsonReader reader)
        {
            if (reader.TokenType == JsonToken.String)
            {
                return reader.Value?.ToString();
            }
            if (reader.TokenType == JsonToken.Integer)
            {
                if (reader.Value is BigInteger integer)
                    return integer.ToString(CultureInfo.InvariantCulture);
                return XmlConvert.ToString(Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture));
            }
            if (reader.TokenType == JsonToken.Float)
            {
                if (reader.Value is decimal @decimal)
                    return XmlConvert.ToString(@decimal);
                if (reader.Value is float f)
                    return XmlConvert.ToString(f);
                return XmlConvert.ToString(Convert.ToDouble(reader.Value, CultureInfo.InvariantCulture));
            }
            if (reader.TokenType == JsonToken.Boolean)
                return XmlConvert.ToString(Convert.ToBoolean(reader.Value, CultureInfo.InvariantCulture));
            if (reader.TokenType == JsonToken.Date)
            {
                if (reader.Value is DateTimeOffset offset)
                    return XmlConvert.ToString(offset);
                var dateTime = Convert.ToDateTime(reader.Value, CultureInfo.InvariantCulture);
                return XmlConvert.ToString(dateTime, Utils.ToDateTimeFormat(dateTime.Kind));
            }
            if (reader.TokenType == JsonToken.Null)
                return null;
            throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, "Cannot get an XML string value from token type '{0}'.", reader.TokenType));
        }

        private static void CreateDocumentType(JsonReader reader, IXmlDocument document, IXmlNode currentNode)
        {
            string name = null;
            string publicId = null;
            string systemId = null;
            string internalSubset = null;
            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
            {
                var str = reader.Value.ToString();
                if (str != "@name")
                {
                    if (str != "@public")
                    {
                        if (str != "@system")
                        {
                            if (str != "@internalSubset")
                                throw new JsonSerializationException("Unexpected property name encountered while deserializing XmlDeclaration: " + reader.Value);
                            reader.Read();
                            internalSubset = reader.Value.ToString();
                        }
                        else
                        {
                            reader.Read();
                            systemId = reader.Value.ToString();
                        }
                    }
                    else
                    {
                        reader.Read();
                        publicId = reader.Value.ToString();
                    }
                }
                else
                {
                    reader.Read();
                    name = reader.Value.ToString();
                }
            }
            var xmlDocumentType = document.CreateXmlDocumentType(name, publicId, systemId, internalSubset);
            currentNode.AppendChild(xmlDocumentType);
        }

        private static IXmlElement CreateElement(string elementName, IXmlDocument document, string elementPrefix, XmlNamespaceManager manager)
        {
            var str = XmlConvert.EncodeName(elementName);
            var namespaceUri = string.IsNullOrEmpty(elementPrefix) ? manager.DefaultNamespace : manager.LookupNamespace(elementPrefix);
            if (string.IsNullOrEmpty(namespaceUri))
                return document.CreateElement(str);
            return document.CreateElement(str, namespaceUri);
        }

        private static void CreateInstruction(JsonReader reader, IXmlDocument document, IXmlNode currentNode, string propertyName)
        {
            if (propertyName == "?xml")
            {
                string version = null;
                string encoding = null;
                string standalone = null;
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    var str = reader.Value.ToString();
                    if (str != "@version")
                    {
                        if (str != "@encoding")
                        {
                            if (str != "@standalone")
                                throw new JsonSerializationException("Unexpected property name encountered while deserializing XmlDeclaration: " + reader.Value);
                            reader.Read();
                            standalone = reader.Value.ToString();
                        }
                        else
                        {
                            reader.Read();
                            encoding = reader.Value.ToString();
                        }
                    }
                    else
                    {
                        reader.Read();
                        version = reader.Value.ToString();
                    }
                }
                var xmlDeclaration = document.CreateXmlDeclaration(version, encoding, standalone);
                currentNode.AppendChild(xmlDeclaration);
            }
            else
            {
                var processingInstruction = document.CreateProcessingInstruction(propertyName.Substring(1), reader.Value.ToString());
                currentNode.AppendChild(processingInstruction);
            }
        }

        private static bool IsArray(IXmlNode node)
        {
            if (node.Attributes == null)
            {
                return false;
            }

            foreach (var attribute in node.Attributes)
            {
                if (attribute.LocalName == "Array" && attribute.NamespaceUri == "http://james.newtonking.com/projects/json")
                    return XmlConvert.ToBoolean(attribute.Value);
            }

            return false;
        }

        /// <summary>Checks if the attributeName is a namespace attribute.</summary>
        /// <param name="attributeName">Attribute name to test.</param>
        /// <param name="prefix">The attribute name prefix if it has one, otherwise an empty string.</param>
        /// <returns><c>true</c> if attribute name is for a namespace attribute, otherwise <c>false</c>.</returns>
        private static bool IsNamespaceAttribute(string attributeName, out string prefix)
        {
            if (attributeName.StartsWith("xmlns", StringComparison.Ordinal))
            {
                if (attributeName.Length == 5)
                {
                    prefix = string.Empty;
                    return true;
                }
                if (attributeName[5] == 58)
                {
                    prefix = attributeName.Substring(6, attributeName.Length - 6);
                    return true;
                }
            }
            prefix = null;
            return false;
        }

        private static bool IsPrimitiveToken(JsonToken token)
        {
            switch (token)
            {
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Boolean:
                case JsonToken.Null:
                case JsonToken.Undefined:
                case JsonToken.Date:
                case JsonToken.Bytes:
                    return true;

                default:
                    return false;
            }
        }

        private static void PushParentNamespaces(IXmlNode node, XmlNamespaceManager manager)
        {
            List<IXmlNode> xmlNodeList = null;
            var xmlNode1 = node;
            while ((xmlNode1 = xmlNode1.ParentNode) != null)
            {
                if (xmlNode1.NodeType == XmlNodeType.Element)
                {
                    xmlNodeList = xmlNodeList ?? new List<IXmlNode>();
                    xmlNodeList.Add(xmlNode1);
                }
            }
            if (xmlNodeList == null)
                return;
            xmlNodeList.Reverse();
            foreach (var xmlNode2 in xmlNodeList)
            {
                manager.PushScope();
                foreach (var attribute in xmlNode2.Attributes)
                {
                    if (attribute.NamespaceUri == "http://www.w3.org/2000/xmlns/" && attribute.LocalName != "xmlns")
                        manager.AddNamespace(attribute.LocalName, attribute.Value);
                }
            }
        }

        private static Dictionary<string, string> ReadAttributeElements(JsonReader reader, XmlNamespaceManager manager)
        {
            var dictionary = new Dictionary<string, string>();
            var flag1 = false;
            var flag2 = false;
            if (reader.TokenType != JsonToken.String && reader.TokenType != JsonToken.Null && (reader.TokenType != JsonToken.Boolean && reader.TokenType != JsonToken.Integer) && (reader.TokenType != JsonToken.Float && reader.TokenType != JsonToken.Date && reader.TokenType != JsonToken.StartConstructor))
            {
                while (!flag1 && !flag2 && reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.PropertyName:
                            var str1 = reader.Value.ToString();
                            if (!string.IsNullOrEmpty(str1))
                            {
                                switch (str1[0])
                                {
                                    case '$':
                                        if (str1 == "$values" || str1 == "$id" || (str1 == "$ref" || str1 == "$type") || str1 == "$value")
                                        {
                                            var prefix = manager.LookupPrefix("http://james.newtonking.com/projects/json");
                                            if (prefix == null)
                                            {
                                                var nullable = new int?();
                                                while (manager.LookupNamespace("json" + nullable) != null)
                                                    nullable = nullable.GetValueOrDefault() + 1;
                                                prefix = "json" + nullable;
                                                dictionary.Add("xmlns:" + prefix, "http://james.newtonking.com/projects/json");
                                                manager.AddNamespace(prefix, "http://james.newtonking.com/projects/json");
                                            }
                                            if (str1 == "$values")
                                            {
                                                flag1 = true;
                                                continue;
                                            }
                                            var str2 = str1.Substring(1);
                                            reader.Read();
                                            if (!IsPrimitiveToken(reader.TokenType))
                                                throw new JsonSerializationException("Unexpected JsonToken: " + reader.TokenType);
                                            var str3 = reader.Value?.ToString();
                                            dictionary.Add(prefix + ":" + str2, str3);
                                            continue;
                                        }
                                        flag1 = true;
                                        continue;
                                    case '@':
                                        var str4 = str1.Substring(1);
                                        reader.Read();
                                        var xmlValue = ConvertTokenToXmlValue(reader);
                                        dictionary.Add(str4, xmlValue);

                                        if (IsNamespaceAttribute(str4, out var prefix1))
                                        {
                                            manager.AddNamespace(prefix1, xmlValue);
                                        }

                                        continue;
                                    default:
                                        flag1 = true;
                                        continue;
                                }
                            }
                            flag1 = true;
                            continue;
                        case JsonToken.Comment:
                            flag2 = true;
                            continue;
                        case JsonToken.EndObject:
                            flag2 = true;
                            continue;
                        default:
                            throw new JsonSerializationException("Unexpected JsonToken: " + reader.TokenType);
                    }
                }
            }
            return dictionary;
        }

        private static string ResolveFullName(IXmlNode node, XmlNamespaceManager manager)
        {
            var str = node.NamespaceUri == null || (node.LocalName == "xmlns" && node.NamespaceUri == "http://www.w3.org/2000/xmlns/") ? null : manager.LookupPrefix(node.NamespaceUri);
            if (!string.IsNullOrEmpty(str))
                return str + ":" + XmlConvert.DecodeName(node.LocalName);
            return XmlConvert.DecodeName(node.LocalName);
        }

        private static bool ValueAttributes(IEnumerable<IXmlNode> c)
        {
            foreach (var xmlNode in c)
            {
                if (xmlNode.NamespaceUri != "http://james.newtonking.com/projects/json")
                    return true;
            }
            return false;
        }

        private void CreateElement(JsonReader reader, IXmlDocument document, IXmlNode currentNode, string elementName, XmlNamespaceManager manager, string elementPrefix, Dictionary<string, string> attributeNameValues)
        {
            var element = CreateElement(elementName, document, elementPrefix, manager);
            currentNode.AppendChild(element);
            foreach (var attributeNameValue in attributeNameValues)
            {
                var str = XmlConvert.EncodeName(attributeNameValue.Key);
                var prefix = Utils.GetPrefix(attributeNameValue.Key);
                var attribute = !string.IsNullOrEmpty(prefix) ? document.CreateAttribute(str, manager.LookupNamespace(prefix) ?? string.Empty, attributeNameValue.Value) : document.CreateAttribute(str, attributeNameValue.Value);
                element.SetAttributeNode(attribute);
            }
            if (reader.TokenType == JsonToken.String || reader.TokenType == JsonToken.Integer || (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Boolean) || reader.TokenType == JsonToken.Date)
            {
                var xmlValue = ConvertTokenToXmlValue(reader);
                if (xmlValue == null)
                    return;
                element.AppendChild(document.CreateTextNode(xmlValue));
            }
            else
            {
                if (reader.TokenType == JsonToken.Null)
                    return;
                if (reader.TokenType != JsonToken.EndObject)
                {
                    manager.PushScope();
                    DeserializeNode(reader, document, manager, element);
                    manager.PopScope();
                }
                manager.RemoveNamespace(string.Empty, manager.DefaultNamespace);
            }
        }

        private void DeserializeNode(JsonReader reader, IXmlDocument document, XmlNamespaceManager manager, IXmlNode currentNode)
        {
            do
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartConstructor:
                        var propertyName1 = reader.Value.ToString();
                        while (reader.Read() && reader.TokenType != JsonToken.EndConstructor)
                            DeserializeValue(reader, document, manager, propertyName1, currentNode);
                        break;

                    case JsonToken.PropertyName:
                        if (currentNode.NodeType == XmlNodeType.Document && document.DocumentElement != null)
                            throw new JsonSerializationException("JSON root object has multiple properties. The root object must have a single property in order to create a valid XML document. Consider specifing a DeserializeRootElementName.");
                        var propertyName2 = reader.Value.ToString();
                        reader.Read();
                        if (reader.TokenType == JsonToken.StartArray)
                        {
                            var num = 0;
                            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                            {
                                DeserializeValue(reader, document, manager, propertyName2, currentNode);
                                ++num;
                            }
                            if (num == 1 && WriteArrayAttribute)
                            {
                                using (var enumerator = currentNode.ChildNodes.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        var current = enumerator.Current as IXmlElement;
                                        if (current?.LocalName == propertyName2)
                                        {
                                            AddJsonArrayAttribute(current, document);
                                            break;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                        DeserializeValue(reader, document, manager, propertyName2, currentNode);
                        break;

                    case JsonToken.Comment:
                        currentNode.AppendChild(document.CreateComment((string)reader.Value));
                        break;

                    case JsonToken.EndObject:
                        return;

                    case JsonToken.EndArray:
                        return;

                    default:
                        throw new JsonSerializationException("Unexpected JsonToken when deserializing node: " + reader.TokenType);
                }
            }
            while (reader.TokenType == JsonToken.PropertyName || reader.Read());
        }

        private void DeserializeValue(JsonReader reader, IXmlDocument document, XmlNamespaceManager manager, string propertyName, IXmlNode currentNode)
        {
            if (propertyName != "#text")
            {
                if (propertyName != "#cdata-section")
                {
                    if (propertyName != "#whitespace")
                    {
                        if (propertyName == "#significant-whitespace")
                            currentNode.AppendChild(document.CreateSignificantWhitespace(reader.Value.ToString()));
                        else if (!string.IsNullOrEmpty(propertyName) && propertyName[0] == 63)
                            CreateInstruction(reader, document, currentNode, propertyName);
                        else if (string.Equals(propertyName, "!DOCTYPE", StringComparison.OrdinalIgnoreCase))
                            CreateDocumentType(reader, document, currentNode);
                        else if (reader.TokenType == JsonToken.StartArray)
                            ReadArrayElements(reader, document, propertyName, currentNode, manager);
                        else
                            ReadElement(reader, document, currentNode, propertyName, manager);
                    }
                    else
                    {
                        currentNode.AppendChild(document.CreateWhitespace(reader.Value.ToString()));
                    }
                }
                else
                {
                    currentNode.AppendChild(document.CreateCDataSection(reader.Value.ToString()));
                }
            }
            else
            {
                currentNode.AppendChild(document.CreateTextNode(reader.Value.ToString()));
            }
        }

        private string GetPropertyName(IXmlNode node, XmlNamespaceManager manager)
        {
            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    return ResolveFullName(node, manager);

                case XmlNodeType.Attribute:
                    return (PrependOutput ? "@" : string.Empty) + ResolveFullName(node, manager);

                case XmlNodeType.Text:
                    return PrependOutput ? "#text" : "value";

                case XmlNodeType.CDATA:
                    return PrependOutput ? "#cdata-section" : "value";

                case XmlNodeType.ProcessingInstruction:
                    return (PrependOutput ? "?" : string.Empty) + ResolveFullName(node, manager);

                case XmlNodeType.Comment:
                    return PrependOutput ? "#comment" : string.Empty;

                case XmlNodeType.DocumentType:
                    return (PrependOutput ? "!" : string.Empty) + ResolveFullName(node, manager);

                case XmlNodeType.Whitespace:
                    return PrependOutput ? "#whitespace" : string.Empty;

                case XmlNodeType.SignificantWhitespace:
                    return PrependOutput ? "#significant-whitespace" : string.Empty;

                case XmlNodeType.XmlDeclaration:
                    return PrependOutput ? "?xml" : string.Empty;

                default:
                    throw new JsonSerializationException("Unexpected XmlNodeType when getting node name: " + node.NodeType);
            }
        }

        private void ReadArrayElements(JsonReader reader, IXmlDocument document, string propertyName, IXmlNode currentNode, XmlNamespaceManager manager)
        {
            var prefix = Utils.GetPrefix(propertyName);
            var element1 = CreateElement(propertyName, document, prefix, manager);
            currentNode.AppendChild(element1);
            var num = 0;
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                DeserializeValue(reader, document, manager, propertyName, element1);
                ++num;
            }
            if (WriteArrayAttribute)
                AddJsonArrayAttribute(element1, document);
            if (num != 1 || !WriteArrayAttribute)
                return;
            foreach (var childNode in element1.ChildNodes)
            {
                var element2 = childNode as IXmlElement;
                if (element2?.LocalName == propertyName)
                {
                    AddJsonArrayAttribute(element2, document);
                    break;
                }
            }
        }

        private void ReadElement(JsonReader reader, IXmlDocument document, IXmlNode currentNode, string propertyName, XmlNamespaceManager manager)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new JsonSerializationException("XmlNodeConverter cannot convert JSON with an empty property name to XML.");
            var attributeNameValues = ReadAttributeElements(reader, manager);
            var prefix1 = Utils.GetPrefix(propertyName);
            if (propertyName.StartsWith("@"))
            {
                var str = propertyName.Substring(1);
                var prefix2 = Utils.GetPrefix(str);
                AddAttribute(reader, document, currentNode, str, manager, prefix2);
            }
            else
            {
                if (propertyName.StartsWith("$"))
                {
                    if (propertyName != "$values")
                    {
                        if (propertyName == "$id" || propertyName == "$ref" || (propertyName == "$type" || propertyName == "$value"))
                        {
                            var attributeName = propertyName.Substring(1);
                            var attributePrefix = manager.LookupPrefix("http://james.newtonking.com/projects/json");
                            AddAttribute(reader, document, currentNode, attributeName, manager, attributePrefix);
                            return;
                        }
                    }
                    else
                    {
                        propertyName = propertyName.Substring(1);
                        var elementPrefix = manager.LookupPrefix("http://james.newtonking.com/projects/json");
                        CreateElement(reader, document, currentNode, propertyName, manager, elementPrefix, attributeNameValues);
                        return;
                    }
                }
                CreateElement(reader, document, currentNode, propertyName, manager, prefix1, attributeNameValues);
            }
        }

        private void SerializeGroupedNodes(JsonWriter writer, IXmlNode node, XmlNamespaceManager manager, bool writePropertyName)
        {
            var dictionary = new Dictionary<string, List<IXmlNode>>();
            foreach (var childNode in node.ChildNodes)
            {
                var propertyName = GetPropertyName(childNode, manager);
                if (!dictionary.TryGetValue(propertyName, out var xmlNodeList))
                {
                    xmlNodeList = new List<IXmlNode>();
                    dictionary.Add(propertyName, xmlNodeList);
                }
                xmlNodeList.Add(childNode);
            }

            foreach (var keyValuePair in dictionary)
            {
                var xmlNodeList = keyValuePair.Value;
                if (xmlNodeList.Count == 1 && !IsArray(xmlNodeList[0]))
                {
                    SerializeNode(writer, xmlNodeList[0], manager, writePropertyName);
                }
                else
                {
                    var key = keyValuePair.Key;
                    if (writePropertyName)
                        writer.WritePropertyName(key);
                    writer.WriteStartArray();
                    foreach (var t in xmlNodeList)
                        SerializeNode(writer, t, manager, false);
                    writer.WriteEndArray();
                }
            }
        }

        private void SerializeNode(JsonWriter writer, IXmlNode node, XmlNamespaceManager manager, bool writePropertyName)
        {
            switch (node.NodeType)
            {
                case XmlNodeType.Element:
                    if (IsArray(node) && AllSameName(node) && node.ChildNodes.Count > 0)
                    {
                        SerializeGroupedNodes(writer, node, manager, false);
                        break;
                    }
                    manager.PushScope();
                    foreach (var attribute in node.Attributes)
                    {
                        if (attribute.NamespaceUri == "http://www.w3.org/2000/xmlns/")
                        {
                            var prefix = attribute.LocalName != "xmlns" ? XmlConvert.DecodeName(attribute.LocalName) : string.Empty;
                            var uri = attribute.Value;
                            manager.AddNamespace(prefix, uri);
                        }
                    }

                    if (writePropertyName)
                        writer.WritePropertyName(GetPropertyName(node, manager));
                    if (!ValueAttributes(node.Attributes) && node.ChildNodes.Count == 1
                        && node.ChildNodes[0].NodeType == XmlNodeType.Text)
                    {
                        writer.WriteValue(GetValue(node.ChildNodes[0].Value, GetPropertyName(node, manager)));
                    }
                    else if (node.ChildNodes.Count == 0 && Utils.IsNullOrEmpty(node.Attributes))
                    {
                        if (((IXmlElement)node).IsEmpty)
                            writer.WriteNull();
                        else
                            writer.WriteValue(string.Empty);
                    }
                    else
                    {
                        writer.WriteStartObject();
                        foreach (var t in node.Attributes)
                            SerializeNode(writer, t, manager, true);
                        SerializeGroupedNodes(writer, node, manager, true);
                        writer.WriteEndObject();
                    }
                    manager.PopScope();
                    break;

                case XmlNodeType.Attribute:
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    if ((node.NamespaceUri == "http://www.w3.org/2000/xmlns/" && node.Value == "http://james.newtonking.com/projects/json") || (node.NamespaceUri == "http://james.newtonking.com/projects/json" && node.LocalName == "Array"))
                        break;

                    var propertyName = GetPropertyName(node, manager);
                    if (writePropertyName)
                        writer.WritePropertyName(propertyName);

                    writer.WriteValue(GetValue(node.Value, propertyName));

                    break;

                case XmlNodeType.Comment:
                    if (!writePropertyName)
                        break;
                    writer.WriteComment(node.Value);
                    break;

                case XmlNodeType.Document:
                case XmlNodeType.DocumentFragment:
                    SerializeGroupedNodes(writer, node, manager, writePropertyName);
                    break;

                case XmlNodeType.DocumentType:
                    var xmlDocumentType = (IXmlDocumentType)node;
                    writer.WritePropertyName(GetPropertyName(node, manager));
                    writer.WriteStartObject();
                    if (!string.IsNullOrEmpty(xmlDocumentType.Name))
                    {
                        writer.WritePropertyName("@name");
                        writer.WriteValue(xmlDocumentType.Name);
                    }
                    if (!string.IsNullOrEmpty(xmlDocumentType.Public))
                    {
                        writer.WritePropertyName("@public");
                        writer.WriteValue(xmlDocumentType.Public);
                    }
                    if (!string.IsNullOrEmpty(xmlDocumentType.System))
                    {
                        writer.WritePropertyName("@system");
                        writer.WriteValue(xmlDocumentType.System);
                    }
                    if (!string.IsNullOrEmpty(xmlDocumentType.InternalSubset))
                    {
                        writer.WritePropertyName("@internalSubset");
                        writer.WriteValue(xmlDocumentType.InternalSubset);
                    }
                    writer.WriteEndObject();
                    break;

                case XmlNodeType.XmlDeclaration:
                    var xmlDeclaration = (IXmlDeclaration)node;
                    writer.WritePropertyName(GetPropertyName(node, manager));
                    writer.WriteStartObject();
                    if (!string.IsNullOrEmpty(xmlDeclaration.Version))
                    {
                        writer.WritePropertyName("@version");
                        writer.WriteValue(xmlDeclaration.Version);
                    }
                    if (!string.IsNullOrEmpty(xmlDeclaration.Encoding))
                    {
                        writer.WritePropertyName("@encoding");
                        writer.WriteValue(xmlDeclaration.Encoding);
                    }
                    if (!string.IsNullOrEmpty(xmlDeclaration.Standalone))
                    {
                        writer.WritePropertyName("@standalone");
                        writer.WriteValue(xmlDeclaration.Standalone);
                    }
                    writer.WriteEndObject();
                    break;

                default:
                    throw new JsonSerializationException("Unexpected XmlNodeType when serializing nodes: " + node.NodeType);
            }
        }

        private IXmlNode WrapXml(object value)
        {
            if (value is XObject o)
            {
                return XContainerWrapper.WrapNode(o);
            }

            throw new ArgumentException("Value must be an XML object.", nameof(value));
        }
    }

    internal class XAttributeWrapper : XObjectWrapper
    {
        public XAttributeWrapper(XAttribute attribute)
            : base(attribute)
        {
        }

        public override string LocalName => Attribute.Name.LocalName;

        public override string NamespaceUri => Attribute.Name.NamespaceName;

        public override IXmlNode ParentNode
        {
            get
            {
                if (Attribute.Parent == null)
                {
                    return null;
                }

                return XContainerWrapper.WrapNode(Attribute.Parent);
            }
        }

        public override string Value
        {
            get => Attribute.Value;
            set => Attribute.Value = value;
        }

        private XAttribute Attribute => (XAttribute)WrappedNode;
    }

    internal class XCommentWrapper : XObjectWrapper
    {
        public XCommentWrapper(XComment text)
            : base(text)
        {
        }

        public override IXmlNode ParentNode
        {
            get
            {
                if (Text.Parent == null)
                {
                    return null;
                }

                return XContainerWrapper.WrapNode(Text.Parent);
            }
        }

        public override string Value
        {
            get => Text.Value;
            set => Text.Value = value;
        }

        private XComment Text => (XComment)WrappedNode;
    }

    internal class XContainerWrapper : XObjectWrapper
    {
        private List<IXmlNode> _childNodes;

        protected XContainerWrapper(XContainer container)
            : base(container)
        {
        }

        public override List<IXmlNode> ChildNodes
        {
            get
            {
                // childnodes is read multiple times
                // cache results to prevent multiple reads which kills perf in large documents
                if (_childNodes == null)
                {
                    _childNodes = new List<IXmlNode>();
                    foreach (var node in Container.Nodes())
                    {
                        _childNodes.Add(WrapNode(node));
                    }
                }

                return _childNodes;
            }
        }

        public override IXmlNode ParentNode
        {
            get
            {
                if (Container.Parent == null)
                {
                    return null;
                }

                return WrapNode(Container.Parent);
            }
        }

        private XContainer Container => (XContainer)WrappedNode;

        public override IXmlNode AppendChild(IXmlNode newChild)
        {
            Container.Add(newChild.WrappedNode);
            _childNodes = null;

            return newChild;
        }

        internal static IXmlNode WrapNode(XObject node)
        {
            if (node is XDocument document)
            {
                return new XDocumentWrapper(document);
            }
            if (node is XElement element)
            {
                return new XElementWrapper(element);
            }
            if (node is XContainer container)
            {
                return new XContainerWrapper(container);
            }
            if (node is XProcessingInstruction instruction)
            {
                return new XProcessingInstructionWrapper(instruction);
            }
            if (node is XText text)
            {
                return new XTextWrapper(text);
            }
            if (node is XComment comment)
            {
                return new XCommentWrapper(comment);
            }
            if (node is XAttribute attribute)
            {
                return new XAttributeWrapper(attribute);
            }
            if (node is XDocumentType type)
            {
                return new XDocumentTypeWrapper(type);
            }
            return new XObjectWrapper(node);
        }
    }

    internal class XDeclarationWrapper : XObjectWrapper, IXmlDeclaration
    {
        public XDeclarationWrapper(XDeclaration declaration)
            : base(null)
        {
            Declaration = declaration;
        }

        public string Encoding
        {
            get => Declaration.Encoding;
            set => Declaration.Encoding = value;
        }

        public override XmlNodeType NodeType => XmlNodeType.XmlDeclaration;

        public string Standalone
        {
            get => Declaration.Standalone;
            set => Declaration.Standalone = value;
        }

        public string Version => Declaration.Version;

        internal XDeclaration Declaration { get; }
    }

    internal class XDocumentTypeWrapper : XObjectWrapper, IXmlDocumentType
    {
        private readonly XDocumentType _documentType;

        public XDocumentTypeWrapper(XDocumentType documentType)
            : base(documentType)
        {
            _documentType = documentType;
        }

        public string InternalSubset => _documentType.InternalSubset;

        public override string LocalName => "DOCTYPE";

        public string Name => _documentType.Name;

        public string Public => _documentType.PublicId;

        public string System => _documentType.SystemId;
    }

    internal class XDocumentWrapper : XContainerWrapper, IXmlDocument
    {
        public XDocumentWrapper(XDocument document)
            : base(document)
        {
        }

        public override List<IXmlNode> ChildNodes
        {
            get
            {
                var childNodes = base.ChildNodes;

                if (Document.Declaration != null && childNodes[0].NodeType != XmlNodeType.XmlDeclaration)
                {
                    childNodes.Insert(0, new XDeclarationWrapper(Document.Declaration));
                }

                return childNodes;
            }
        }

        public IXmlElement DocumentElement
        {
            get
            {
                if (Document.Root == null)
                {
                    return null;
                }

                return new XElementWrapper(Document.Root);
            }
        }

        private XDocument Document => (XDocument)WrappedNode;

        public override IXmlNode AppendChild(IXmlNode newChild)
        {
            if (newChild is XDeclarationWrapper declarationWrapper)
            {
                Document.Declaration = declarationWrapper.Declaration;
                return declarationWrapper;
            }
            return base.AppendChild(newChild);
        }

        public IXmlNode CreateAttribute(string name, string value)
        {
            return new XAttributeWrapper(new XAttribute(name, value));
        }

        public IXmlNode CreateAttribute(string qualifiedName, string namespaceUri, string value)
        {
            var localName = Utils.GetLocalName(qualifiedName);
            return new XAttributeWrapper(new XAttribute(XName.Get(localName, namespaceUri), value));
        }

        public IXmlNode CreateCDataSection(string data)
        {
            return new XObjectWrapper(new XCData(data));
        }

        public IXmlNode CreateComment(string text)
        {
            return new XObjectWrapper(new XComment(text));
        }

        public IXmlElement CreateElement(string elementName)
        {
            return new XElementWrapper(new XElement(elementName));
        }

        public IXmlElement CreateElement(string qualifiedName, string namespaceUri)
        {
            var localName = Utils.GetLocalName(qualifiedName);
            return new XElementWrapper(new XElement(XName.Get(localName, namespaceUri)));
        }

        public IXmlNode CreateProcessingInstruction(string target, string data)
        {
            return new XProcessingInstructionWrapper(new XProcessingInstruction(target, data));
        }

        public IXmlNode CreateSignificantWhitespace(string text)
        {
            return new XObjectWrapper(new XText(text));
        }

        public IXmlNode CreateTextNode(string text)
        {
            return new XObjectWrapper(new XText(text));
        }

        public IXmlNode CreateWhitespace(string text)
        {
            return new XObjectWrapper(new XText(text));
        }

        public IXmlNode CreateXmlDeclaration(string version, string encoding, string standalone)
        {
            return new XDeclarationWrapper(new XDeclaration(version, encoding, standalone));
        }

        public IXmlNode CreateXmlDocumentType(string name, string publicId, string systemId, string internalSubset)
        {
            return new XDocumentTypeWrapper(new XDocumentType(name, publicId, systemId, internalSubset));
        }
    }

    internal class XElementWrapper : XContainerWrapper, IXmlElement
    {
        private List<IXmlNode> _attributes;

        public XElementWrapper(XElement element)
            : base(element)
        {
        }

        public override List<IXmlNode> Attributes
        {
            get
            {
                // attributes is read multiple times
                // cache results to prevent multiple reads which kills perf in large documents
                if (_attributes == null)
                {
                    _attributes = new List<IXmlNode>();
                    foreach (var attribute in Element.Attributes())
                    {
                        _attributes.Add(new XAttributeWrapper(attribute));
                    }

                    // ensure elements created with a namespace but no namespace attribute are converted correctly
                    // e.g. new XElement("{http://example.com}MyElement");
                    var namespaceUri = NamespaceUri;
                    if (!string.IsNullOrEmpty(namespaceUri) && namespaceUri != ParentNode?.NamespaceUri)
                    {
                        if (string.IsNullOrEmpty(GetPrefixOfNamespace(namespaceUri)))
                        {
                            var namespaceDeclared = false;
                            foreach (var attribute in _attributes)
                            {
                                if (attribute.LocalName == "xmlns" && string.IsNullOrEmpty(attribute.NamespaceUri) && attribute.Value == namespaceUri)
                                {
                                    namespaceDeclared = true;
                                }
                            }

                            if (!namespaceDeclared)
                            {
                                _attributes.Insert(0, new XAttributeWrapper(new XAttribute("xmlns", namespaceUri)));
                            }
                        }
                    }
                }

                return _attributes;
            }
        }

        public bool IsEmpty => Element.IsEmpty;

        public override string LocalName => Element.Name.LocalName;

        public override string NamespaceUri => Element.Name.NamespaceName;

        public override string Value
        {
            get => Element.Value;
            set => Element.Value = value;
        }

        private XElement Element => (XElement)WrappedNode;

        public override IXmlNode AppendChild(IXmlNode newChild)
        {
            var result = base.AppendChild(newChild);
            _attributes = null;
            return result;
        }

        public string GetPrefixOfNamespace(string namespaceUri)
        {
            return Element.GetPrefixOfNamespace(namespaceUri);
        }

        public void SetAttributeNode(IXmlNode attribute)
        {
            var wrapper = (XObjectWrapper)attribute;
            Element.Add(wrapper.WrappedNode);
            _attributes = null;
        }
    }

    internal class XObjectWrapper : IXmlNode
    {
        private static readonly List<IXmlNode> EmptyChildNodes = new List<IXmlNode>();
        private readonly XObject _xmlObject;

        public XObjectWrapper(XObject xmlObject)
        {
            _xmlObject = xmlObject;
        }

        public virtual List<IXmlNode> Attributes => null;

        public virtual List<IXmlNode> ChildNodes => EmptyChildNodes;

        public virtual string LocalName => null;

        public virtual string NamespaceUri => null;

        public virtual XmlNodeType NodeType => _xmlObject.NodeType;

        public virtual IXmlNode ParentNode => null;

        public virtual string Value
        {
            get => null;
            set => throw new InvalidOperationException();
        }

        public object WrappedNode => _xmlObject;

        public virtual IXmlNode AppendChild(IXmlNode newChild)
        {
            throw new InvalidOperationException();
        }
    }

    internal class XProcessingInstructionWrapper : XObjectWrapper
    {
        public XProcessingInstructionWrapper(XProcessingInstruction processingInstruction)
            : base(processingInstruction)
        {
        }

        public override string LocalName => ProcessingInstruction.Target;

        public override string Value
        {
            get => ProcessingInstruction.Data;
            set => ProcessingInstruction.Data = value;
        }

        private XProcessingInstruction ProcessingInstruction => (XProcessingInstruction)WrappedNode;
    }

    internal class XTextWrapper : XObjectWrapper
    {
        public XTextWrapper(XText text)
            : base(text)
        {
        }

        public override IXmlNode ParentNode
        {
            get
            {
                if (Text.Parent == null)
                {
                    return null;
                }

                return XContainerWrapper.WrapNode(Text.Parent);
            }
        }

        public override string Value
        {
            get => Text.Value;
            set => Text.Value = value;
        }

        private XText Text => (XText)WrappedNode;
    }
}