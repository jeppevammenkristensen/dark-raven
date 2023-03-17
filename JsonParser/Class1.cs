using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Schema;
using Pluralize.NET.Core;

namespace JsonParsing
{
    public class PathId
    {
        private readonly string[] _path;

        public string Id => string.Join(".", _path);

        public PathId(params string[] path)
        {
            if (path == null || !path.Any())
                throw new InvalidOperationException("Must be called with at least 1 parameter");

            _path = path;
        }

        public PathId CreateSubpath(string path)
        {
            return new PathId(_path.Concat(new[] {path}).ToArray());
        }

        public string Last => _path.Last();

        public int Depth => _path.Length;


        public override string ToString()
        {
            return Id;
        }

        #region Equality

        protected bool Equals(PathId other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PathId) obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion


        public PathId GetParent()
        {
            if (Depth == 1) throw new InvalidOperationException("Cannot get parent as this is the upper level");

            return new PathId(_path[..^1]);
        }
    }

    public class SchemaVisitor
    {
        private readonly StringBuilder _builder;

        public SchemaVisitor(StringBuilder builder)
        {
            _builder = builder;
        }

        public void VisitDefinition(JsonDefinition definition)
        {
            _builder.AppendLine($"public class {definition.Path!.Last}");
            _builder.AppendLine("{");

            foreach (var property in definition.Properties)
            {
                VisitProperties(property.Value);
            }
            

            _builder.AppendLine("}");
            _builder.AppendLine();


        }

        public void VisitProperties(JsonProperty property)
        {
            _builder.Append("   public ");
            VisitPropertyType(property);
            if (property.CanBeNull)
            {
                _builder.Append("?");
            }

            _builder.Append(" ");
            _builder.Append(property.SuggestedName);
            _builder.AppendLine(""" {get; set;}""");
        }

        private void VisitPropertyType(JsonProperty property)
        {
            switch (property.Type)
            {
                case JsonObjectType.None:
                    _builder.Append("object");
                    break;
                case JsonObjectType.Array:
                    _builder.Append(value: $"System.Collections.Generic.List<{property.SuggestedName}>");
                    break;
                case JsonObjectType.Boolean:
                    _builder.Append(value: $"bool");
                    break;
                case JsonObjectType.Integer:
                    _builder.Append("int");
                    break;
                case JsonObjectType.Null:
                    _builder.Append("object");
                    break;
                case JsonObjectType.Number:
                    _builder.Append("double");
                    break;
                case JsonObjectType.Object:
                    _builder.Append(property.SuggestedName);
                    break;
                case JsonObjectType.String:
                    _builder.Append("string");
                    break;
                case JsonObjectType.File:
                    _builder.Append("byte[]");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Visit(JsonDefinition definition)
        {
            foreach (var definitionPathIdDefinition in definition.PathIdDefinitions)
            {
                VisitDefinition(definitionPathIdDefinition.Value);
            }
        }

        public override string ToString() => _builder.ToString();

    }


    public class SchemaConverter
    {
        public string Convert(JsonDefinition definition)
        {
            var visitor = new SchemaVisitor(new StringBuilder());
            visitor.Visit(definition);
            return visitor.ToString();

        }

    }
    

    public class Parser
    {
        private Pluralizer _pluralizer = new Pluralizer();

        public Parser()
        {
            RootName = "Anonymous";
        }

        public string RootName { get; private set; }

        public JsonDefinition? Generate(string json)
        {
            JToken? token = JsonConvert.DeserializeObject<JToken>(json, new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            });

            if (token == null)
            {
                return null;
            }

            var schema = new JsonDefinition();
            schema.Path = new PathId(RootName);

            Generate(token, schema, ref schema, new PathId(RootName));
            schema.Process();
            
            return schema;
        }

        private void Generate(JToken token, JsonDefinition definition, ref JsonDefinition rootDefinition, PathId path)
        {
            // Logic to catch if we about to traverse a different kind of type
            GenerateWithoutReference(token, definition, rootDefinition, path);
        }

        private void AddSchemaDefinition(JsonDefinition rootDefinition, JsonDefinition definition, string suggestedName)
        {
            if (string.IsNullOrEmpty(suggestedName) || rootDefinition.Definitions.ContainsKey(suggestedName))
            {
                rootDefinition.Definitions["Anonymous" + (rootDefinition.Definitions.Count + 1)] = definition;
            }
            else
            {
                rootDefinition.Definitions[suggestedName] = definition;
            }
        }

        private void GenerateWithoutReference(JToken? token, JsonDefinition definition, JsonDefinition rootDefinition, PathId path)
        {
            if (token == null)
            {
                return;
            }

            definition.Path = path;

            switch (token.Type)
            {
                case JTokenType.Object:
                    GenerateObject((JObject)token, ref definition, ref rootDefinition, path);
                    break;

                case JTokenType.Array:
                    GenerateArray(token, ref definition, rootDefinition, path);
                    break;

                case JTokenType.Date:
                    definition.Type = JsonObjectType.String;
                    definition.Format = token.Value<DateTime>() == token.Value<DateTime>().Date
                        ? JsonFormatStrings.Date
                        : JsonFormatStrings.DateTime;
                    break;

                case JTokenType.String:
                    definition.Type = JsonObjectType.String;
                    break;

                case JTokenType.Boolean:
                    definition.Type = JsonObjectType.Boolean;
                    break;

                case JTokenType.Integer:
                    definition.Type = JsonObjectType.Integer;
                    break;

                case JTokenType.Float:
                    definition.Type = JsonObjectType.Number;
                    break;

                case JTokenType.Bytes:
                    definition.Type = JsonObjectType.String;
                    definition.Format = JsonFormatStrings.Byte;
                    break;
                    
                case JTokenType.TimeSpan:
                    definition.Type = JsonObjectType.String;
                    definition.Format = JsonFormatStrings.Duration;
                    break;

                case JTokenType.Guid:
                    definition.Type = JsonObjectType.String;
                    definition.Format = JsonFormatStrings.Guid;
                    break;

                case JTokenType.Uri:
                    definition.Type = JsonObjectType.String;
                    definition.Format = JsonFormatStrings.Uri;
                    break;
            }

            if (definition.Type == JsonObjectType.String && token.Value<string>() is {} stringValue)
            {

                if (Regex.IsMatch(stringValue, "^[0-2][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]$"))
                {
                    definition.Format = JsonFormatStrings.Date;
                }

                if (Regex.IsMatch(stringValue, "^[0-2][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9] [0-9][0-9]:[0-9][0-9](:[0-9][0-9])?$"))
                {
                    definition.Format = JsonFormatStrings.DateTime;
                }

                if (Regex.IsMatch(stringValue, "^[0-9][0-9]:[0-9][0-9](:[0-9][0-9])?$"))
                {
                    definition.Format = JsonFormatStrings.Duration;
                }
            }
        }

        private void GenerateArray(JToken token, ref JsonDefinition definition, JsonDefinition rootDefinition, PathId path)
        {
            definition.Type = JsonObjectType.Array;

            var itemSchemas = ((JArray)token).Take(500).Select(item =>
            {
                var itemSchema = new JsonDefinition();
                GenerateWithoutReference(item, itemSchema, rootDefinition, path);
                return itemSchema;
            }).ToList();

            definition.Data.AddRange(itemSchemas);
        }

        private void MergeAndAssignItemSchemas(JsonDefinition rootDefinition, JsonDefinition definition, List<JsonDefinition> itemSchemas, string suggestedName)
        {
            var firstItemSchema = itemSchemas.First();
            var itemSchema = new JsonDefinition
            {
                Type = firstItemSchema.Type
            };

            if (firstItemSchema.Type == JsonObjectType.Object)
            {
                foreach (var property in itemSchemas.SelectMany(s => s.Properties).GroupBy(p => p.Key))
                {
                    itemSchema.Properties[property.Key] = property.First().Value;
                    itemSchema.Properties[property.Key].CanBeNull = property.Count() != itemSchemas.Count || property.Any(x => x.Value.Type == JsonObjectType.Null);
                }
            }

            AddSchemaDefinition(rootDefinition, itemSchema, suggestedName);
            definition.Item = new JsonDefinition { Reference = itemSchema };
        }

        private void GenerateObject(JObject jObject, ref JsonDefinition definition, ref JsonDefinition rootDefinition,
            PathId pathId)
        {
            definition.Type = JsonObjectType.Object;
            
            foreach (var jProperty in jObject.Properties())
            {
                var propertySchema = new JsonProperty();
                var propertyName = jProperty.Value.Type == JTokenType.Array
                    ? _pluralizer.Singularize(jProperty.Name)
                    : jProperty.Name;

                var suggestedName = propertyName.ConvertToUpperCamelCase();

                Generate(jProperty.Value, propertySchema, ref rootDefinition, pathId.CreateSubpath(suggestedName));

                propertySchema.Name = jProperty.Name;
                propertySchema.SuggestedName = suggestedName;
                definition.Properties[jProperty.Name] = propertySchema;
                if (propertySchema.Type is JsonObjectType.Object or JsonObjectType.Array)
                {
                    definition.Data.Add(propertySchema);
                }
            }
        }
    }

     public static class JsonFormatStrings
    {
        /// <summary>Format for a <see cref="System.DateTime"/>. </summary>
        public const string DateTime = "date-time";

        /// <summary>Non-standard Format for a duration (time span)<see cref="TimeSpan"/>. </summary>
        public const string TimeSpan = "time-span";

        /// <summary>Format for a duration (time span) as of 2019-09 <see cref="TimeSpan"/>. </summary>
        public const string Duration = "duration";
        
        /// <summary>Format for an email. </summary>
        public const string Email = "email";

        /// <summary>Format for an URI. </summary>
        public const string Uri = "uri";

        /// <summary>Format for an GUID. </summary>
        public const string Guid = "guid";

        /// <summary>Format for an UUID (same as GUID). </summary>
        [Obsolete("Now made redundant. Use \"guid\" instead.")]
        public const string Uuid = "uuid";

        /// <summary>Format for an integer. </summary>
        public const string Integer = "int32";

        /// <summary>Format for a long integer. </summary>
        public const string Long = "int64";

        /// <summary>Format for a double number. </summary>
        public const string Double = "double";

        /// <summary>Format for a float number. </summary>
        public const string Float = "float";

        /// <summary>Format for a decimal number. </summary>
        public const string Decimal = "decimal";

        /// <summary>Format for an IP v4 address. </summary>
        public const string IpV4 = "ipv4";

        /// <summary>Format for an IP v6 address. </summary>
        public const string IpV6 = "ipv6";

        /// <summary>Format for binary data encoded with Base64.</summary>
        /// <remarks>Should not be used. Prefer using Byte property of <see cref="JsonFormatStrings"/></remarks>
        [Obsolete("Now made redundant. Use \"byte\" instead.")]
        public const string Base64 = "base64";

        /// <summary>Format for a byte if used with numeric type or for base64 encoded value otherwise.</summary>
        public const string Byte = "byte";
        
        /// <summary>Format for a binary value.</summary>
        public const string Binary = "binary";

        /// <summary>Format for a hostname (DNS name).</summary>
        public const string Hostname = "hostname";

        /// <summary>Format for a phone number.</summary>
        public const string Phone = "phone";

        /// <summary>Format for a full date per RFC3339 Section 5.6.</summary>
        public const string Date = "date";

        /// <summary>Format for a full time per RFC3339 Section 5.6.</summary>
        public const string Time = "time";
    }

    public class JsonDefinition
    {
        private IDictionary<string, JsonProperty> _properties = new Dictionary<string, JsonProperty>();
        public virtual JsonDefinition Parent { get; set; }

        public List<JsonDefinition> Data { get; set; } = new List<JsonDefinition>();
        public JsonObjectType Type { get; set; }
        public string Format { get; set; }

        public IDictionary<string, JsonProperty> Properties
        {
            get => _properties;
            set
            {
                if (_properties != null)
                {
                    var newCollection = new Dictionary<string, JsonProperty>(value);
                    RegisterProperties(_properties, newCollection);
                }
            }
        }

        private void RegisterProperties(IDictionary<string, JsonProperty> properties, Dictionary<string, JsonProperty> newCollection)
        {
            _properties = newCollection;
            InitializeSchemaCollection(newCollection);
        }

        private void InitializeSchemaCollection(Dictionary<string, JsonProperty> newCollection)
        {
            foreach (var jsonSchemaProperty in newCollection)
            {
                jsonSchemaProperty.Value.Name = jsonSchemaProperty.Key;
                jsonSchemaProperty.Value.Parent = this;
            }
        }

        public IDictionary<string, JsonDefinition> Definitions { get; set; } = new Dictionary<string, JsonDefinition>();

        public Dictionary<PathId, JsonDefinition> PathIdDefinitions { get; set; } = new();

        public JsonDefinition Item { get; set; }
        public JsonDefinition Reference { get; set; }
        public PathId? Path { get; set; }

        public void Process()
        {
            Dictionary<PathId, JsonDefinition> dictionary = new Dictionary<PathId, JsonDefinition>();

            var groupedDefinitions = GetAllDefinitions().Where(x => x.Path is {}).GroupBy(x => x.Path!).OrderByDescending(x => x.Key.Depth).ToList();

            while (groupedDefinitions.Count > 0)
            {
                var grouping = groupedDefinitions.First();
                var merge = Merge(grouping.ToList());
                dictionary.Add(grouping.Key, merge);

                if (grouping.Key.Depth > 1)
                {
                    var parent =  grouping.Key.GetParent();
                    foreach (var groupedDefinition in groupedDefinitions.Where(x => x.Key.Equals(parent)))
                    {
                        foreach (var jsonDefinition in groupedDefinition)
                        {
                            var index = jsonDefinition.Data.FindIndex(x => x.Path.Equals(grouping.Key));
                            if (index != -1)
                            {
                                jsonDefinition.Data[index] = merge;
                            }
                        }
                    }
                }

                groupedDefinitions.RemoveAt(0);
            }

            PathIdDefinitions = dictionary;

        }

        public JsonDefinition Merge(IReadOnlyList<JsonDefinition> jsonDefinitions)
        {
            var definition = jsonDefinitions[0];

            foreach (var jsonDefinition in jsonDefinitions.Skip(1))
            {
                foreach (var (key, value) in jsonDefinition.Properties)
                {
                    if (definition.Properties.ContainsKey(key))
                    {
                        var property = definition.Properties[key];

                        if (definition.Properties[key].Type != value.Type)
                        {
                            if (value.Type == JsonObjectType.Null)
                            {
                                property.CanBeNull = true;
                            }
                            else
                            {
                                value.Type = JsonObjectType.Object;
                            }
                        }
                    }
                    else
                    {
                        value.CanBeNull = true;
                        definition.Properties.Add(key, value);
                    }
                }
            }

            return definition;
        }

        public IEnumerable<JsonDefinition> GetAllDefinitions()
        {
            foreach (var jsonDefinition in Data)
            {
                foreach (var children in jsonDefinition.GetAllDefinitions())
                {
                    yield return children;
                }
            }

            yield return this;
        }
    }

    public class JsonProperty : JsonDefinition
    {
        public string Name { get; set; }
        public bool CanBeNull { get; set; }
        public string SuggestedName { get; set; }


        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Type)} : {Type}, {nameof(CanBeNull)}: {CanBeNull}";
        }
    }

    [Flags]
    public enum JsonObjectType
    {
        /// <summary>No object type. </summary>
        [JsonProperty("none")]
        None = 0,

        /// <summary>An array. </summary>
        [JsonProperty("array")]
        Array = 1,

        /// <summary>A boolean value. </summary>
        [JsonProperty("boolean")]
        Boolean = 2,

        /// <summary>An integer value. </summary>
        [JsonProperty("integer")]
        Integer = 4,

        /// <summary>A null. </summary>
        [JsonProperty("null")]
        Null = 8, 

        /// <summary>An number value. </summary>
        [JsonProperty("number")]
        Number = 16,

        /// <summary>An object. </summary>
        [JsonProperty("object")]
        Object = 32,

        /// <summary>A string. </summary>
        [JsonProperty("string")]
        String = 64,

        /// <summary>A file (used in Swagger specifications). </summary>
        [JsonProperty("file")]
        File = 128,
    }

   

}