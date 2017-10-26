using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Common.Migration.Phase2Process
{
    [RunOrder(1)]
    public class ClearAllRelationsProcessor : IPhase2Processor
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<ClearAllRelationsProcessor>();

        public string Name => Constants.RelationPhaseClearAllRelations;

        public bool IsEnabled(ConfigJson config)
        {
            return true;
        }

        public async Task Preprocess(IMigrationContext migrationContext, IBatchMigrationContext batchContext, IList<WorkItem> sourceWorkItems, IList<WorkItem> targetWorkItems)
        {

        }

        public async Task<IEnumerable<JsonPatchOperation>> Process(IMigrationContext migrationContext, IBatchMigrationContext batchContext, WorkItem sourceWorkItem, WorkItem targetWorkItem)
        {
            return GetRemoveAllRelationsOperations(batchContext, targetWorkItem);
        }

        public IEnumerable<JsonPatchOperation> GetRemoveAllRelationsOperations(IBatchMigrationContext batchContext, WorkItem targetWorkItem)
        {
            return targetWorkItem.Relations?.Select((r, index) => MigrationHelpers.GetRelationRemoveOperation(index));
        }
    }
}
