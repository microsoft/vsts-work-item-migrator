using System.Linq;
using Common.Migration;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.ApiWrappers
{
    public class Phase2ApiWrapper : BaseBatchApiWrapper
    {
        protected override ILogger Logger { get; } = MigratorLogging.CreateLogger<Phase2ApiWrapper>();

        protected override WorkItemTrackingHttpClient GetWorkItemTrackingHttpClient(IMigrationContext migrationContext)
        {
            return migrationContext.TargetClient.WorkItemTrackingHttpClient;
        }

        protected override void UpdateWorkItemMigrationStatus(IBatchMigrationContext batchContext, int sourceId, WorkItem targetWorkItem)
        {
            WorkItemMigrationState state = batchContext.WorkItemMigrationState.First(w => w.SourceId == sourceId);
            state.MigrationCompleted |= WorkItemMigrationState.MigrationCompletionStatus.Phase2;
        }

        protected override void BatchCompleted(IMigrationContext migrationContext, IBatchMigrationContext batchContext)
        {
            // no-op
        }
    }
}
