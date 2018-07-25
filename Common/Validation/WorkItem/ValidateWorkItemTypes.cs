using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.Validation
{
    [RunOrder(5)]
    public class ValidateWorkItemTypes : IWorkItemValidator
    {
        private ILogger Logger { get; } = MigratorLogging.CreateLogger<ValidateWorkItemTypes>();

        public string Name => "Work item types";

        public async Task Prepare(IValidationContext context)
        {
            Logger.LogInformation("Reading the work item types from the source and target accounts");

            // When reading the work item, we need the type field for this validator
            context.RequestedFields.Add(FieldNames.WorkItemType);

            try
            {
                // We need all fields to validate the field types
                var sourceFields = (await WorkItemTrackingHelpers.GetFields(context.SourceClient.WorkItemTrackingHttpClient)).ToDictionary(key => key.ReferenceName);
                context.SourceFields = new ConcurrentDictionary<string, WorkItemField>(sourceFields, StringComparer.CurrentCultureIgnoreCase);
            }
            catch (Exception e)
            {
                throw new ValidationException("Unable to read fields on the source", e);
            }

            try
            {
                // We need all fields to validate the field types
                var targetFields = (await WorkItemTrackingHelpers.GetFields(context.TargetClient.WorkItemTrackingHttpClient)).ToDictionary(key => key.ReferenceName);
                context.TargetFields = new ConcurrentDictionary<string, WorkItemField>(targetFields, StringComparer.CurrentCultureIgnoreCase);
                context.IdentityFields = new HashSet<string>(targetFields.Where(f => f.Value.IsIdentity).Select(f => f.Key), StringComparer.CurrentCultureIgnoreCase);
            }
            catch (Exception e)
            {
                throw new ValidationException("Unable to read fields on the target", e);
            }

            ValidateFieldReplacements(context, context.Config.FieldReplacements);

            try
            {
                var workItemTypes = await WorkItemTrackingHelpers.GetWorkItemTypes(context.SourceClient.WorkItemTrackingHttpClient, context.Config.SourceConnection.Project);
                foreach (var workItemType in workItemTypes)
                {
                    context.SourceTypesAndFields[workItemType.Name] = new HashSet<string>(workItemType.Fields.Select(f => f.ReferenceName), StringComparer.CurrentCultureIgnoreCase);
                }
            }
            catch (Exception e)
            {
                throw new ValidationException("Unable to read work item types on the source", e);
            }

            try
            {
                var workItemTypes = await WorkItemTrackingHelpers.GetWorkItemTypes(context.TargetClient.WorkItemTrackingHttpClient, context.Config.TargetConnection.Project);
                foreach (var workItemType in workItemTypes)
                {
                    context.TargetTypesAndFields[workItemType.Name] = new HashSet<string>(workItemType.Fields.Select(f => f.ReferenceName), StringComparer.CurrentCultureIgnoreCase);
                }
            }
            catch (Exception e)
            {
                throw new ValidationException("Unable to read work item types on the target", e);
            }

            // A very edge case, but we should fail this scenario.
            if (context.SourceTypesAndFields.Count == 0 || context.TargetTypesAndFields.Count == 0)
            {
                throw new ValidationException("Source or target does not have any work item types");
            }

            ValidateTypeMapping(context);
        }

        public void ValidateFieldReplacements(IValidationContext context, FieldReplacements fieldReplacements)
        {
            if (fieldReplacements == null)
            {
                return;
            }

            foreach (var sourceToTargetFields in fieldReplacements)
            {
                string sourceFieldName = sourceToTargetFields.Key;
                TargetFieldMap targetFieldMap = sourceToTargetFields.Value;

                if (context.FieldsThatRequireSourceProjectToBeReplacedWithTargetProject.Contains(sourceFieldName, StringComparer.OrdinalIgnoreCase)
                    || context.FieldsThatRequireSourceProjectToBeReplacedWithTargetProject.Contains(targetFieldMap.FieldReferenceName, StringComparer.OrdinalIgnoreCase))
                {
                    string unsupportedFields = string.Join(", ", context.FieldsThatRequireSourceProjectToBeReplacedWithTargetProject);
                    throw new ValidationException($"Source fields or field-reference-name cannot be set to any of: {unsupportedFields} in the configuration file.");
                }

                if (!context.SourceFields.TryGetValueIgnoringCase(sourceFieldName, out WorkItemField sourceField))
                {
                    throw new ValidationException($"Source fields do not contain {sourceFieldName} that is specified in the configuration file.");
                }

                if (targetFieldMap.Value != null && targetFieldMap.FieldReferenceName != null)
                {
                    throw new ValidationException($"Under fields in config, for source field: {sourceFieldName}, you must specify value or field-reference-name, but not both.");
                }
                else if (targetFieldMap.Value == null && string.IsNullOrEmpty(targetFieldMap.FieldReferenceName))
                {
                    throw new ValidationException($"Under fields in config, for source field: {sourceFieldName}, you must specify value or field-reference-name.");
                }
                else if (!string.IsNullOrEmpty(targetFieldMap.FieldReferenceName))
                {
                    if (!context.TargetFields.TryGetValueIgnoringCase(targetFieldMap.FieldReferenceName, out WorkItemField targetField))
                    {
                        throw new ValidationException($"Target does not contain the field-reference-name you provided: {targetFieldMap.FieldReferenceName}.");
                    }

                    if (sourceField.Type != targetField.Type)
                    {
                        throw new ValidationException($"Target field {targetField.ReferenceName} of type {targetField.Type} must be of the same type as {sourceField.ReferenceName}, which is {sourceField.Type}");
                    }
                }
            }
        }

        public void ValidateTypeMapping(IValidationContext context)
        {
            if (context.Config.TypeMapping == null)
            {
                return;
            }

            foreach (var sourceType in context.Config.TypeMapping.Keys)
            {
                if (!context.SourceTypesAndFields.ContainsKey(sourceType))
                {
                    throw new ValidationException($"Source type {sourceType} in the mapping does not exist");
                }

                var targetType = context.Config.TypeMapping[sourceType].Type;
                if (!context.TargetTypesAndFields.ContainsKey(targetType))
                {
                    throw new ValidationException($"Target type in the mapping for ${sourceType} to ${targetType} does not exist");
                }

                ValidateFieldReplacements(context, context.Config.TypeMapping[sourceType].FieldReplacements);
            }
        }

        public async Task Validate(IValidationContext context, WorkItem workItem)
        {
            var sourceWorkItemType = (string)workItem.Fields[FieldNames.WorkItemType];
            var targetWorkItemType = sourceWorkItemType;
            var fieldReplacements = new FieldReplacements();
            
            if (context.Config.TypeMapping.TryGetValueIgnoringCase(sourceWorkItemType, out TypeMapping targetWorkItemTypeMapping))
            {
                targetWorkItemType = targetWorkItemTypeMapping.Type;
                if (targetWorkItemTypeMapping.FieldReplacements != null)
                {
                    fieldReplacements.AddRange(targetWorkItemTypeMapping.FieldReplacements);
                }
            }

            if (context.Config.FieldReplacements != null)
            {
                fieldReplacements.AddRange(context.Config.FieldReplacements);
            }

            // if we haven't processed this type at all
            if (!context.ValidatedTypes.Contains(sourceWorkItemType) && !context.SkippedTypes.Contains(sourceWorkItemType))
            {
                ClientHelpers.ExecuteOnlyOnce(sourceWorkItemType, () =>
                {
                    // just because we got the ownership of the mutex, it doesn't mean the 
                    // type hasn't been validated.
                    if (!context.ValidatedTypes.Contains(sourceWorkItemType) && !context.SkippedTypes.Contains(sourceWorkItemType))
                    {
                        ValidateWorkItemTypeFidelity(context, sourceWorkItemType, targetWorkItemType, fieldReplacements);
                    }
                });
            }

            // Nothing we can do with a work item type that doesn't exist, add it to the skipped list.
            if (context.SkippedTypes.Contains(sourceWorkItemType))
            {
                context.SkippedWorkItems.Add(workItem.Id.Value);
            }
        }

        private void ValidateWorkItemTypeFidelity(IValidationContext context, string sourceWorkItemType, string targetWorkItemType, FieldReplacements fieldReplacements)
        {
            Logger.LogInformation($"Checking metadata for work item type {sourceWorkItemType}");

            var matches = false;
            var exists = false;
            if (context.SourceTypesAndFields.TryGetValueIgnoringCase(sourceWorkItemType, out ISet<string> sourceWorkItemTypeFields)
                && context.TargetTypesAndFields.TryGetValueIgnoringCase(targetWorkItemType, out ISet<string> targetWorkItemTypeFields))
            {
                exists = true;
                matches = this.CompareWorkItemTypeFields(context, sourceWorkItemType, targetWorkItemType, sourceWorkItemTypeFields, targetWorkItemTypeFields, fieldReplacements);
            }

            // Only log this once so not to overload the log
            if (!matches && context.Config.SkipWorkItemsWithTypeMissingFields)
            {
                Logger.LogWarning("One or more work item types or fields do not exist in the target account. Please check the log for more details.");
            }

            //work item type does not exist. Log and continue since there is nothing we can do with it
            if (!exists)
            {
                Logger.LogWarning(LogDestination.File, $"Work item of type {sourceWorkItemType} mapped to {targetWorkItemType} does not exist on the target account and will be skipped");
                context.SkippedTypes.Add(sourceWorkItemType);
            }
            else
            {
                // if skipping types with field mismatch, add it to the skipped list
                if (!matches && context.Config.SkipWorkItemsWithTypeMissingFields)
                {
                    Logger.LogWarning(LogDestination.File, $"Work item type {sourceWorkItemType} exists but has missing fields on the target account and will be skipped");
                    context.SkippedTypes.Add(sourceWorkItemType);
                }
                // otherwise mark it as validated so that we don't re-validate this type
                else
                {
                    if (!matches)
                    {
                        Logger.LogInformation($"Work item type {sourceWorkItemType} validation completed but has missing fields, please check the log for more details");
                    }
                    else
                    {
                        Logger.LogInformation($"Work item type {sourceWorkItemType} validation completed successfully");
                    }

                    context.ValidatedTypes.Add(sourceWorkItemType);
                }
            }
        }

        public bool CompareWorkItemTypeFields(IValidationContext context, string sourceWorkItemType, string targetWorkItemType, ISet<string> sourceFields, ISet<string> targetFields, FieldReplacements fieldReplacements)
        {
            if (!sourceFields.Any() || !targetFields.Any())
            {
                return false;
            }

            var matches = true;
            foreach (var sourceField in sourceFields)
            {
                var targetField = sourceField;
                if (fieldReplacements.TryGetValueIgnoringCase(sourceField, out TargetFieldMap fieldMap) 
                    && fieldMap.FieldReferenceName != null)
                {
                    targetField = fieldMap.FieldReferenceName;
                }

                if (!context.ValidatedFields.Contains(sourceField) && !context.SkippedFields.Contains(sourceField))
                {
                    if (!targetFields.Contains(targetField))
                    {
                        matches = false;
                        context.SkippedFields.Add(sourceField);
                        Logger.LogWarning(LogDestination.File, $"Target: Field {targetField} does not exist in {targetWorkItemType}");
                    }
                    else
                    {
                        matches &= CompareField(context, context.SourceFields[sourceField], context.TargetFields[targetField]);
                    }
                }
            }

            return matches;
        }

        private bool CompareField(IValidationContext context, WorkItemField source, WorkItemField target)
        {
            var matches = true;
            if (source.Type != target.Type)
            {
                matches = false;
                Logger.LogWarning(LogDestination.File, $"Target: Field {source.ReferenceName} of type {source.Type} is not of the same type {target.Type}");
            }

            if (matches)
            {
                context.ValidatedFields.Add(source.ReferenceName);
            }
            else
            {
                context.SkippedFields.Add(source.ReferenceName);
            }

            return matches;
        }
    }
}
