using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Common.Config;
using Logging;

namespace Common.Validation
{
    public class ValidationContext : IValidationContext
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<ValidationContext>();

        public ConfigJson Config { get; }

        public WorkItemClientConnection SourceClient { get; }

        public WorkItemClientConnection TargetClient { get; }

        public ConcurrentDictionary<int, string> WorkItemIdsUris { get; set; }
        public ConcurrentBag<WorkItemMigrationState> WorkItemsMigrationState { get; private set; } = new ConcurrentBag<WorkItemMigrationState>();

        //Mapping of targetId of a work item to attribute id of the hyperlink
        public ConcurrentDictionary<int, Int64> TargetIdToSourceHyperlinkAttributeId { get; set; } = new ConcurrentDictionary<int, Int64>();

        public ISet<string> RequestedFields { get; } = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentDictionary<int, int> SourceWorkItemRevision { get; set; } = new ConcurrentDictionary<int, int>();

        public ConcurrentDictionary<string, WorkItemField> SourceFields { get; set; } = new ConcurrentDictionary<string, WorkItemField>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentDictionary<string, WorkItemField> TargetFields { get; set; } = new ConcurrentDictionary<string, WorkItemField>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentDictionary<string, ISet<string>> SourceTypesAndFields { get; } = new ConcurrentDictionary<string, ISet<string>>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentDictionary<string, ISet<string>> TargetTypesAndFields { get; } = new ConcurrentDictionary<string, ISet<string>>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentSet<string> ValidatedTypes { get; } = new ConcurrentSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentSet<string> ValidatedFields { get; } = new ConcurrentSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public ISet<string> IdentityFields { get; set; }

        public ConcurrentSet<string> SkippedTypes { get; } = new ConcurrentSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentSet<string> SkippedFields { get; } = new ConcurrentSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public ISet<string> TargetAreaPaths { get; set; } = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public ISet<string> TargetIterationPaths { get; set; } = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentSet<string> ValidatedAreaPaths { get; set; } = new ConcurrentSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentSet<string> SkippedAreaPaths { get; set; } = new ConcurrentSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentSet<string> ValidatedIterationPaths { get; set; } = new ConcurrentSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentSet<string> SkippedIterationPaths { get; set; } = new ConcurrentSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentSet<string> ValidatedWorkItemLinkRelationTypes { get; set; } = new ConcurrentSet<string>(StringComparer.CurrentCultureIgnoreCase);

        public ConcurrentSet<int> SkippedWorkItems { get; } = new ConcurrentSet<int>();

        public IList<string> FieldsThatRequireSourceProjectToBeReplacedWithTargetProject => fieldsThatRequireSourceProjectToBeReplacedWithTargetProject;

        // This includes area path and iteration path because these fields must have their project name updated to the target project name
        private readonly IList<string> fieldsThatRequireSourceProjectToBeReplacedWithTargetProject = new ReadOnlyCollection<string>(new[] {
            FieldNames.AreaPath,
            FieldNames.IterationPath,
            FieldNames.TeamProject
        });

        public ValidationContext(ConfigJson configJson)
        {
            this.Config = configJson;
            MigratorLogging.configMinimumLogLevel = this.Config.LogLevelForFile;

            LogConfigData();

            this.SourceClient = this.CreateValidationClient(this.Config.SourceConnection);
            this.TargetClient = this.CreateValidationClient(this.Config.TargetConnection);
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

        /// <summary>
        /// Wraps the call to CreateClient, handling any specific errors that could be thrown 
        /// around invalid PAT and account name and wraps them in a ValidationException with
        /// a helpful message.
        /// </summary>
        private WorkItemClientConnection CreateValidationClient(ConfigConnection connection)
        {
            try
            {
                return ClientHelpers.CreateClient(connection);
            }
            catch (Exception e) when (e is VssServiceResponseException && e.Message == "The resource cannot be found.")
            {
                throw new ValidationException(connection.Account, (VssServiceResponseException)e);
            }
            catch (Exception e) when (e is VssUnauthorizedException)
            {
                throw new ValidationException(connection.Account, (VssUnauthorizedException)e);
            }
        }

        public ValidationContext()
        {
        }
    }
}
