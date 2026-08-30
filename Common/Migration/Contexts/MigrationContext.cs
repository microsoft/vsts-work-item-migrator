using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Common.Config;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.Migration
{
    public class MigrationContext : BaseContext, IMigrationContext
    {
        //Mapping of targetId of a work item to attribute id of the hyperlink
        public ConcurrentDictionary<int, Int64> TargetIdToSourceHyperlinkAttributeId { get; set; } = new ConcurrentDictionary<int, Int64>();

        public ConcurrentSet<string> ValidatedWorkItemLinkRelationTypes { get; set; } 

        public ConcurrentDictionary<string, ISet<string>> WorkItemTypes { get; set; }

        public ConcurrentDictionary<string, WorkItemField> SourceFields { get; set; } = new ConcurrentDictionary<string, WorkItemField>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentDictionary<int, string> SourceToTags { get; set; } = new ConcurrentDictionary<int, string>();

        public ISet<string> HtmlFieldReferenceNames { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ISet<string> TargetAreaPaths { get; set; }

        public ISet<string> TargetIterationPaths { get; set; }

        public ISet<string> IdentityFields { get; set; }

        public ConcurrentSet<string> ValidatedIdentities { get; set; } = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);

        public ConcurrentSet<string> InvalidIdentities { get; set; } = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);

        public IList<string> UnsupportedFields => unsupportedFields;

        public IList<string> FieldsThatRequireSourceProjectToBeReplacedWithTargetProject { get; set; }
        public AreaAndIterationPathTree SourceAreaAndIterationTree { get; set; }
        public AreaAndIterationPathTree TargetAreaAndIterationTree { get; set; }

        // List of fields that we do not support in migration because they are related to the board or another reason.
        private readonly IList<string> unsupportedFields = new ReadOnlyCollection<string>(new[]{
            "System.BoardColumn",
            "System.BoardColumnDone",
            "Kanban.Column",
            "Kanban.Column.Done",
            "System.BoardLane",
            "System.AreaId",
            "System.IterationId",
            "System.IterationLevel1",
            "System.IterationLevel2",
            "System.IterationLevel3",
            "System.IterationLevel4",
            "System.IterationLevel5",
            "System.IterationLevel6",
            "System.IterationLevel7",
            "System.AreaLevel1",
            "System.AreaLevel2",
            "System.AreaLevel3",
            "System.AreaLevel4",
            "System.AreaLevel5",
            "System.AreaLevel6",
            "System.AreaLevel7"
        });

        public MigrationContext(ConfigJson configJson) : base(configJson)
        {
        }
    }
}
