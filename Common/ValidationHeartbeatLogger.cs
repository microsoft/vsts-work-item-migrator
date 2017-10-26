using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Common.Validation;
using Logging;

namespace Common
{
    public class ValidationHeartbeatLogger : IDisposable
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<ValidationHeartbeatLogger>();

        private Timer timer;
        private IEnumerable<WorkItemMigrationState> WorkItemMigrationStates;
        private IValidationContext ValidationContext;

        public ValidationHeartbeatLogger(IEnumerable<WorkItemMigrationState> workItemMigrationStates, IValidationContext validationContext, int heartbeatFrequencyInSeconds)
        {
            this.timer = new Timer(Beat, "Some state", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(heartbeatFrequencyInSeconds));
            this.WorkItemMigrationStates = workItemMigrationStates;
            this.ValidationContext = validationContext;
        }

        public void Beat()
        {
            Beat("Some state");
        }

        private void Beat(object state)
        {
            string line1 = "VALIDATION STATUS:";
            string line2 = $"new work items found:                      {GetNewWorkItemsFound()}";
            string line3 = $"existing work items found:                 {GetExistingWorkItemsFound()}";
            string line4 = $"existing work items validated for phase 1: {GetExistingWorkItemsValidatedForPhase1()}";
            string line5 = $"existing work items validated for phase 2: {GetExistingWorkItemsValidatedForPhase2()}";

            string output = $"{line1}{Environment.NewLine}{line2}{Environment.NewLine}{line3}{Environment.NewLine}{line4}{Environment.NewLine}{line5}";

            int? workItemsReturnedFromQuery = GetWorkItemsReturnedFromQuery();
            string extraLine;

            if (workItemsReturnedFromQuery != null)
            {
                extraLine = $"total work items from query to validate:   {workItemsReturnedFromQuery}";
            }
            else
            {
                extraLine = $"Waiting for query to retrieve work items to be validated...";
            }

            output = $"{output}{Environment.NewLine}{extraLine}";
            Logger.LogInformation(output);
        }

        public void Dispose()
        {
            this.timer.Dispose();
        }

        private int GetNewWorkItemsFound()
        {
            return this.WorkItemMigrationStates.Where(w => w.MigrationState == WorkItemMigrationState.State.Create).Count();
        }

        private int GetExistingWorkItemsFound()
        {
            return this.WorkItemMigrationStates.Where(w => w.MigrationState == WorkItemMigrationState.State.Existing).Count();
        }

        private int GetExistingWorkItemsValidatedForPhase1()
        {
            return this.WorkItemMigrationStates.Where(w => w.MigrationState == WorkItemMigrationState.State.Existing && w.Requirement.HasFlag(WorkItemMigrationState.RequirementForExisting.UpdatePhase1)).Count();
        }

        private int GetExistingWorkItemsValidatedForPhase2()
        {
            return this.WorkItemMigrationStates.Where(w => w.MigrationState == WorkItemMigrationState.State.Existing && w.Requirement.HasFlag(WorkItemMigrationState.RequirementForExisting.UpdatePhase2)).Count();
        }

        private int? GetWorkItemsReturnedFromQuery()
        {
            if (this.ValidationContext.WorkItemIdsUris != null)
            {
                return this.ValidationContext.WorkItemIdsUris.Count();
            }
            else
            {
                return null;
            }
        }
    }
}
