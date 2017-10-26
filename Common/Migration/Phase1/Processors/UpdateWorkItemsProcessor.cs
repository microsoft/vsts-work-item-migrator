using System.Collections.Generic;
using System.Linq;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;

namespace Common.Migration
{
    [RunOrder(2)]
    public class UpdateWorkItemsProcessor : BaseWorkItemsProcessor
    {
        protected override ILogger Logger { get; } = MigratorLogging.CreateLogger<UpdateWorkItemsProcessor>();

        public override string Name => "Update work items";

        public override bool IsEnabled(ConfigJson config)
        {
            return !config.SkipExisting;
        }

        public override IList<WorkItemMigrationState> GetWorkItemsAndStateToMigrate(IMigrationContext context)
        {
            return context.WorkItemsMigrationState.Where(w => w.MigrationState == WorkItemMigrationState.State.Existing && w.Requirement.HasFlag(WorkItemMigrationState.RequirementForExisting.UpdatePhase1)).ToList();
        }

        public override void PrepareBatchContext(IBatchMigrationContext batchContext, IList<WorkItemMigrationState> workItemsAndStateToMigrate)
        {
            foreach (var sourceWorkItem in batchContext.SourceWorkItems)
            {
                var workItemMigrationState = workItemsAndStateToMigrate.Where(w => w.SourceId == sourceWorkItem.Id.Value).FirstOrDefault();
                if (workItemMigrationState != null && workItemMigrationState.TargetId.HasValue)
                {
                    batchContext.TargetIdToSourceWorkItemMapping.Add(workItemMigrationState.TargetId.Value, sourceWorkItem);
                }
                else
                {
                    Logger.LogWarning(LogDestination.File, $"Expected source work item {sourceWorkItem.Id} to map to a target work item");
                }
            }
        }

        public override int GetWorkItemsToProcessCount(IBatchMigrationContext batchContext)
        {
            return batchContext.TargetIdToSourceWorkItemMapping.Count;
        }

        public override BaseWitBatchRequestGenerator GetWitBatchRequestGenerator(IMigrationContext context, IBatchMigrationContext batchContext)
        {
            return new UpdateWitBatchRequestGenerator(context, batchContext);
        }
    }
}
