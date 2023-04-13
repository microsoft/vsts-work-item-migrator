using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;


namespace Common.Migration
{
    public class FieldsPreProcessor : IPhase1PreProcessor
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<FieldsPreProcessor>();

        private IMigrationContext context;

        public string Name => "Fields";

        public bool IsEnabled(ConfigJson config)
        {
            return config.ProcessSourceFields;
        }

        public Task Prepare(IMigrationContext context)
        {
            this.context = context;
            return Task.CompletedTask;
        }
        public Task Process(IBatchMigrationContext batchContext)
        {
            foreach (var sourceWorkItem in batchContext.SourceWorkItems)
            {
                
                foreach (var fieldName in context.Config.SourceFieldsProcessing.Keys)
                {
                    var sourceField = context.Config.SourceFieldsProcessing[fieldName]; 
                    var fields = sourceField.Fields; 
                    var format = sourceField.Format;  
                    var type = sourceField.WorkItemType; 
                
                    string sourceWorkItemType = GetWorkItemTypeFromWorkItem(sourceWorkItem);

                    if(sourceWorkItemType == type )
                    {
                    var formatted = string.Format(format, fields.Select(f => sourceWorkItem.Fields.GetValueOrDefault(f)).ToArray());
                    sourceWorkItem.Fields[fieldName] = formatted;
                    }
                }
            }

            return Task.CompletedTask;
        }

        public string GetWorkItemTypeFromWorkItem(WorkItem sourceWorkItem)
        {
            return sourceWorkItem.Fields[FieldNames.WorkItemType] as string;
        }
    }
}