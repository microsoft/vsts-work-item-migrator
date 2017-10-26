using System.Collections.Generic;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.Migration
{
    public class BatchMigrationContext : IBatchMigrationContext
    {
        public int BatchId { get; set; }
        public IList<WorkItemMigrationState> WorkItemMigrationState { get; set; }
        //Inline image urls in html fields on source mapped to the guids of those on target. Used to replace url on target to point to the attachment file that was actually uploaded to the target. 
        public IDictionary<string, string> SourceInlineImageUrlToTargetInlineImageGuid { get; set; } = new Dictionary<string, string>();
        //List of source workitems that need to be migrated 
        public IList<WorkItem> SourceWorkItems {get; set;} = new List<WorkItem>();
        public IDictionary<int, WorkItem> TargetIdToSourceWorkItemMapping { get; set; } = new Dictionary<int, WorkItem>();
        public IDictionary<int, string> SourceToTagsOfWorkItemsSuccessfullyMigrated { get; set; } = new Dictionary<int, string>();
        public IDictionary<int, int> SourceWorkItemIdToTargetWorkItemIdMapping { get; set; } = new Dictionary<int, int>();

        public BatchMigrationContext(int batchId, IList<WorkItemMigrationState> workItemMigrationState)
        {
            this.BatchId = batchId;
            this.WorkItemMigrationState = workItemMigrationState;
        }
    }
}