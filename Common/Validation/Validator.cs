using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Logging;

namespace Common.Validation
{
    public class Validator
    {
        private ILogger Logger { get; } = MigratorLogging.CreateLogger<Validator>();

        private ValidationContext context;

        public Validator(ValidationContext context)
        {
            this.context = context;
        }

        public async Task Validate()
        {
            //Run all validations as specified in the RunOrder attribute of the classes that determine the interface IValidate
            //If no attribute is specified, then we add that instance to the bottom of the list for execution. 
            Logger.LogInformation("Starting validation");
            await ValidateConfiguration();
            await ValidateWorkItemMetadata();
            await TargetWorkItemValidation();

            //Validation completed 
            Logger.LogInformation($"{this.context.ValidatedFields.Count} fields validated, {this.context.SkippedFields.Count} skipped for migration");
            Logger.LogInformation($"{this.context.ValidatedTypes.Count} work item types validated, {this.context.SkippedTypes.Count} skipped for migration");
            Logger.LogInformation($"{this.context.ValidatedAreaPaths.Count} area paths validated, {this.context.SkippedAreaPaths.Count} skipped for migration");
            Logger.LogInformation($"{this.context.ValidatedIterationPaths.Count} iteration paths validated, {this.context.SkippedIterationPaths.Count} skipped for migration");
            Logger.LogInformation($"{this.context.WorkItemIdsUris.Count} work item(s) returned from the query for migration");

            var previouslyMigratedWorkItemsCount = this.context.WorkItemsMigrationState.Where(a => a.MigrationState == WorkItemMigrationState.State.Existing).Count();
            if (previouslyMigratedWorkItemsCount > 0)
            {
                Logger.LogInformation($"{previouslyMigratedWorkItemsCount} work item(s) were previously migrated to the target");
                // logging details of existing work items to the log file only
                Logger.LogInformation(LogDestination.File, "Source Id   :: Target Id");
                foreach (var item in this.context.WorkItemsMigrationState.Where(a => a.MigrationState == WorkItemMigrationState.State.Existing))
                {
                    Logger.LogInformation(LogDestination.File, $"{item.SourceId} :: {item.TargetId}");
                }
            }

            var workItemsWithErrorsCount = this.context.WorkItemsMigrationState.Where(a => a.MigrationState == WorkItemMigrationState.State.Error).Count();
            if (workItemsWithErrorsCount > 0)
            {
                Logger.LogInformation($"{workItemsWithErrorsCount} work item(s) have error entries and will not be migrated");
                // logging details of failed work items to the log file only
                Logger.LogInformation(LogDestination.File, "Source Id");
                foreach (var item in this.context.WorkItemsMigrationState.Where(a => a.MigrationState == WorkItemMigrationState.State.Error))
                {
                    Logger.LogInformation(LogDestination.File, $"{item.SourceId}");
                }
            }

            var skippedWorkItemsCount = this.context.SkippedWorkItems.Count();
            if (skippedWorkItemsCount > 0)
            {
                Logger.LogInformation($"{skippedWorkItemsCount} work item(s) have been skipped due to an invalid area/iteration path or type and will not be migrated");
                // logging details of skipped work items to the log file only
                Logger.LogInformation(LogDestination.File, "Source Id");
                foreach (var item in this.context.SkippedWorkItems)
                {
                    Logger.LogInformation(LogDestination.File, $"{item}");
                }
            }

            var workItemsToCreateCount = this.context.WorkItemsMigrationState.Where(a => a.MigrationState == WorkItemMigrationState.State.Create).Count();
            Logger.LogInformation($"{workItemsToCreateCount} work item(s) are new and will be migrated");

            var workItemsToUpdate = this.context.WorkItemsMigrationState.Where(w => w.MigrationState == WorkItemMigrationState.State.Existing && w.Requirement.HasFlag(WorkItemMigrationState.RequirementForExisting.UpdatePhase1));
            if (!context.Config.SkipExisting && workItemsToUpdate.Any())
            {
                Logger.LogInformation($"{workItemsToUpdate.Count()} work item(s) with field changes will be updated");
                // logging details of existing work items to update the log file only
                Logger.LogInformation(LogDestination.File, "Source Id   :: Target Id");
                foreach (var item in workItemsToUpdate)
                {
                    Logger.LogInformation(LogDestination.File, $"{item.SourceId} :: {item.TargetId}");
                }

                if (context.Config.MoveLinks)
                {
                    Logger.LogInformation("Move-Links is set to true, additional work items may be included for link processing if they had any link changes");
                }
            }

            Logger.LogSuccess(LogDestination.All, "Validation complete");
        }

        private async Task ValidateConfiguration()
        {
            Logger.LogInformation("Starting configuration validation");
            foreach (IConfigurationValidator validator in ClientHelpers.GetInstances<IConfigurationValidator>())
            {
                var stopwatch = Stopwatch.StartNew();
                Logger.LogInformation(LogDestination.File, $"Starting configuration validation for: {validator.Name}");

                await validator.Validate(context);

                stopwatch.Stop();
                Logger.LogInformation(LogDestination.File, $"Completed configuration validation for: {validator.Name} in {stopwatch.Elapsed.TotalSeconds}s");
            }

            Logger.LogInformation("Completed configuration validation");
        }

        private async Task ValidateWorkItemMetadata()
        {
            Logger.LogInformation("Starting work item metadata validation");

            var validators = ClientHelpers.GetInstances<IWorkItemValidator>();
            foreach (var validator in validators)
            {
                await validator.Prepare(context);
            }

            var totalNumberOfBatches = ClientHelpers.GetBatchCount(context.WorkItemIdsUris.Count, Constants.BatchSize);

            await context.WorkItemIdsUris.Keys.Batch(Constants.BatchSize).ForEachAsync(context.Config.Parallelism, async (workItemIds, batchId) =>
            {
                var stopwatch = Stopwatch.StartNew();
                Logger.LogInformation(LogDestination.File, $"Work item metadata validation batch {batchId} of {totalNumberOfBatches}: Starting");

                var workItems = await WorkItemTrackingHelpers.GetWorkItemsAsync(
                                    context.SourceClient.WorkItemTrackingHttpClient,
                                    workItemIds,
                                    context.RequestedFields);

                foreach (var validator in validators)
                {
                    Logger.LogInformation(LogDestination.File, $"Work item metadata validation batch {batchId} of {totalNumberOfBatches}: {validator.Name}");
                    foreach (var workItem in workItems)
                    {
                        await validator.Validate(context, workItem);
                    }
                }

                stopwatch.Stop();

                Logger.LogInformation(LogDestination.File, $"Work item metadata validation batch {batchId} of {totalNumberOfBatches}: Completed in {stopwatch.Elapsed.TotalSeconds}s");
            });

            Logger.LogInformation("Completed work item metadata validation");
        }

        private async Task TargetWorkItemValidation()
        {
            Logger.LogInformation("Starting target work item migration status");

            foreach (ITargetValidator validator in ClientHelpers.GetInstances<ITargetValidator>())
            {
                var stopwatch = Stopwatch.StartNew();
                Logger.LogInformation(LogDestination.File, $"Starting target work item migration status for: {validator.Name}");

                await validator.Validate(context);

                stopwatch.Stop();
                Logger.LogInformation(LogDestination.File, $"Completed target work item migration status for: {validator.Name} in {stopwatch.Elapsed.TotalSeconds}s");
            }

            Logger.LogInformation("Completed target work item migration status");
        }
    }
}
