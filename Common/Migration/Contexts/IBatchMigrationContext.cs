using System.Collections.Generic;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.Migration
{
    public interface IBatchMigrationContext
    {
        int BatchId { get; set; }
        IList<WorkItemMigrationState> WorkItemMigrationState { get; set; }
        IDictionary<string, string> SourceInlineImageUrlToTargetInlineImageGuid { get; set; }
        IList<WorkItem> SourceWorkItems { get; set; }
        IDictionary<int, WorkItem> TargetIdToSourceWorkItemMapping { get; set; }
        IDictionary<int, string> SourceToTagsOfWorkItemsSuccessfullyMigrated { get; set; }
        IDictionary<int, int> SourceWorkItemIdToTargetWorkItemIdMapping { get; set; }
    }
}