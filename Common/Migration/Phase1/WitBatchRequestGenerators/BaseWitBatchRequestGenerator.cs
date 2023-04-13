using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Common.Migration
{
    public abstract class BaseWitBatchRequestGenerator
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<BaseWitBatchRequestGenerator>();

        protected IMigrationContext migrationContext; // stop passing this around
        protected IBatchMigrationContext batchContext;

        // Chose List of tuples instead of Dictionary because this guarantees that the ordering is maintained. Also lets us use custom names for the 2 values rather than key/value.
        // WorkItem Id property is a nullable int, so we have it here also
        protected List<(int BatchId, int WorkItemId)> IdWithinBatchToWorkItemIdMapping { get; }
        protected int IdWithinBatch;
        protected IList<WitBatchRequest> WitBatchRequests;
        protected string QueryString;

        public BaseWitBatchRequestGenerator()
        {
        }

        public BaseWitBatchRequestGenerator(IMigrationContext migrationContext, IBatchMigrationContext batchContext)
        {
            this.migrationContext = migrationContext;
            this.batchContext = batchContext;
            this.IdWithinBatchToWorkItemIdMapping = new List<(int BatchId, int WorkItemId)>();
            this.IdWithinBatch = -1;
            this.WitBatchRequests = new List<WitBatchRequest>();
            bool bypassRules = true;
            bool suppressNotifications = true;
            this.QueryString = $"bypassRules={bypassRules}&suppressNotifications={suppressNotifications}&api-version=4.0";

            // we only have a batch context when it's create/update work items
            if (batchContext != null)
            {
                // remove any work items marked as failed during preprocessing
                this.batchContext.SourceWorkItems = RemoveWorkItemsThatFailedPreprocessing(batchContext.WorkItemMigrationState, batchContext.SourceWorkItems);
            }
        }

        public abstract Task Write();

        protected bool WorkItemHasFailureState(WorkItem sourceWorkItem)
        {
            WorkItemMigrationState state = batchContext.WorkItemMigrationState.FirstOrDefault(a => a.SourceId == sourceWorkItem.Id.Value);

            if (state != null && state.MigrationState == WorkItemMigrationState.State.Error)
            {
                Logger.LogWarning(LogDestination.File, $"Skipping migration of work item with id {sourceWorkItem.Id.Value} due to Error in migration state with failure reasons: {state.FailureReason.ToString()}");
                return true;
            }

            return false;
        }

        protected IList<WorkItem> RemoveWorkItemsThatFailedPreprocessing(IList<WorkItemMigrationState> workItemMigrationState, IList<WorkItem> sourceWorkItems)
        {
            if (sourceWorkItems != null)
            {
                Dictionary<int, FailureReason> notMigratedWorkItems = ClientHelpers.GetNotMigratedWorkItemsFromWorkItemsMigrationState(workItemMigrationState);
                return sourceWorkItems.Where(w => !notMigratedWorkItems.ContainsKey(w.Id.Value)).ToList();
            }
            else
            {
                return null;
            }
        }

        protected void DecrementIdWithinBatch(int? sourceWorkItemId)
        {
            this.IdWithinBatchToWorkItemIdMapping.Add((this.IdWithinBatch, sourceWorkItemId.Value));
            this.IdWithinBatch--;
        }

        protected JsonPatchDocument CreateJsonPatchDocumentFromWorkItemFields(WorkItem sourceWorkItem)
        {
            string sourceWorkItemType = GetWorkItemTypeFromWorkItem(sourceWorkItem);
            JsonPatchDocument jsonPatchDocument = new JsonPatchDocument();

            IList<string> fieldNamesAlreadyPopulated = new List<string>();

            var fields = sourceWorkItem.Fields.AsEnumerable().ToList();
            if (migrationContext.Config.StaticFields.ContainsKey(sourceWorkItemType))
            {
                fields.AddRange(migrationContext.Config.StaticFields[sourceWorkItemType]);
            }

            foreach (var sourceField in fields)
            {
                if (fieldNamesAlreadyPopulated.Contains(sourceField.Key)) // we have already processed the content for this target field so skip
                {
                    continue;
                }

                if (FieldIsWithinType(sourceField.Key, sourceWorkItemType) && !IsFieldUnsupported(sourceField.Key))
                {
                    KeyValuePair<string, object> fieldProcessedForConfigFields = GetTargetField(sourceWorkItemType, sourceField, fieldNamesAlreadyPopulated);
                    KeyValuePair<string, object> preparedField = UpdateProjectNameIfNeededForField(sourceWorkItem, fieldProcessedForConfigFields);

                    // TEMPORARY HACK for handling emoticons in identity fields:
                    if (this.migrationContext.Config.ClearIdentityDisplayNames)
                    {
                        preparedField = RemoveEmojis(sourceField, preparedField);
                    }

                    // add inline image urls
                    JsonPatchOperation jsonPatchOperation;
                    if (this.migrationContext.HtmlFieldReferenceNames.Contains(preparedField.Key)
                        && preparedField.Value is string)
                    {
                        string updatedHtmlFieldValue = GetUpdatedHtmlField((string)preparedField.Value);
                        KeyValuePair<string, object> updatedField = new KeyValuePair<string, object>(preparedField.Key, updatedHtmlFieldValue);
                        jsonPatchOperation = MigrationHelpers.GetJsonPatchOperationAddForField(updatedField);
                    }
                    else
                    {
                        jsonPatchOperation = MigrationHelpers.GetJsonPatchOperationAddForField(preparedField);
                    }
                    jsonPatchDocument.Add(jsonPatchOperation);
                }
            }

            return jsonPatchDocument;
        }

        private string GetTargetFieldName(string workItemType, string sourceFieldName)
        {
            var field = GetTargetFieldMap(workItemType, sourceFieldName);

            if (!string.IsNullOrEmpty(field?.FieldReferenceName))
            {
                return field.FieldReferenceName;
            }

            return sourceFieldName;
        }

        private TargetFieldMap GetTargetFieldMap(string workItemType, string sourceFieldName)
        {
            var replacements = migrationContext.Config.FieldReplacements;
            if (replacements != null)
            {
                var typeSpecificReplacementSourceName = $"{workItemType}.{sourceFieldName}";
                if (replacements.ContainsKeyIgnoringCase(typeSpecificReplacementSourceName))
                {
                    return replacements[typeSpecificReplacementSourceName];
                }
                else if (replacements.ContainsKeyIgnoringCase(sourceFieldName))
                {
                    return replacements[sourceFieldName];
                }
            }

            return null;
        }

        private KeyValuePair<string, object> GetTargetField(string sourceWorkItemType, KeyValuePair<string, object> sourceField, IList<string> fieldNamesAlreadyPopulated)
        {
            string fieldName = sourceField.Key;
            object fieldValue = sourceField.Value;

            var targetFieldMap = GetTargetFieldMap(sourceWorkItemType, fieldName);

            if (targetFieldMap != null)
            {
                if (!string.IsNullOrEmpty(targetFieldMap.FieldReferenceName))
                {
                    fieldName = targetFieldMap.FieldReferenceName; // bring source value to specified target field
                    fieldNamesAlreadyPopulated.Add(targetFieldMap.FieldReferenceName);
                }

                // if (fieldValue is string sFieldValue && targetFieldMap.MappingName != null && migrationContext.Config.FieldMappings.ContainsKey(targetFieldMap.MappingName))
                if (fieldValue != null && targetFieldMap.MappingName != null && migrationContext.Config.FieldMappings.ContainsKey(targetFieldMap.MappingName))
                {
                    string sFieldValue = fieldValue.ToString();

                    var mapping = migrationContext.Config.FieldMappings[targetFieldMap.MappingName];
                    if (mapping.ContainsKey(sFieldValue))
                    {
                        fieldValue = mapping[sFieldValue];
                    }
                    else if (mapping.ContainsKey("default"))
                    {
                        fieldValue = mapping["default"];
                    }
                }
            }

            return new KeyValuePair<string, object>(fieldName, fieldValue);
        }

        // TEMPORARY HACK for handling emoticons in identity fields:
        protected KeyValuePair<string, object> RemoveEmojis(KeyValuePair<string, object> sourceField, KeyValuePair<string, object> targetField)
        {
            if (targetField.Value is string
                && this.migrationContext.IdentityFields.Contains(targetField.Key))
            {
                string targetFieldValueString = targetField.Value as string;

                if (!string.IsNullOrEmpty(targetFieldValueString))
                {
                    string fixedTargetFieldValue;
                    if (targetFieldValueString.Contains('<'))
                    {
                        int index = targetFieldValueString.IndexOf('<');
                        fixedTargetFieldValue = targetFieldValueString.Substring(index);
                    }
                    else
                    {
                        fixedTargetFieldValue = Regex.Replace(targetFieldValueString, @"[\p{C}\p{S}]*", "");
                    }

                    KeyValuePair<string, object> targetFieldNoSanta = new KeyValuePair<string, object>(targetField.Key, fixedTargetFieldValue);
                    targetField = targetFieldNoSanta;
                }
            }

            return targetField;
        }

        /// <summary>
        /// Returns a JsonPatchOperation with any inline image urls in the html content replaced to point to the appropriate stored attachment on the target.
        /// </summary>
        /// <param name="htmlField"></param>
        /// <returns></returns>
        private string GetUpdatedHtmlField(string htmlFieldValue)
        {
            HashSet<string> inlineImageUrls = MigrationHelpers.GetInlineImageUrlsFromField(htmlFieldValue, this.migrationContext.SourceClient.Connection.Uri.AbsoluteUri);

            foreach (string inlineImageUrl in inlineImageUrls)
            {
                if (this.batchContext.SourceInlineImageUrlToTargetInlineImageGuid.ContainsKey(inlineImageUrl))
                {
                    string newValue = BuildTargetInlineImageUrl(inlineImageUrl, this.batchContext.SourceInlineImageUrlToTargetInlineImageGuid[inlineImageUrl]);
                    htmlFieldValue = htmlFieldValue.Replace(inlineImageUrl, newValue);
                }
            }

            return htmlFieldValue;
        }

        private string BuildTargetInlineImageUrl(string sourceInlineImageUrl, string targetInlineImageGuid)
        {
            string sourceAccount = this.migrationContext.Config.SourceConnection.Account;
            string targetAccount = this.migrationContext.Config.TargetConnection.Account;
            string result = sourceInlineImageUrl.Replace(sourceAccount, targetAccount);
            return MigrationHelpers.ReplaceAttachmentUrlGuid(result, targetInlineImageGuid);
        }

        protected KeyValuePair<string, object> UpdateProjectNameIfNeededForField(WorkItem sourceWorkItem, KeyValuePair<string, object> sourceField)
        {
            if (FieldRequiresProjectNameUpdate(sourceField.Key))
            {
                // do the check so we know which of the 3 it is here
                return CreateTargetField(sourceWorkItem, sourceField);
            }
            else
            {
                return sourceField;
            }
        }

        protected KeyValuePair<string, object> CreateTargetField(WorkItem sourceWorkItem, KeyValuePair<string, object> sourceField)
        {
            KeyValuePair<string, object> targetField;
            string targetProject = this.migrationContext.Config.TargetConnection.Project;
            string sourceProject = this.migrationContext.Config.SourceConnection.Project;

            string defaultAreaPath = string.IsNullOrEmpty(this.migrationContext.Config.DefaultAreaPath) ? targetProject : this.migrationContext.Config.DefaultAreaPath;
            string defaultIterationPath = string.IsNullOrEmpty(this.migrationContext.Config.DefaultIterationPath) ? targetProject : this.migrationContext.Config.DefaultIterationPath;

            // Make sure the new area path and iteration path exist on target before assigning them.
            // Otherwise assign targetProject
            if (sourceField.Key.Equals(FieldNames.AreaPath, StringComparison.OrdinalIgnoreCase))
            {
                string targetPathName = GetTargetPathName(sourceField.Value as string, sourceProject, targetProject);
                AreaAndIterationPathTree.ReplaceRemainingPathComponents(targetPathName, migrationContext.Config.AreaPathMappings, out targetPathName);

                if (ExistsInTargetAreaPathList(targetPathName))
                {
                    targetField = new KeyValuePair<string, object>(sourceField.Key, targetPathName);
                }
                else
                {
                    targetField = new KeyValuePair<string, object>(sourceField.Key, defaultAreaPath);
                    Logger.LogWarning(LogDestination.File, $"Could not find corresponding AreaPath: {targetPathName} on target. Assigning the AreaPath: {defaultAreaPath} on source work item with Id: {sourceWorkItem.Id}.");
                }
            }
            else if (sourceField.Key.Equals(FieldNames.IterationPath, StringComparison.OrdinalIgnoreCase))
            {
                string targetPathName = GetTargetPathName(sourceField.Value as string, sourceProject, targetProject);
                AreaAndIterationPathTree.ReplaceRemainingPathComponents(targetPathName, migrationContext.Config.IterationPathMappings, out targetPathName);

                if (ExistsInTargetIterationPathList(targetPathName))
                {
                    targetField = new KeyValuePair<string, object>(sourceField.Key, targetPathName);
                }
                else
                {
                    targetField = new KeyValuePair<string, object>(sourceField.Key, defaultIterationPath);
                    Logger.LogWarning(LogDestination.File, $"Could not find corresponding IterationPath: {targetPathName} on target. Assigning the IterationPath: {defaultIterationPath} on source work item with Id: {sourceWorkItem.Id}.");
                }
            }
            else if (sourceField.Key.Equals(FieldNames.TeamProject, StringComparison.OrdinalIgnoreCase))
            {
                targetField = new KeyValuePair<string, object>(sourceField.Key, targetProject);
            }

            return targetField;
        }

        public string GetTargetPathName(string fieldValue, string sourceProject, string targetProject)
        {
            return AreaAndIterationPathTree.ReplaceLeadingProjectName(fieldValue, sourceProject, targetProject);
        }

        public bool ExistsInTargetAreaPathList(string areaPath)
        {
            return this.migrationContext.TargetAreaPaths.Any(a => a.Equals(areaPath, StringComparison.OrdinalIgnoreCase));
        }

        public bool ExistsInTargetIterationPathList(string iterationPath)
        {
            return this.migrationContext.TargetIterationPaths.Any(a => a.Equals(iterationPath, StringComparison.OrdinalIgnoreCase));
        }

        public bool FieldRequiresProjectNameUpdate(string fieldName)
        {
            return this.migrationContext.FieldsThatRequireSourceProjectToBeReplacedWithTargetProject.Any(a => a.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        }

        public virtual bool IsFieldUnsupported(string fieldRefName)
        {
            return this.migrationContext.UnsupportedFields.Any(a => fieldRefName.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// returns true if fieldName exists within workItemType on target ignoring case of strings.
        /// </summary>
        /// <param name="sourceFieldName"></param>
        /// <param name="sourceWorkItemType"></param>
        /// <returns></returns>
        public bool FieldIsWithinType(string sourceFieldName, string sourceWorkItemType)
        {
            var targetFieldName = GetTargetFieldName(sourceWorkItemType, sourceFieldName);
            ISet<string> fieldsOfKey = this.migrationContext.WorkItemTypes.First(a => a.Key.Equals(sourceWorkItemType, StringComparison.OrdinalIgnoreCase)).Value;
            return fieldsOfKey.Any(a => a.Equals(targetFieldName, StringComparison.OrdinalIgnoreCase));
        }

        public string GetWorkItemTypeFromWorkItem(WorkItem sourceWorkItem)
        {
            return sourceWorkItem.Fields[FieldNames.WorkItemType] as string;
        }

        public JsonPatchOperation GetInsertBatchIdAddOperation()
        {
            JsonPatchOperation jsonPatchOperation = new JsonPatchOperation();
            jsonPatchOperation.Operation = Operation.Add;
            jsonPatchOperation.Path = "/id";
            jsonPatchOperation.Value = this.IdWithinBatch;

            return jsonPatchOperation;
        }
    }
}
