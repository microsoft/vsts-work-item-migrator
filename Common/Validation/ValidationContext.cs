using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Newtonsoft.Json;

namespace Common.Validation
{
    public class ValidationContext : BaseContext, IValidationContext
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<ValidationContext>();

        //Mapping of targetId of a work item to attribute id of the hyperlink
        public ConcurrentDictionary<int, Int64> TargetIdToSourceHyperlinkAttributeId { get; set; } = new ConcurrentDictionary<int, Int64>();

        public ISet<string> RequestedFields { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentDictionary<int, int> SourceWorkItemRevision { get; set; } = new ConcurrentDictionary<int, int>();

        public ConcurrentDictionary<int, String> SourceWorkItemIterationPath { get; set; } = new ConcurrentDictionary<int, string>();

        public ConcurrentDictionary<string, WorkItemField> SourceFields { get; set; } = new ConcurrentDictionary<string, WorkItemField>(StringComparer.OrdinalIgnoreCase);
        
        public ConcurrentDictionary<string, WorkItemField> TargetFields { get; set; } = new ConcurrentDictionary<string, WorkItemField>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentDictionary<string, ISet<string>> SourceTypesAndFields { get; } = new ConcurrentDictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentDictionary<string, ISet<string>> TargetTypesAndFields { get; } = new ConcurrentDictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentSet<string> ValidatedTypes { get; } = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentSet<string> ValidatedFields { get; } = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);

        public ISet<string> IdentityFields { get; set; }

        public ConcurrentSet<string> SkippedTypes { get; } = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentSet<string> SkippedFields { get; } = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);

        public ISet<string> TargetAreaPaths { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ISet<string> TargetIterationPaths { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentSet<string> ValidatedAreaPaths { get; set; } = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentSet<string> SkippedAreaPaths { get; set; } = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentSet<string> ValidatedIterationPaths { get; set; } = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentSet<string> SkippedIterationPaths { get; set; } = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentSet<string> ValidatedWorkItemLinkRelationTypes { get; set; } = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentSet<int> SkippedWorkItems { get; } = new ConcurrentSet<int>();

        public IList<string> FieldsThatRequireSourceProjectToBeReplacedWithTargetProject => fieldsThatRequireSourceProjectToBeReplacedWithTargetProject;

        // This includes area path and iteration path because these fields must have their project name updated to the target project name
        private readonly IList<string> fieldsThatRequireSourceProjectToBeReplacedWithTargetProject = new ReadOnlyCollection<string>(new[] {
            FieldNames.AreaPath,
            FieldNames.IterationPath,
            FieldNames.TeamProject
        });

        public ValidationContext(ConfigJson configJson) : base(configJson)
        {
            MigratorLogging.configMinimumLogLevel = this.Config.LogLevelForFile;

            LogConfigData();
        }

        private void LogConfigData()
        {
            Logger.LogInformation("Config data:");
            MemoryStream stream = new MemoryStream();
            
            using (StreamWriter sw = new StreamWriter(stream))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Ignore;
                    serializer.Serialize(writer, this.Config);

                    writer.Flush();
                    stream.Position = 0;
                    
                    StreamReader sr = new StreamReader(stream);
                    string output = sr.ReadToEnd();
                    Logger.LogInformation(output);
                }
            }
        }

        public ValidationContext() : base()
        {
        }
    }
}
