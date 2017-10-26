using Newtonsoft.Json;

namespace Common.Config
{
    public class TargetFieldMap
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public string Value { get; set; }

        [JsonProperty(PropertyName = "field-reference-name", Required = Required.DisallowNull)]
        public string FieldReferenceName { get; set; }
    }
}
