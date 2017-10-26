using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Common.Config;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.Migration
{
    public interface IMigrationContext
    {
        ConfigJson Config { get; }

        WorkItemClientConnection SourceClient { get; }

        WorkItemClientConnection TargetClient { get; }

        ConcurrentDictionary<int, string> WorkItemIdsUris { get; set; }

        ConcurrentBag<WorkItemMigrationState> WorkItemsMigrationState { get; set; }

        ConcurrentDictionary<int, int> SourceToTargetIds { get; set; }

        //Mapping of targetId of a work item to attribute id of the hyperlink
        ConcurrentDictionary<int, Int64> TargetIdToSourceHyperlinkAttributeId { get; set; }

        //relations that do exist on the target
        ConcurrentSet<string> ValidatedWorkItemLinkRelationTypes { get; set; }

        ConcurrentDictionary<string, ISet<string>> WorkItemTypes { get; set; }

        ConcurrentDictionary<string, WorkItemField> SourceFields { get; set; }

        //Source work item id to Tags Field. Tags Field is null if the work item has no tags
        ConcurrentDictionary<int, string> SourceToTags { get; set; }

        ISet<string> HtmlFieldReferenceNames { get; set; }

        ISet<string> TargetAreaPaths { get; set; }

        ISet<string> TargetIterationPaths { get; set; }

        ISet<string> IdentityFields { get; set; }

        ConcurrentSet<string> ValidatedIdentities { get; set; }
        ConcurrentSet<string> InvalidIdentities { get; set; }

        IList<string> UnsupportedFields { get; }

        IList<string> FieldsThatRequireSourceProjectToBeReplacedWithTargetProject { get; set; }
    }
}
