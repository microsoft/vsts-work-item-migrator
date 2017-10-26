using System;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Common.Migration;
using Common.Validation;

namespace Common
{
    public class RelationHelpers
    {
        public static bool IsRelationHyperlinkToSourceWorkItem(IValidationContext context, WorkItemRelation relation, int sourceId)
        {
            string hyperlinkToSourceWorkItem = context.WorkItemIdsUris[sourceId];
            return relation.Rel.Equals(Constants.Hyperlink, StringComparison.OrdinalIgnoreCase) && relation.Url.Equals(hyperlinkToSourceWorkItem, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsRelationHyperlinkToSourceWorkItem(IMigrationContext context, WorkItemRelation relation, int sourceId)
        {
            string hyperlinkToSourceWorkItem = context.WorkItemIdsUris[sourceId];
            return relation.Rel.Equals(Constants.Hyperlink, StringComparison.OrdinalIgnoreCase) && relation.Url.Equals(hyperlinkToSourceWorkItem, StringComparison.OrdinalIgnoreCase);
        }
    }
}
