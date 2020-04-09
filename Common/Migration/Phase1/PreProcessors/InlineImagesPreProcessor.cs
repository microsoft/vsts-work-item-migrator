using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Common.Config;
using Logging;

namespace Common.Migration
{
    public class InlineImagesPreProcessor : IPhase1PreProcessor
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<InlineImagesPreProcessor>();
        private IMigrationContext context;

        public string Name => "Inline images";

        public bool IsEnabled(ConfigJson config)
        {
            return true;
        }

        public async Task Prepare(IMigrationContext context)
        {
            this.context = context;
            this.context.HtmlFieldReferenceNames = GetHtmlFieldReferenceNames(this.context.SourceFields.Values.ToList());
        }

        public async Task Process(IBatchMigrationContext batchContext)
        {
            await ProcessInlineImages(batchContext);
        }

        private ISet<string> GetHtmlFieldReferenceNames(IList<WorkItemField> sourceWorkItemFields)
        {
            IEnumerable<WorkItemField> htmlFields = sourceWorkItemFields.Where(a => a.Type == FieldType.Html);
            ISet<string> htmlFieldReferenceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in htmlFields)
            {
                htmlFieldReferenceNames.Add(field.ReferenceName);
            }

            return htmlFieldReferenceNames;
        }

        private async Task ProcessInlineImages(IBatchMigrationContext batchContext)
        {
            foreach (WorkItem sourceWorkItem in batchContext.SourceWorkItems)
            {
                await ProcessInlineImagesForWorkItem(batchContext, sourceWorkItem);
            }
        }

        private async Task ProcessInlineImagesForWorkItem(IBatchMigrationContext batchContext, WorkItem sourceWorkItem)
        {
            if (sourceWorkItem.Fields != null)
            {
                foreach (KeyValuePair<string, object> field in sourceWorkItem.Fields)
                {
                    if (this.context.HtmlFieldReferenceNames.Contains(field.Key)) // field is of FieldType.Html
                    {
                        await ProcessInlineImagesForHtmlField(batchContext, sourceWorkItem.Id.Value, field);
                    }
                }
            }
        }

        private async Task ProcessInlineImagesForHtmlField(IBatchMigrationContext batchContext, int sourceWorkItemId, KeyValuePair<string, object> field)
        {
            // we are assuming that this will always be a string, so log if it is not
            if (!(field.Value is string))
            {
                Logger.LogWarning(LogDestination.File, $"Unexpected value for html field {field.Key} for source work item {sourceWorkItemId}");
            }

            string fieldHtmlContent = field.Value as string; 
            HashSet<string> inlineImageUrls = MigrationHelpers.GetInlineImageUrlsFromField(fieldHtmlContent, this.context.SourceClient.Connection.Uri.AbsoluteUri);

            foreach (string inlineImageUrl in inlineImageUrls)
            {
                // There are scenarios where someone copy/pastes image from one work item to another and the
                // it ends up using the same inline attachment link.
                if (!batchContext.SourceInlineImageUrlToTargetInlineImageGuid.ContainsKey(inlineImageUrl))
                {
                    string targetGuid = await UploadInlineImageAttachmentFromSourceWorkItemToTarget(batchContext, inlineImageUrl, sourceWorkItemId, context.Config.MaxAttachmentSize);
                    if (!String.IsNullOrEmpty(targetGuid))
                    {
                        batchContext.SourceInlineImageUrlToTargetInlineImageGuid.Add(inlineImageUrl, targetGuid);
                    }
                }
            }
        }

        /// <summary>
        /// Uploads inline image attachment from source to target and returns target inline image AttachmentReference Guid.
        /// </summary>
        /// <param name="inlineImageUrl"></param>
        /// <returns></returns>
        private async Task<string> UploadInlineImageAttachmentFromSourceWorkItemToTarget(IBatchMigrationContext batchContext, string inlineImageUrl, int sourceWorkItemId, int maxAttachmentSize)
        {
            Guid sourceGuid = MigrationHelpers.GetAttachmentUrlGuid(inlineImageUrl);
            string targetGuid = null;
            if (Guid.Empty.Equals(sourceGuid))
            {
                Logger.LogWarning(LogDestination.File, $"Unexpected format for inline image url {inlineImageUrl} for source work item {sourceWorkItemId}");
                ClientHelpers.AddFailureReasonToWorkItemMigrationState(sourceWorkItemId, FailureReason.InlineImageUrlFormatError, batchContext.WorkItemMigrationState);

                // just return the null guid since there is nothing we can do with an invalid url
                return null;
            }

            Stream stream = null;
            try
            {
                Logger.LogTrace(LogDestination.File, $"Reading inline image {inlineImageUrl} for source work item {sourceWorkItemId} from the source account");
                stream = await WorkItemTrackingHelpers.GetAttachmentAsync(this.context.SourceClient.WorkItemTrackingHttpClient, sourceGuid);
                Logger.LogTrace(LogDestination.File, $"Completed reading inline image {inlineImageUrl} for source work item {sourceWorkItemId} from the source account");
            }
            catch (Exception e)
            {
                Logger.LogError(LogDestination.File, e, $"Unable to download inline image {inlineImageUrl} for source work item {sourceWorkItemId} from the source account");
                ClientHelpers.AddFailureReasonToWorkItemMigrationState(sourceWorkItemId, FailureReason.InlineImageDownloadError, batchContext.WorkItemMigrationState);

                // just return the null guid since there is nothing we can do if we couldn't download the inline image
                return null;
            }

            if (stream != null)
            {
                using (MemoryStream memstream = new MemoryStream())
                {
                    bool copiedStream = false;
                    using (stream)
                    {
                        try
                        {
                            Logger.LogTrace(LogDestination.File, $"Downloading inline image {inlineImageUrl} for source work item {sourceWorkItemId} from the source account");
                            await ClientHelpers.CopyStreamAsync(stream, memstream);
                            Logger.LogTrace(LogDestination.File, $"Completed downloading inline image {inlineImageUrl} for source work item {sourceWorkItemId} from the source account");
                            copiedStream = true;
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(LogDestination.File, e, $"Unable to download inline image {inlineImageUrl} for source work item {sourceWorkItemId} from the source account");
                            ClientHelpers.AddFailureReasonToWorkItemMigrationState(sourceWorkItemId, FailureReason.InlineImageDownloadError, batchContext.WorkItemMigrationState);
                        }
                    }

                    if (memstream.Length > maxAttachmentSize)
                    {
                        Logger.LogWarning(LogDestination.File, $"Inline image attachment of source work item with id {sourceWorkItemId} and url {inlineImageUrl} exceeded the maximum attachment size of {maxAttachmentSize} bytes." +
                                $" Skipping creating the inline image attachment in target account.");
                        return null;
                    }

                    if (copiedStream)
                    {
                        memstream.Position = 0;
                        //upload the attachment to target
                        try
                        {
                            Logger.LogTrace(LogDestination.File, $"Uploading inline image {inlineImageUrl} for source work item {sourceWorkItemId} from the source account");
                            var aRef = await WorkItemTrackingHelpers.CreateAttachmentChunkedAsync(this.context.TargetClient.WorkItemTrackingHttpClient, this.context.TargetClient.Connection, memstream, this.context.Config.AttachmentUploadChunkSize);
                            targetGuid = aRef.Id.ToString();
                            Logger.LogTrace(LogDestination.File, $"Completed uploading inline image {inlineImageUrl} for source work item {sourceWorkItemId} from the source account");
                        }
                        catch (Exception e)
                        {
                            Logger.LogError(LogDestination.File, e, $"Unable to upload inline image for source work item {sourceWorkItemId} in the target account");
                            ClientHelpers.AddFailureReasonToWorkItemMigrationState(sourceWorkItemId, FailureReason.InlineImageUploadError, batchContext.WorkItemMigrationState);
                        }
                    }
                }
            }

            return targetGuid;
        }
    }
}
