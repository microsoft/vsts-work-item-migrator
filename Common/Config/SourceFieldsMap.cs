using System.Collections.Generic;
using Newtonsoft.Json;

namespace Common.Config
{
    public class SourceFieldsMap
    {

        [JsonProperty(PropertyName = "specific-to-type", Required = Required.DisallowNull)]
        public string WorkItemType { get; set; }

        [JsonProperty(PropertyName = "format", Required = Required.DisallowNull)]
        public string Format { get; set; }

        [JsonProperty(PropertyName = "fields", DefaultValueHandling = DefaultValueHandling.Populate, Required = Required.DisallowNull)]
        public string[] Fields { get; set; }
    }
}
