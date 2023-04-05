using System.Collections.Generic;

namespace Common
{
    public class Constants
    {
        public const int BatchSize = 200;
        public const int PageSize = 200;

        public const string Hyperlink = "Hyperlink";
        public const string Related = "Related";
        public const string AttachedFile = "AttachedFile";
        public const string WorkItemHistory = "WorkItemHistory";
        public const string RelationAttributeName = "name";
        public const string RelationAttributeResourceSize = "resourceSize";
        public const string RelationArtifactLink = "ArtifactLink";

        public const string RelationAttributeComment = "comment";
        public const string RelationAttributeId = "id";
        public const string RelationAttributeAuthorizedDate = "authorizedDate";
        public const string RelationAttributeResourceCreatedDate = "resourceCreatedDate";
        public const string RelationAttributeResourceModifiedDate = "resourceModifiedDate";
        public const string RelationAttributeRevisedDate = "revisedDate";

        public const string RelationAttributeGitCommitNameValue = "Fixed in Commit";
        public const string RelationAttributeGitCommitCommentValue = "(Git Commit Link) Comment: ";
        public const string Fields = "fields";
        public const string Relations = "relations";
        public const string UsageAttributeKey = "usage";
        public const string UsageAttributeValue = "workItemLink";
        public const string RemoteLinkAttributeKey = "remote";

        public const string TagsFieldReferenceName = "System.Tags";
        public const string TeamProjectReferenceName = "System.TeamProject";

        public const string RelationPhaseClearAllRelations = "clear all relations";

        public const string RelationPhaseAttachments = "attachments";
        public const string RelationPhaseGitCommitLinks = "git commit links";
        public const string RelationPhaseRevisionHistoryAttachments = "revision history attachments";
        public const string RelationPhaseWorkItemLinks = "work item links";
        public const string RelationPhaseHyperLinks = "hyperlinks";
        public const string RelationPhaseRemoteLinks = "remote work item links";
        public const string RelationPhaseSourcePostMoveTags = "source post move tags";
        public const string RelationPhaseTargetPostMoveTags = "target post move tags";
        
        public static readonly ISet<string> RelationPhases = new HashSet<string>(new[] {
            RelationPhaseAttachments,
            RelationPhaseGitCommitLinks,
            RelationPhaseRevisionHistoryAttachments,
            RelationPhaseWorkItemLinks,
            RelationPhaseRemoteLinks
        });
    }
}
