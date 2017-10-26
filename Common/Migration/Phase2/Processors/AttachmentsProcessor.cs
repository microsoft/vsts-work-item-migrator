using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Logging;
using Common.Config;

namespace Common.Migration
{
    public class AttachmentsProcessor : IPhase2Processor
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<AttachmentsProcessor>();

        public string Name => Constants.RelationPhaseAttachments;

        public bool IsEnabled(ConfigJson config)
        {
            return config.MoveAttachments;
        }

        public async Task Preprocess(IMigrationContext migrationContext, IBatchMigrationContext batchContext, IList<WorkItem> sourceWorkItems, IList<WorkItem> targetWorkItems)
        {
            
        }

        public async Task<IEnumerable<JsonPatchOperation>> Process(IMigrationContext migrationContext, IBatchMigrationContext batchContext, WorkItem sourceWorkItem, WorkItem targetWorkItem)
        {
            IList<JsonPatchOperation> jsonPatchOperations = new List<JsonPatchOperation>();

            if (sourceWorkItem.Relations == null)
            {
                return jsonPatchOperations;
            }

            IEnumerable<WorkItemRelation> sourceAttachmentWorkItemRelations = GetAttachmentWorkItemRelations(sourceWorkItem.Relations);

            if (sourceAttachmentWorkItemRelations.Any())
            {
                foreach (WorkItemRelation sourceAttachmentWorkItemRelation in sourceAttachmentWorkItemRelations)
                {
                    WorkItemRelation targetAttachmentRelation = GetAttachmentIfExistsOnTarget(targetWorkItem, sourceAttachmentWorkItemRelation);

                    if (targetAttachmentRelation != null) // is on target
                    {
                        JsonPatchOperation attachmentAddOperation = MigrationHelpers.GetRelationAddOperation(targetAttachmentRelation);
                        jsonPatchOperations.Add(attachmentAddOperation);
                    }
                    else // is not on target
                    {
                        AttachmentLink attachmentLink = await UploadAttachmentFromSourceRelation(migrationContext, batchContext, sourceWorkItem, sourceAttachmentWorkItemRelation, migrationContext.Config.MaxAttachmentSize);
                        if (attachmentLink != null)
                        {
                            WorkItemRelation newAttachmentWorkItemRelation = new WorkItemRelation();
                            newAttachmentWorkItemRelation.Rel = sourceAttachmentWorkItemRelation.Rel;
                            newAttachmentWorkItemRelation.Url = attachmentLink.AttachmentReference.Url;
                            newAttachmentWorkItemRelation.Attributes = new Dictionary<string, object>();
                            newAttachmentWorkItemRelation.Attributes[Constants.RelationAttributeName] = attachmentLink.FileName;
                            newAttachmentWorkItemRelation.Attributes[Constants.RelationAttributeResourceSize] = attachmentLink.ResourceSize;
                            newAttachmentWorkItemRelation.Attributes[Constants.RelationAttributeComment] = attachmentLink.Comment;

                            JsonPatchOperation attachmentAddOperation = MigrationHelpers.GetRelationAddOperation(newAttachmentWorkItemRelation);
                            jsonPatchOperations.Add(attachmentAddOperation);
                        }
                    }
                }
            }

            return jsonPatchOperations;
        }

        private WorkItemRelation GetAttachmentIfExistsOnTarget(WorkItem targetWorkItem, WorkItemRelation sourceRelation)
        {
            if (targetWorkItem.Relations == null)
            {
                return null;
            }

            foreach (WorkItemRelation targetRelation in targetWorkItem.Relations)
            {
                if (targetRelation.Rel.Equals(Constants.AttachedFile) && targetRelation.Url.Equals(sourceRelation.Url, StringComparison.OrdinalIgnoreCase))
                {
                    return targetRelation;
                }
            }

            return null;
        }

        private IEnumerable<WorkItemRelation> GetAttachmentWorkItemRelations(IList<WorkItemRelation> relations)
        {
            IList<WorkItemRelation> result = new List<WorkItemRelation>();

            if (relations == null)
            {
                return result;
            }

            foreach (WorkItemRelation relation in relations)
            {
                if (IsRelationAttachedFile(relation))
                {
                    result.Add(relation);
                }
            }

            return result;
        }

        private bool IsRelationAttachedFile(WorkItemRelation relation)
        {
            return relation.Rel.Equals(Constants.AttachedFile, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<AttachmentLink> UploadAttachmentFromSourceRelation(IMigrationContext migrationContext, IBatchMigrationContext batchContext, WorkItem sourceWorkItem, WorkItemRelation sourceRelation, int maxAttachmentSize)
        {
            //Attachments are of type Rel = "AttachedFile"
            if (sourceRelation.Rel == Constants.AttachedFile)
            {
                string filename = null;
                string comment = null;
                long resourceSize = 0;
                //get the file name and comment  
                if (sourceRelation.Attributes.ContainsKey(Constants.RelationAttributeName))
                {
                    filename = sourceRelation.Attributes[Constants.RelationAttributeName].ToString();
                }
                if (sourceRelation.Attributes.ContainsKey(Constants.RelationAttributeComment))
                {
                    comment = sourceRelation.Attributes[Constants.RelationAttributeComment].ToString();
                }
                //get the guid from the url
                Guid attachmentId;
                if (Guid.TryParse(sourceRelation.Url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last(), out attachmentId))
                {
                    Stream stream = null;
                    try
                    {
                        Logger.LogTrace(LogDestination.File, $"Reading attachment {filename} for source work item {sourceWorkItem.Id} from the source account");
                        stream = await WorkItemTrackingHelpers.GetAttachmentAsync(migrationContext.SourceClient.WorkItemTrackingHttpClient, attachmentId);
                        Logger.LogTrace(LogDestination.File, $"Completed reading attachment {filename} for source work item {sourceWorkItem.Id} from the source account");
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(LogDestination.File, e, $"Unable to download attachment {filename} for source work item {sourceWorkItem.Id} from the source account");
                        ClientHelpers.AddFailureReasonToWorkItemMigrationState(sourceWorkItem.Id.Value, FailureReason.AttachmentDownloadError, batchContext.WorkItemMigrationState);
                        return null;
                    }

                    AttachmentReference aRef = null;
                    using (MemoryStream memstream = new MemoryStream())
                    {
                        using (stream)
                        {
                            try
                            {
                                Logger.LogTrace(LogDestination.File, $"Downloading attachment {filename} for source work item {sourceWorkItem.Id} from the source account");
                                await ClientHelpers.CopyStreamAsync(stream, memstream);
                                Logger.LogTrace(LogDestination.File, $"Completed downloading attachment {filename} for source work item {sourceWorkItem.Id} from the source account");
                            }
                            catch (Exception e)
                            {
                                Logger.LogError(LogDestination.File, e, $"Unable to read downloaded attachment {filename} for source work item {sourceWorkItem.Id} from the source account");
                                ClientHelpers.AddFailureReasonToWorkItemMigrationState(sourceWorkItem.Id.Value, FailureReason.AttachmentDownloadError, batchContext.WorkItemMigrationState);
                                return null;
                            }
                        }

                        resourceSize = memstream.Length;
                        if (resourceSize > maxAttachmentSize)
                        {
                            Logger.LogWarning(LogDestination.File, $"Attachment of source work item with id {sourceWorkItem.Id} and url {sourceRelation.Url} exceeded the maximum attachment size of {maxAttachmentSize} bytes." +
                                $" Skipping creating the attachment in target account.");
                            return null;
                        }
                        memstream.Position = 0;
                        //upload the attachment to the target
                        try
                        {
                            Logger.LogTrace(LogDestination.File, $"Uploading attachment {filename} of {resourceSize} bytes for source work item {sourceWorkItem.Id} from the source account");
                            aRef = await WorkItemTrackingHelpers.CreateAttachmentChunkedAsync(migrationContext.TargetClient.WorkItemTrackingHttpClient, migrationContext.TargetClient.Connection, memstream, migrationContext.Config.AttachmentUploadChunkSize);
                            Logger.LogTrace(LogDestination.File, $"Completed uploading attachment {filename} for source work item {sourceWorkItem.Id} from the source account");
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(LogDestination.File, e, $"Unable to upload attachment {filename} for source work item {sourceWorkItem.Id} to the target account");
                            ClientHelpers.AddFailureReasonToWorkItemMigrationState(sourceWorkItem.Id.Value, FailureReason.AttachmentUploadError, batchContext.WorkItemMigrationState);
                        }
                    }
                    if (aRef != null)
                    {
                         return new AttachmentLink(filename, aRef, resourceSize, comment);
                    }
                }
                else
                {
                    Logger.LogError(LogDestination.File, $"Attachment link is incorrect for {sourceWorkItem.Id} {sourceRelation.Url}. Skipping creating the attachment in target account.");
                    ClientHelpers.AddFailureReasonToWorkItemMigrationState(sourceWorkItem.Id.Value, FailureReason.AttachmentUploadError, batchContext.WorkItemMigrationState);
                }
            }

            return null;
        }
    }
}