using System.Collections.Generic;
using System.Linq;
using Common.Migration;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;

namespace Common.ApiWrappers
{
    public class Phase1ApiWrapper : BaseBatchApiWrapper
    {
        protected override ILogger Logger { get; } = MigratorLogging.CreateLogger<Phase1ApiWrapper>();

        protected override WorkItemTrackingHttpClient GetWorkItemTrackingHttpClient(IMigrationContext migrationContext)
        {
            return migrationContext.TargetClient.WorkItemTrackingHttpClient;
        }

        protected override void UpdateWorkItemMigrationStatus(IBatchMigrationContext batchContext, int sourceId, WorkItem targetWorkItem)
        {
            WorkItemMigrationState state = batchContext.WorkItemMigrationState.First(w => w.SourceId == sourceId);
            state.MigrationCompleted |= WorkItemMigrationState.MigrationCompletionStatus.Phase1;
            state.TargetId = targetWorkItem.Id.Value;
        }

        protected override void BatchCompleted(IMigrationContext migrationContext, IBatchMigrationContext batchContext)
        {
            var successfullyMigrated = batchContext.WorkItemMigrationState.Where(m => m.MigrationCompleted == WorkItemMigrationState.MigrationCompletionStatus.Phase1).Select(m => new KeyValuePair<int, int>(m.SourceId, m.TargetId.Value));
            // add successfully migrated workitems to main cache
            if (successfullyMigrated.Any())
            {
                migrationContext.SourceToTargetIds.AddRange(successfullyMigrated);
            }
        }
    }
}
