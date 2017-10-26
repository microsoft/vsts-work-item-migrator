using System.Collections.Generic;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.Migration
{
    public class RevisionHistoryAttachments
    {
        public WorkItem Workitem { get; set; }
        public List<WorkItemUpdate> Updates { get; set; }
    }
}
