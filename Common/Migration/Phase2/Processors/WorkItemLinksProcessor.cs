using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Logging;
using Microsoft.VisualStudio.Services.Common;
using Common.Config;

namespace Common.Migration
{
    public class WorkItemLinksProcessor : IPhase2Processor
    {
        private static ILogger Logger { get; } = MigratorLogging.CreateLogger<WorkItemLinksProcessor>();

        public string Name => Constants.RelationPhaseWorkItemLinks;

        public bool IsEnabled(ConfigJson config)
        {
            return config.MoveLinks;
        }

        public async Task Preprocess(IMigrationContext migrationContext, IBatchMigrationContext batchContext, IList<WorkItem> sourceWorkItems, IList<WorkItem> targetWorkItems)
        {
            var linkedWorkItemArtifactUrls = new HashSet<string>();
            foreach (WorkItem sourceWorkItem in sourceWorkItems)
            {
                var relations = GetWorkItemLinkRelations(migrationContext, sourceWorkItem.Relations);
                var linkedIds = relations.Select(r => ClientHelpers.GetWorkItemIdFromApiEndpoint(r.Url));
                var uris = linkedIds.Where(id => !migrationContext.SourceToTargetIds.ContainsKey(id)).Select(id => ClientHelpers.GetWorkItemApiEndpoint(migrationContext.Config.SourceConnection.Account, id));
                linkedWorkItemArtifactUrls.AddRange(uris);
            }

            await linkedWorkItemArtifactUrls.Batch(Constants.BatchSize).ForEachAsync(migrationContext.Config.Parallelism, async (workItemArtifactUris, batchId) =>
            {
                Logger.LogTrace(LogDestination.File, $"Finding linked work items on target for batch {batchId}");
                var results = await ClientHelpers.QueryArtifactUriToGetIdsFromUris(migrationContext.TargetClient.WorkItemTrackingHttpClient, workItemArtifactUris);
                foreach (var result in results.ArtifactUrisQueryResult)
                {
                    if (result.Value != null)
                    {
                        if (result.Value.Count() == 1)
                        {
                            var sourceId = ClientHelpers.GetWorkItemIdFromApiEndpoint(result.Key);
                            var targetId = result.Value.First().Id;

                            migrationContext.SourceToTargetIds[sourceId] = targetId;
                        }
                    }
                }

                Logger.LogTrace(LogDestination.File, $"Finished finding linked work items on target for batch {batchId}");
            });
        }

        public async Task<IEnumerable<JsonPatchOperation>> Process(IMigrationContext migrationContext, IBatchMigrationContext batchContext, WorkItem sourceWorkItem, WorkItem targetWorkItem)
        {
            IList<JsonPatchOperation> jsonPatchOperations = new List<JsonPatchOperation>();

            if (sourceWorkItem.Relations == null)
            {
                return jsonPatchOperations;
            }

            IList<WorkItemRelation> sourceWorkItemLinkRelations = GetWorkItemLinkRelations(migrationContext, sourceWorkItem.Relations);

            if (sourceWorkItemLinkRelations.Any())
            {
                foreach (WorkItemRelation sourceWorkItemLinkRelation in sourceWorkItemLinkRelations)
                {
                    int linkedSourceId = ClientHelpers.GetWorkItemIdFromApiEndpoint(sourceWorkItemLinkRelation.Url);
                    int targetWorkItemId = targetWorkItem.Id.Value;
                    int linkedTargetId;

                    if (!migrationContext.SourceToTargetIds.TryGetValue(linkedSourceId, out linkedTargetId))
                    {
                        continue;
                    }

                    string comment = MigrationHelpers.GetCommentFromAttributes(sourceWorkItemLinkRelation);
                    WorkItemLink newWorkItemLink = new WorkItemLink(linkedTargetId, sourceWorkItemLinkRelation.Rel, false, false, comment, 0);

                    JsonPatchOperation workItemLinkAddOperation = MigrationHelpers.GetWorkItemLinkAddOperation(migrationContext, newWorkItemLink);
                    jsonPatchOperations.Add(workItemLinkAddOperation);
                }
            }

            return jsonPatchOperations;
        }

        private IList<WorkItemRelation> GetWorkItemLinkRelations(IMigrationContext migrationContext, IList<WorkItemRelation> relations)
        {
            IList<WorkItemRelation> result = new List<WorkItemRelation>();

            if (relations == null)
            {
                return result;
            }

            foreach (WorkItemRelation relation in relations)
            {
                if (IsRelationWorkItemLink(migrationContext, relation))
                {
                    result.Add(relation);
                }
            }

            return result;
        }

        private bool IsRelationWorkItemLink(IMigrationContext migrationContext, WorkItemRelation relation)
        {
            if (migrationContext.ValidatedWorkItemLinkRelationTypes.Contains(relation.Rel))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
