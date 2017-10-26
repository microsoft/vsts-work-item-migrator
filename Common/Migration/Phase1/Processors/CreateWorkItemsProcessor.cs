using System.Collections.Generic;
using System.Linq;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;

namespace Common.Migration
{
    [RunOrder(1)]
    public class CreateWorkItemsProcessor : BaseWorkItemsProcessor
    {
        protected override ILogger Logger { get; } = MigratorLogging.CreateLogger<CreateWorkItemsProcessor>();

        public override string Name => "Create work items";

        public override bool IsEnabled(ConfigJson config)
        {
            return true;
        }

        public override void PrepareBatchContext(IBatchMigrationContext batchContext, IList<WorkItemMigrationState> workItemsAndStateToMigrate)
        {
        }

        public override IList<WorkItemMigrationState> GetWorkItemsAndStateToMigrate(IMigrationContext context)
        {
            return context.WorkItemsMigrationState.Where(wi => wi.MigrationState == WorkItemMigrationState.State.Create).ToList();
        }

        public override int GetWorkItemsToProcessCount(IBatchMigrationContext batchContext)
        {
            return batchContext.SourceWorkItems?.Count ?? 0;
        }

        public override BaseWitBatchRequestGenerator GetWitBatchRequestGenerator(IMigrationContext context, IBatchMigrationContext batchContext)
        {
            return new CreateWitBatchRequestGenerator(context, batchContext);
        }
    }
}
