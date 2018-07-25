using Newtonsoft.Json;

namespace Common.Config
{
    public class TypeMapping
    {
        [JsonProperty(Required = Required.Always)]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "field-replacements", Required = Required.DisallowNull, DefaultValueHandling = DefaultValueHandling.Populate)]
        public FieldReplacements FieldReplacements { get; set; }
    }
}
