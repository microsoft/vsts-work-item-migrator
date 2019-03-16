using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

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

            foreach (var relationType in sourceRelationTypes)
            {
                //retrieve relations which are of type workitemlink defined by attribute kvp {"usage", "workitemlink"}
                //exclude remote link types because they need to be converted into hyperlinks
                if (IsWorkItemLinkType(relationType))
                {
                    if (RelationHelpers.IsRemoteLinkType(relationType))
                    {
                        context.RemoteLinkRelationTypes.Add(relationType.ReferenceName);
                    }
                    else
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
        }

        private bool TargetHasRelationType(WorkItemRelationType relation, IList<WorkItemRelationType> targetRelationTypes)
        {
            return targetRelationTypes.Where(a => string.Equals(relation.ReferenceName, a.ReferenceName, StringComparison.OrdinalIgnoreCase)).Any();
        }

        private bool IsWorkItemLinkType(WorkItemRelationType relationType)
        {
            return relationType.Attributes.TryGetValueOrDefaultIgnoringCase<string>(Constants.UsageAttributeKey, out var usage) &&
                    String.Equals(relationType.Attributes[Constants.UsageAttributeKey].ToString(), Constants.UsageAttributeValue, StringComparison.OrdinalIgnoreCase);
        }
    }
}