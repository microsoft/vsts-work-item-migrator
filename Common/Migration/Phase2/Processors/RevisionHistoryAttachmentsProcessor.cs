using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Newtonsoft.Json;

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
            var jsonPatchOperations = new List<JsonPatchOperation>();
            var attachments = await UploadAttachmentsToTarget(migrationContext, sourceWorkItem);
            foreach (var attachment in attachments)
            {
                JsonPatchOperation revisionHistoryAttachmentAddOperation = MigrationHelpers.GetRevisionHistoryAttachmentAddOperation(attachment, sourceWorkItem.Id.Value);
                jsonPatchOperations.Add(revisionHistoryAttachmentAddOperation);
            }

            return jsonPatchOperations;
        }

        private async Task<IList<AttachmentLink>> UploadAttachmentsToTarget(IMigrationContext migrationContext, WorkItem sourceWorkItem)
        {
            var attachmentLinks = new List<AttachmentLink>();
            int updateLimit = migrationContext.Config.MoveHistoryLimit;
            int updateCount = 0;

            while (updateCount < updateLimit)
            {
                var updates = await GetWorkItemUpdates(migrationContext, sourceWorkItem, skip: updateCount);
                string attachmentContent = JsonConvert.SerializeObject(updates);
                AttachmentReference attachmentReference;
                using (MemoryStream stream = new MemoryStream())
                {
                    var stringBytes = System.Text.Encoding.UTF8.GetBytes(attachmentContent);
                    await stream.WriteAsync(stringBytes, 0, stringBytes.Length);
                    stream.Position = 0;
                    //upload the attachment to the target for each workitem
                    attachmentReference = await WorkItemTrackingHelpers.CreateAttachmentAsync(migrationContext.TargetClient.WorkItemTrackingHttpClient, stream);
                    attachmentLinks.Add(
                        new AttachmentLink(
                            $"{Constants.WorkItemHistory}-{sourceWorkItem.Id}-{updateCount}.json", 
                            attachmentReference, 
                            stringBytes.Length,
                            comment: $"Update range from {updateCount} to {updateCount + updates.Count}"));
                }
                
                updateCount += updates.Count;

                // if we got less than a page size, that means we're on the last
                // page and shouldn't try and read another page.
                if (updates.Count < Constants.PageSize)
                {
                    break;
                }
            }

            return attachmentLinks;
        }

        private async Task<IList<WorkItemUpdate>> GetWorkItemUpdates(IMigrationContext migrationContext, WorkItem sourceWorkItem, int skip = 0)
        {
            return await WorkItemTrackingHelpers.GetWorkItemUpdatesAsync(migrationContext.SourceClient.WorkItemTrackingHttpClient, sourceWorkItem.Id.Value, skip);
        }
    }
}
