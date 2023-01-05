using System.Collections.Generic;
using Newtonsoft.Json;

namespace Common.Config
{
    public class TargetFieldMap
    {
        [JsonProperty(Required = Required.DisallowNull)]
        public string Value { get; set; }

        [JsonProperty(PropertyName = "specific-to-type", Required = Required.DisallowNull)]
        public string WorkItemType { get; set; }

        [JsonProperty(PropertyName = "field-reference-name", Required = Required.DisallowNull)]
        public string FieldReferenceName { get; set; }

        [JsonProperty(PropertyName = "mapping-name", DefaultValueHandling = DefaultValueHandling.Populate, Required = Required.DisallowNull)]
        public string MappingName { get; set; }
    }
}
