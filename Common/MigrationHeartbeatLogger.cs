using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Logging;

namespace Common
{
    public class MigrationHeartbeatLogger : IDisposable
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<MigrationHeartbeatLogger>();

        private Timer timer;
        private IEnumerable<WorkItemMigrationState> WorkItemMigrationStates;

        public MigrationHeartbeatLogger(IEnumerable<WorkItemMigrationState> workItemMigrationStates, int heartbeatFrequencyInSeconds)
        {
            this.timer = new Timer(Beat, "Some state", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(heartbeatFrequencyInSeconds));
            this.WorkItemMigrationStates = workItemMigrationStates;
        }

        public void Beat()
        {
            Beat("Some state");
        }

        private void Beat(object state)
        {
            string line1 = "MIGRATION STATUS:";
            string line2 = $"work items that succeeded phase 1 migration: {GetSucceededPhase1WorkItemsCount()}";
            string line3 = $"work items that failed phase 1 migration:    {GetFailedPhase1WorkItemsCount()}";
            string line4 = $"work items to be processed in phase 1:       {GetPhase1Total()}";
            string line5 = $"work items that succeeded phase 2 migration: {GetSucceededPhase2WorkItemsCount()}";
            string line6 = $"work items that failed phase 2 migration:    {GetFailedPhase2WorkItemsCount()}";
            string line7 = $"work items to be processed in phase 2:       {GetPhase2Total()}";

            Logger.LogInformation($"{line1}{Environment.NewLine}{line2}{Environment.NewLine}{line3}{Environment.NewLine}{line4}{Environment.NewLine}{line5}{Environment.NewLine}{line6}{Environment.NewLine}{line7}");
        }

        public void Dispose()
        {
            this.timer.Dispose();
        }

        private int GetSucceededPhase1WorkItemsCount()
        {
            return this.WorkItemMigrationStates.Where(w => w.MigrationCompleted.HasFlag(WorkItemMigrationState.MigrationCompletionStatus.Phase1) && w.FailureReason == Migration.FailureReason.None).Count();
        }

        private int GetFailedPhase1WorkItemsCount()
        {
            return this.WorkItemMigrationStates.Where(w => w.MigrationCompleted.HasFlag(WorkItemMigrationState.MigrationCompletionStatus.Phase1) && w.FailureReason != Migration.FailureReason.None).Count();
        }

        private int GetSucceededPhase2WorkItemsCount()
        {
            return this.WorkItemMigrationStates.Where(w => w.MigrationCompleted.HasFlag(WorkItemMigrationState.MigrationCompletionStatus.Phase2) && w.FailureReason == Migration.FailureReason.None).Count();
        }

        private int GetFailedPhase2WorkItemsCount()
        {
            return this.WorkItemMigrationStates.Where(w => w.MigrationCompleted.HasFlag(WorkItemMigrationState.MigrationCompletionStatus.Phase2) && w.FailureReason != Migration.FailureReason.None).Count();
        }

        private int GetPhase1Total()
        {
            int workItemsToCreateCount = this.WorkItemMigrationStates.Where(a => a.MigrationState == WorkItemMigrationState.State.Create).Count();
            int workItemsToUpdate = this.WorkItemMigrationStates.Where(w => w.MigrationState == WorkItemMigrationState.State.Existing && w.Requirement.HasFlag(WorkItemMigrationState.RequirementForExisting.UpdatePhase1)).Count();
            return workItemsToCreateCount + workItemsToUpdate;
        }

        private int GetPhase2Total()
        {
            return this.WorkItemMigrationStates.Where(a => a.MigrationState == WorkItemMigrationState.State.Create || (a.MigrationState == WorkItemMigrationState.State.Existing && a.Requirement.HasFlag(WorkItemMigrationState.RequirementForExisting.UpdatePhase2))).Count();
        }
    }
}
