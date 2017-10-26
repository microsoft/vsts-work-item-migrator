using Newtonsoft.Json;

namespace Common.Config
{
    public class ConfigConnection
    {
        [JsonProperty(Required = Required.Always)]
        public string Account { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Project { get; set; }

        [JsonProperty(PropertyName = "access-token", Required = Required.DisallowNull)]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "use-integrated-auth", Required = Required.DisallowNull)]
        public bool UseIntegratedAuth { get; set; }

        // used by JSON.NET to control weather or not AccessToken gets serialized.
        // JSON.NET just knows to apply it to AccessToken because it’s in the method name.
        public bool ShouldSerializeAccessToken()
        {
            return false;
        }
    }
}
