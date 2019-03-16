using System;
using System.Linq;
using System.Text.RegularExpressions;
using Common.Migration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common
{
    public class RelationHelpers
    {
        public static bool IsRelationHyperlinkToSourceWorkItem(IContext context, WorkItemRelation relation, int sourceId)
        {
            // only hyperlinks can contain the link to the source work item
            if (relation.Rel.Equals(Constants.Hyperlink, StringComparison.OrdinalIgnoreCase))
            {
                var hyperlinkToSourceWorkItem = context.WorkItemIdsUris[sourceId];

                var sourceParts = Regex.Split(hyperlinkToSourceWorkItem, "/_apis/wit/workitems/", RegexOptions.IgnoreCase);
                var targetParts = Regex.Split(relation.Url, "/_apis/wit/workitems/", RegexOptions.IgnoreCase);

                if (sourceParts.Length == 2 && targetParts.Length == 2)
                {
                    var sourceIdPart = sourceParts.Last();
                    var targetIdPart = targetParts.Last();

                    var sourceAccountPart = sourceParts.First().Split("/", StringSplitOptions.RemoveEmptyEntries);
                    var targetAccountPart = targetParts.First().Split("/", StringSplitOptions.RemoveEmptyEntries);

                    // url of the work item can contain project which we want to ignore since the url we generate does not include project
                    // and we just need to verify the ids are the same and the account are the same.
                    if (sourceAccountPart.Length > 1
                        && targetAccountPart.Length > 1
                        && string.Equals(sourceIdPart, targetIdPart, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(sourceAccountPart[1], targetAccountPart[1], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsRemoteLinkType(WorkItemRelationType relationType)
        {
            return relationType.Attributes.TryGetValueOrDefaultIgnoringCase<bool>(Constants.RemoteLinkAttributeKey, out var remote) && remote;
        }

        public static bool IsRemoteLinkType(IContext context, string relationReferenceName)
        {
            return context.RemoteLinkRelationTypes.Contains(relationReferenceName);
        }
    }
}
