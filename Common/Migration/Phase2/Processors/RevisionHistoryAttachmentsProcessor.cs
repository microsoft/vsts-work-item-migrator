using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Newtonsoft.Json;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Logging;
using Common.Config;

namespace Common.Migration
{
    public class RevisionHistoryAttachmentsProcessor : IPhase2Processor
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<RevisionHistoryAttachmentsProcessor>();

        public string Name => Constants.RelationPhaseRevisionHistoryAttachments;

        public bool IsEnabled(ConfigJson config)
        {
            return config.MoveHistory;
        }

        public async Task Preprocess(IMigrationContext migrationContext, IBatchMigrationContext batchContext, IList<WorkItem> sourceWorkItems, IList<WorkItem> targetWorkItems)
        {
            
        }

        public async Task<IEnumerable<JsonPatchOperation>> Process(IMigrationContext migrationContext, IBatchMigrationContext batchContext, WorkItem sourceWorkItem, WorkItem targetWorkItem)
        {
            IList<JsonPatchOperation> jsonPatchOperations = new List<JsonPatchOperation>();
            AttachmentReference aRef = await UploadAttachmentsToTarget(migrationContext, sourceWorkItem);
            JsonPatchOperation revisionHistoryAttachmentAddOperation = MigrationHelpers.GetRevisionHistoryAttachmentAddOperation(aRef, sourceWorkItem.Id.Value);
            jsonPatchOperations.Add(revisionHistoryAttachmentAddOperation);

            return jsonPatchOperations; // We could just return one item, but we make an IList to be consistent
        }

        private async Task<AttachmentReference> UploadAttachmentsToTarget(IMigrationContext migrationContext, WorkItem sourceWorkItem)
        {
            RevisionHistoryAttachments revisionHistoryAttachmentsItem = await GetWorkItemUpdates(migrationContext, sourceWorkItem);

            string attachment = JsonConvert.SerializeObject(revisionHistoryAttachmentsItem.Updates);
            AttachmentReference aRef;
            using (MemoryStream stream = new MemoryStream())
            {
                var stringBytes = System.Text.Encoding.UTF8.GetBytes(attachment);
                await stream.WriteAsync(stringBytes, 0, stringBytes.Length);
                stream.Position = 0;
                //upload the attachment to the target for each workitem
                aRef = await WorkItemTrackingHelpers.CreateAttachmentAsync(migrationContext.TargetClient.WorkItemTrackingHttpClient, stream);
            }

            return aRef;
        }

        private async Task<RevisionHistoryAttachments> GetWorkItemUpdates(IMigrationContext migrationContext, WorkItem sourceWorkItem)
        {
            IList<RevisionHistoryAttachments> revisionHistoryAttachments = new List<RevisionHistoryAttachments>();
            var wiUpdates = await WorkItemTrackingHelpers.GetWorkItemUpdatesAsync(migrationContext.SourceClient.WorkItemTrackingHttpClient, sourceWorkItem.Id.Value);
            return new RevisionHistoryAttachments { Workitem = sourceWorkItem, Updates = wiUpdates };
        }
    }
}
