using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Common.Config;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.Validation
{
    public interface IValidationContext : IContext
    {
        //Mapping of targetId of a work item to attribute id of the hyperlink
        ConcurrentDictionary<int, Int64> TargetIdToSourceHyperlinkAttributeId { get; set; }

        /// <summary>
        /// Fields to read on the work item for any validation steps that need the full work item,
        /// which is populated by the Prepare method by the IWorkITemMetadataValidator
        /// </summary>
        ISet<string> RequestedFields { get; }

        ConcurrentDictionary<int, int> SourceWorkItemRevision { get; set; }

        ConcurrentDictionary<string, WorkItemField> SourceFields { get; set; }

        ConcurrentDictionary<string, WorkItemField> TargetFields { get; set; }

        ConcurrentDictionary<string, ISet<string>> SourceTypesAndFields { get; }

        ConcurrentDictionary<string, ISet<string>> TargetTypesAndFields { get; }

        ConcurrentSet<string> ValidatedTypes { get; }

        ConcurrentSet<string> ValidatedFields { get; }

        ISet<string> IdentityFields { get; set; }

        ConcurrentSet<string> SkippedTypes { get; }

        ConcurrentSet<string> SkippedFields { get; }

        ISet<string> TargetAreaPaths { get; set; }

        ConcurrentSet<string> ValidatedAreaPaths { get; }

        ConcurrentSet<string> SkippedAreaPaths { get; }

        ISet<string> TargetIterationPaths { get; set; }

        ConcurrentSet<string> ValidatedIterationPaths { get; }
        
        ConcurrentSet<string> SkippedIterationPaths { get; }
        
        ConcurrentSet<string> ValidatedWorkItemLinkRelationTypes { get; set; }

        ConcurrentSet<int> SkippedWorkItems { get; }

        IList<string> FieldsThatRequireSourceProjectToBeReplacedWithTargetProject { get; }
    }
}
