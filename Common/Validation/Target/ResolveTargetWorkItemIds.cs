using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;

namespace Common.Validation
{
    [RunOrder(6)]
    public class ResolveTargetWorkItemIds : ITargetValidator
    {
        private ILogger Logger { get; } = MigratorLogging.CreateLogger<ResolveTargetWorkItemIds>();

        public string Name => "Resolve target work item ids";

        public async Task Validate(IValidationContext context)
        {
            await ClassifyWorkItemIds(context);
        }

        private async Task ClassifyWorkItemIds(IValidationContext context)
        {
            // Remove all skipped work items to reduce the load of what we need to query
            var workItemIdsUrisToClassify = context.WorkItemIdsUris.Where(wi => !context.SkippedWorkItems.Contains(wi.Key)).ToList();
            var totalNumberOfBatches = ClientHelpers.GetBatchCount(workItemIdsUrisToClassify.Count(), Constants.BatchSize);

            if (workItemIdsUrisToClassify.Any())
            {
                var stopwatch = Stopwatch.StartNew();
                Logger.LogInformation(LogDestination.File, "Started querying target account to find previously migrated work items");

                await workItemIdsUrisToClassify.Batch(Constants.BatchSize).ForEachAsync(context.Config.Parallelism, async (workItemIdsUris, batchId) =>
                {
                    var batchStopwatch = Stopwatch.StartNew();
                    Logger.LogInformation(LogDestination.File, $"{Name} batch {batchId} of {totalNumberOfBatches}: Started");
                    //check if the workitems have already been migrated and add the classified work items to the context
                    var migrationStates = await FilterWorkItemIds(context, context.TargetClient.WorkItemTrackingHttpClient, workItemIdsUris.ToDictionary(k => k.Key, v => v.Value));

                    if (migrationStates.Any())
                    {
                        foreach (var migrationState in migrationStates)
                        {
                            context.WorkItemsMigrationState.Add(migrationState);
                        }
                    }

                    batchStopwatch.Stop();
                    Logger.LogInformation(LogDestination.File, $"{Name} batch {batchId} of {totalNumberOfBatches}: Completed in {batchStopwatch.Elapsed.TotalSeconds}s");
                });

                stopwatch.Stop();
                Logger.LogInformation(LogDestination.File, $"Completed querying target account to find previously migrated work items in {stopwatch.Elapsed.TotalSeconds}s");
            }
        }

        private async Task<IList<WorkItemMigrationState>> FilterWorkItemIds(IValidationContext context, WorkItemTrackingHttpClient client, IDictionary<int, string> workItems)
        {
            // call GetWorkItemIdsForArtifactUrisAsync for target client to get the mapping of artifacturis and ids 
            // do a check to see if any of them have already been migrated
            var artifactUris = workItems.Select(a => a.Value).ToList();
            var result = await ClientHelpers.QueryArtifactUriToGetIdsFromUris(client, artifactUris);

            IList<WorkItemMigrationState> workItemStateList = new List<WorkItemMigrationState>();

            //check if any of the workitems have been migrated before 
            foreach (var workItem in workItems)
            {
                try
                {
                    if (ClientHelpers.GetMigratedWorkItemId(result, workItem, out int id))
                    {
                        workItemStateList.Add(new WorkItemMigrationState { SourceId = workItem.Key, TargetId = id, MigrationState = WorkItemMigrationState.State.Existing });
                    }
                    else
                    {
                        workItemStateList.Add(new WorkItemMigrationState { SourceId = workItem.Key, MigrationState = WorkItemMigrationState.State.Create });
                    }
                }
                catch (Exception e)
                {
                    //edge case where we find more than one workitems in the target for the workitem
                    Logger.LogError(LogDestination.File, e, e.Message);
                    //Add this workitem to notmigratedworkitem list 
                    workItemStateList.Add(new WorkItemMigrationState { SourceId = workItem.Key, MigrationState = WorkItemMigrationState.State.Error });
                }
            }

            return workItemStateList;
        }
    }
}
