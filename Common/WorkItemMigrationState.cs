using System;
using Common.Migration;

namespace Common
{
    public class WorkItemMigrationState
    {
        public int SourceId { get; set; }
        public int? TargetId { get; set; }

        public FailureReason FailureReason { get; set; }

        public State MigrationState { get; set; }
        public RequirementForExisting Requirement { get; set; }
        public MigrationCompletionStatus MigrationCompleted { get; set; }
        public enum State { Create, Existing, Error }

        [Flags]
        public enum RequirementForExisting { None, UpdatePhase1, UpdatePhase2 }
        [Flags]
        public enum MigrationCompletionStatus { None, Phase1, Phase2, Phase3 }
        public RevAndPhaseStatus RevAndPhaseStatus { get; set; }
    }
}
