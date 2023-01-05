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
                context.SourceFields = new ConcurrentDictionary<string, WorkItemField>(sourceFields, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                throw new ValidationException("Unable to read fields on the source", e);
            }

            try
            {
                // We need all fields to validate the field types
                var targetFields = (await WorkItemTrackingHelpers.GetFields(context.TargetClient.WorkItemTrackingHttpClient)).ToDictionary(key => key.ReferenceName);
                context.TargetFields = new ConcurrentDictionary<string, WorkItemField>(targetFields, StringComparer.OrdinalIgnoreCase);
                context.IdentityFields = new HashSet<string>(targetFields.Where(f => f.Value.IsIdentity).Select(f => f.Key), StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                throw new ValidationException("Unable to read fields on the target", e);
            }

            try
            {
                var workItemTypes = await WorkItemTrackingHelpers.GetWorkItemTypes(context.SourceClient.WorkItemTrackingHttpClient, context.Config.SourceConnection.Project);
                foreach (var workItemType in workItemTypes)
                {
                    context.SourceTypesAndFields[workItemType.Name] = new HashSet<string>(workItemType.Fields.Select(f => f.ReferenceName), StringComparer.OrdinalIgnoreCase);
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
                    context.TargetTypesAndFields[workItemType.Name] = new HashSet<string>(workItemType.Fields.Select(f => f.ReferenceName), StringComparer.OrdinalIgnoreCase);
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

            // handle condition of activated/not activated:
            ValidateFieldsMapping(context);
        }

        public void ValidateFieldsMapping(IValidationContext context)
        {
            if (context.Config.FieldReplacements == null)
            {
                return;
            }

            foreach (var sourceToTargetFields in context.Config.FieldReplacements)
            {
                string sourceField = sourceToTargetFields.Key;
                TargetFieldMap targetFieldMap = sourceToTargetFields.Value;

                if (!string.IsNullOrEmpty(targetFieldMap.WorkItemType))
                {
                    if (!context.SourceTypesAndFields.ContainsKey(targetFieldMap.WorkItemType))
                    {
                        throw new ValidationException($"Work item type {targetFieldMap.WorkItemType} specified for source field {sourceField} does not exist in the source project.");
                    }

                    if (!sourceField.StartsWith($"{targetFieldMap.WorkItemType}.", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ValidationException($"Source field {sourceField} does not start with the work item type {targetFieldMap.WorkItemType}.");
                    }
                    sourceField = sourceField.Substring(targetFieldMap.WorkItemType.Length + 1);
                }

                if (context.FieldsThatRequireSourceProjectToBeReplacedWithTargetProject.Contains(sourceField, StringComparer.OrdinalIgnoreCase) || context.FieldsThatRequireSourceProjectToBeReplacedWithTargetProject.Contains(targetFieldMap.FieldReferenceName, StringComparer.OrdinalIgnoreCase))
                {
                    string unsupportedFields = string.Join(", ", context.FieldsThatRequireSourceProjectToBeReplacedWithTargetProject);
                    throw new ValidationException($"Source fields or field-reference-name cannot be set to any of: {unsupportedFields} in the configuration file.");
                }


                if (!context.SourceFields.ContainsKeyIgnoringCase(sourceField))
                {
                    throw new ValidationException($"Source fields do not contain {sourceField} that is specified in the configuration file.");
                }

                if (!string.IsNullOrEmpty(targetFieldMap.FieldReferenceName))
                {
                    if (!context.TargetFields.ContainsKeyIgnoringCase(targetFieldMap.FieldReferenceName))
                    {
                        throw new ValidationException($"Target does not contain the field-reference-name you provided: {targetFieldMap.FieldReferenceName}.");
                    }
                }

                if (!string.IsNullOrEmpty(targetFieldMap.MappingName))
                {
                    if (!context.Config.FieldMappings.ContainsKey(targetFieldMap.MappingName))
                    {
                        throw new ValidationException($"Under fields-mappings in config, you must add a mapping with the name: {targetFieldMap.MappingName}, to handle source field {sourceField}.");
                    }
                }
            }
        }

        public async Task Validate(IValidationContext context, WorkItem workItem)
        {
            var type = (string)workItem.Fields[FieldNames.WorkItemType];

            // if we haven't processed this type at all
            if (!context.ValidatedTypes.Contains(type) && !context.SkippedTypes.Contains(type))
            {
                ClientHelpers.ExecuteOnlyOnce(type, () =>
                {
                    // just because we got the ownership of the mutex, it doesn't mean the 
                    // type hasn't been validated.
                    if (!context.ValidatedTypes.Contains(type) && !context.SkippedTypes.Contains(type))
                    {
                        ValidateWorkItemTypeFidelity(context, type);
                    }
                });
            }

            // Nothing we can do with a work item type that doesn't exist, add it to the skipped list.
            if (context.SkippedTypes.Contains(type))
            {
                context.SkippedWorkItems.Add(workItem.Id.Value);
            }
        }

        private void ValidateWorkItemTypeFidelity(IValidationContext context, string type)
        {
            Logger.LogInformation($"Checking metadata for work item type {type}");

            var matches = false;
            var exists = false;
            if (context.TargetTypesAndFields.ContainsKey(type) &&
                context.SourceTypesAndFields.ContainsKey(type))
            {
                exists = true;

                var targetWorkItemTypeFields = context.TargetTypesAndFields[type];
                var sourceWorkItemTypeFields = context.SourceTypesAndFields[type];

                matches = this.CompareWorkItemType(context, type, sourceWorkItemTypeFields, targetWorkItemTypeFields);
            }

            // Only log this once so not to overload the log
            if (!matches && context.Config.SkipWorkItemsWithTypeMissingFields)
            {
                Logger.LogWarning("One or more work item types or fields do not exist in the target account. Please check the log for more details.");
            }

            //work item type does not exist. Log and continue since there is nothing we can do with it
            if (!exists)
            {
                Logger.LogWarning(LogDestination.File, $"Work item type {type} does not exist on the target account and will be skipped");
                context.SkippedTypes.Add(type);
            }
            else
            {
                // if skipping types with field mismatch, add it to the skipped list
                if (!matches && context.Config.SkipWorkItemsWithTypeMissingFields)
                {
                    Logger.LogWarning(LogDestination.File, $"Work item type {type} exists but has missing fields on the target account and will be skipped");
                    context.SkippedTypes.Add(type);
                }
                // otherwise mark it as validated so that we don't re-validate this type
                else
                {
                    if (!matches)
                    {
                        Logger.LogInformation($"Work item type {type} validation completed but has missing fields, please check the log for more details");
                    }
                    else
                    {
                        Logger.LogInformation($"Work item type {type} validation completed successfully");
                    }

                    context.ValidatedTypes.Add(type);
                }
            }
        }

        public bool CompareWorkItemType(IValidationContext context, string workItemType, ISet<string> sourceFields, ISet<string> targetFields)
        {
            if (!sourceFields.Any() || !targetFields.Any())
            {
                return false;
            }
            var matches = true;
            foreach (var field in sourceFields)
            {
                if (!context.ValidatedFields.Contains(field) && !context.SkippedFields.Contains(field))
                {
                    var targetField = GetTargetFieldName(context, workItemType, field, out var targetIsMapped);
                    if (!targetFields.Contains(targetField))
                    {
                        matches = false;
                        context.SkippedFields.Add(field);
                        if (targetIsMapped)
                        {
                            Logger.LogWarning(LogDestination.File, $"Target field {targetField} does not exist in {workItemType} for Source Field {field}");
                        }
                        else
                        {
                            Logger.LogWarning(LogDestination.File, $"Target field {field} does not exist in {workItemType}");
                        }
                    }
                    else
                    {
                        matches &= CompareField(context, context.SourceFields[field], context.TargetFields[targetField], targetIsMapped);
                    }
                }
            }

            return matches;
        }

        private string GetTargetFieldName(IValidationContext context, string workItemType, string sourceFieldName, out bool targetFieldMapped)
        {
            Dictionary<string, TargetFieldMap> replacements = context.Config.FieldReplacements;
            targetFieldMapped = false;
            
            if (replacements != null)
            {
                TargetFieldMap targetFieldMap = null;
                var typeSpecificReplacementSourceName = $"{workItemType}.{sourceFieldName}";
                if (replacements.ContainsKeyIgnoringCase(typeSpecificReplacementSourceName))
                {
                    targetFieldMap = replacements[typeSpecificReplacementSourceName];
                }
                else if (replacements.ContainsKeyIgnoringCase(sourceFieldName))
                {
                    targetFieldMap = replacements[sourceFieldName];
                }

                if (!string.IsNullOrEmpty(targetFieldMap?.FieldReferenceName))
                {
                    targetFieldMapped = true;
                    return targetFieldMap.FieldReferenceName;
                }
            }

            return sourceFieldName;
        }

        private bool CompareField(IValidationContext context, WorkItemField source, WorkItemField target, bool isMapped)
        {
            var matches = true;
            if (source.Type != target.Type)
            {
                matches = false;
                if (isMapped)
                {
                    Logger.LogWarning(LogDestination.File, $"Target field {target.ReferenceName} of type {target.Type} is not of the same type {source.Type} of source field {source.ReferenceName}");
                }
                else
                {
                    Logger.LogWarning(LogDestination.File, $"Target field {target.ReferenceName} of type {target.Type} is not of the same type {source.Type}");
                }
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
