using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Common.Config;
using Logging;

namespace Common.Migration
{
    public class TargetPostMoveTagsProcessor : IPhase2Processor
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<TargetPostMoveTagsProcessor>();

        public TargetPostMoveTagsProcessor()
        {
        }

        public string Name => Constants.RelationPhaseTargetPostMoveTags;

        public bool IsEnabled(ConfigJson config)
        {
            return !string.IsNullOrEmpty(config.TargetPostMoveTag);
        }

        public async Task Preprocess(IMigrationContext migrationContext, IBatchMigrationContext batchContext, IList<WorkItem> sourceWorkItems, IList<WorkItem> targetWorkItems)
        {

        }

        public async Task<IEnumerable<JsonPatchOperation>> Process(IMigrationContext migrationContext, IBatchMigrationContext batchContext, WorkItem sourceWorkItem, WorkItem targetWorkItem)
        {
            IList<JsonPatchOperation> jsonPatchOperations = new List<JsonPatchOperation>();
            JsonPatchOperation addPostMoveTagOperation = AddPostMoveTag(migrationContext, targetWorkItem);
            jsonPatchOperations.Add(addPostMoveTagOperation);
            return jsonPatchOperations;
        }

        // here we modify the in-memory workItem.Fields because that is the easiest way to handle adding Post-Move-Tag
        private JsonPatchOperation AddPostMoveTag(IMigrationContext migrationContext, WorkItem targetWorkItem)
        {
            string tagKey = targetWorkItem.Fields.GetKeyIgnoringCase(Constants.TagsFieldReferenceName);

            if (tagKey != null) // Tags Field already exists
            {
                string existingTagsValue = (string)targetWorkItem.Fields[tagKey];
                string updatedTagsFieldWithPostMove = GetUpdatedTagsFieldWithPostMove(migrationContext, existingTagsValue);

                KeyValuePair<string, object> field = new KeyValuePair<string, object>(tagKey, updatedTagsFieldWithPostMove);
                return MigrationHelpers.GetJsonPatchOperationReplaceForField(field);
            }
            else // Tags Field does not exist, so we add it here
            {
                KeyValuePair<string, object> field = new KeyValuePair<string, object>(Constants.TagsFieldReferenceName, migrationContext.Config.TargetPostMoveTag);
                return MigrationHelpers.GetJsonPatchOperationAddForField(field);
            }
        }

        public string GetUpdatedTagsFieldWithPostMove(IMigrationContext migrationContext, string tagFieldValue)
        {
            string postMoveTag = migrationContext.Config.TargetPostMoveTag;
            return $"{tagFieldValue}; {postMoveTag}";
        }
    }
}
