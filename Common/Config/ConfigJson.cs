using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Common.Config
{
    public class ConfigJson
    {
        [JsonProperty(Required = Required.Always)]
        public string Query { get; set; }

        [JsonProperty(PropertyName = "source-connection", Required = Required.Always)]
        public ConfigConnection SourceConnection { get; set; }

        [JsonProperty(PropertyName = "target-connection", Required = Required.Always)]
        public ConfigConnection TargetConnection { get; set; }

        [JsonProperty(PropertyName = "parallelism", DefaultValueHandling = DefaultValueHandling.Populate)]
        public int Parallelism { get; set; }

        [JsonProperty(PropertyName = "link-parallelism", DefaultValueHandling = DefaultValueHandling.Populate)]
        public int LinkParallelism { get; set; }

        [JsonProperty(PropertyName = "heartbeat-frequency-in-seconds", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(30)]
        public int HeartbeatFrequencyInSeconds { get; set; }

        [JsonProperty(PropertyName = "query-page-size", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(20000)]
        public int QueryPageSize { get; set; }

        [JsonProperty(PropertyName = "max-attachment-size", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(60 * 1024 * 1024)]
        public int MaxAttachmentSize { get; set; }

        [JsonProperty(PropertyName = "attachment-upload-chunk-size", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(1 * 1024 * 1024)]
        public int AttachmentUploadChunkSize { get; set; }

        [JsonProperty(PropertyName = "skip-existing", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(true)]
        public bool SkipExisting { get; set; }

        [JsonProperty(PropertyName = "move-history", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool MoveHistory { get; set; }

        [JsonProperty(PropertyName = "move-history-limit", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(200)]
        public int MoveHistoryLimit { get; set; }

        [JsonProperty(PropertyName = "move-git-links", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool MoveGitLinks { get; set; }

        [JsonProperty(PropertyName = "move-attachments", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool MoveAttachments { get; set; }

        [JsonProperty(PropertyName = "move-links", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool MoveLinks { get; set; }

        [JsonProperty(PropertyName = "source-post-move-tag", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string SourcePostMoveTag { get; set; }

        [JsonProperty(PropertyName = "target-post-move-tag", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string TargetPostMoveTag { get; set; }

        [JsonProperty(PropertyName = "skip-work-items-with-type-missing-fields", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool SkipWorkItemsWithTypeMissingFields { get; set; }

        [JsonProperty(PropertyName = "skip-work-items-with-missing-area-path", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool SkipWorkItemsWithMissingAreaPath { get; set; }

        [JsonProperty(PropertyName = "skip-work-items-with-missing-iteration-path", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool SkipWorkItemsWithMissingIterationPath { get; set; }

        [JsonProperty(PropertyName = "default-area-path", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string DefaultAreaPath { get; set; }

        [JsonProperty(PropertyName = "default-iteration-path", DefaultValueHandling = DefaultValueHandling.Populate)]
        public string DefaultIterationPath { get; set; }

        [JsonProperty(PropertyName = "clear-identity-display-names", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool ClearIdentityDisplayNames { get; set; }

        [JsonProperty(PropertyName = "ensure-identities", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool EnsureIdentities { get; set; }

        [JsonProperty(PropertyName = "include-web-link", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool IncludeWebLink { get; set; }

        [JsonProperty(PropertyName = "hyper-links-excludes", DefaultValueHandling = DefaultValueHandling.Populate)]
        public List<string> HyperLinkExcludes { get; set; }

        [JsonProperty(PropertyName = "log-level-for-file", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(LogLevel.Information)]
        public LogLevel LogLevelForFile { get; set; }

        [JsonProperty(PropertyName = "field-replacements", DefaultValueHandling = DefaultValueHandling.Populate)]
        public Dictionary<string, TargetFieldMap> FieldReplacements { get; set; }

        [JsonProperty(PropertyName = "field-mappings", DefaultValueHandling = DefaultValueHandling.Populate)]
        public Dictionary<string, Dictionary<string, string>> FieldMappings { get; set; }

        [JsonProperty(PropertyName = "static-fields", DefaultValueHandling = DefaultValueHandling.Populate)]
        public Dictionary<string, Dictionary<string, object>> StaticFields { get; set; }

        [JsonProperty(PropertyName = "mapped-work-items", DefaultValueHandling = DefaultValueHandling.Populate)]
        public Dictionary<string, int> MappedWorkItems { get; set; }

        [JsonProperty(PropertyName = "send-email-notification", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool SendEmailNotification { get; set; }

        [JsonProperty(PropertyName = "email-notification", Required = Required.DisallowNull)]
        public EmailNotification EmailNotification { get; set; }
    }
}
