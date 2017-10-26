using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Logging;

namespace Common.Validation
{
    public class ValidateWorkItemRelationTypes : IConfigurationValidator
    {
        private ILogger Logger { get; } = MigratorLogging.CreateLogger<ValidateWorkItemRelationTypes>();

        public string Name => "Relation types";

        public async Task Validate(IValidationContext context)
        {
            if (context.Config.MoveLinks)
            {
                await VerifyRelationTypesExistsOnTarget(context);
            }
        }

        private async Task VerifyRelationTypesExistsOnTarget(IValidationContext context)
        {
            var sourceRelationTypes = await WorkItemTrackingHelpers.GetRelationTypesAsync(context.SourceClient.WorkItemTrackingHttpClient);
            var targetRelationTypes = await WorkItemTrackingHelpers.GetRelationTypesAsync(context.TargetClient.WorkItemTrackingHttpClient);

            var targetRelationNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var relationType in sourceRelationTypes)
            {
                //retrieve relations which are of type workitemlink defined by attribute kvp {"usage", "workitemlink"}
                if (relationType.Attributes.ContainsKeyIgnoringCase(Constants.WorkItemLinkAttributeKey) &&
                    String.Equals(relationType.Attributes[Constants.WorkItemLinkAttributeKey].ToString(), Constants.WorkItemLinkAttributeValue, StringComparison.OrdinalIgnoreCase))
                {
                    if (TargetHasRelationType(relationType, targetRelationTypes))
                    {
                        context.ValidatedWorkItemLinkRelationTypes.Add(relationType.ReferenceName);
                    }
                    else
                    {
                        Logger.LogWarning(LogDestination.File, $"Target: Relation type {relationType.ReferenceName} does not exist");
                    }
                }
            }
        }

        private bool TargetHasRelationType(WorkItemRelationType relation, IList<WorkItemRelationType> targetRelationTypes)
        {
            return targetRelationTypes.Where( a => string.Equals(relation.ReferenceName, a.ReferenceName, StringComparison.OrdinalIgnoreCase)).Any();
        }
    }
}