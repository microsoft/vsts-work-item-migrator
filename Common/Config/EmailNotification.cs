using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Common.Config
{
    public class EmailNotification
    {
        [JsonProperty(PropertyName = "smtp-server", Required = Required.Always)]
        public string SmtpServer { get; set; }

        [JsonProperty(PropertyName = "use-ssl", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool UseSsl { get; set; }

        [JsonProperty(PropertyName = "port", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(25)]
        public int Port { get; set; }

        [JsonProperty(PropertyName = "from-address", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue("wimigrator@example.com")]
        public string FromAddress { get; set; }

        [JsonProperty(PropertyName = "recipient-addresses", DefaultValueHandling = DefaultValueHandling.Populate)]
        public List<string> RecipientAddresses { get; set; }

        [JsonProperty(PropertyName = "user-name", Required = Required.DisallowNull)]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "password", Required = Required.DisallowNull)]
        public string Password { get; set; }
    }
}
