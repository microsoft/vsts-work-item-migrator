using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Logging;
using Common.Config;

namespace Common.Migration
{
    public class GitCommitLinksProcessor : IPhase2Processor
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<GitCommitLinksProcessor>();

        public string Name => Constants.RelationPhaseGitCommitLinks;

        public bool IsEnabled(ConfigJson config)
        {
            return config.MoveGitLinks;
        }

        public async Task Preprocess(IMigrationContext migrationContext, IBatchMigrationContext batchContext, IList<WorkItem> sourceWorkItems, IList<WorkItem> targetWorkItems)
        {
            
        }

        public async Task<IEnumerable<JsonPatchOperation>> Process(IMigrationContext migrationContext, IBatchMigrationContext batchContext, WorkItem sourceWorkItem, WorkItem targetWorkItem)
        {
            IList<JsonPatchOperation> jsonPatchOperations = new List<JsonPatchOperation>();
            IEnumerable<WorkItemRelation> sourceGitCommitLinksRelations = GetGitLinksRelationsFromWorkItem(sourceWorkItem, Constants.RelationArtifactLink, migrationContext.Config.SourceConnection.Account);

            if (sourceGitCommitLinksRelations.Any())
            {
                foreach (WorkItemRelation sourceGitCommitLinkRelation in sourceGitCommitLinksRelations)
                {
                    string adjustedUrl = ConvertGitCommitLinkToHyperLink(sourceWorkItem.Id.Value, sourceGitCommitLinkRelation.Url, migrationContext.Config.SourceConnection.Account);
                    WorkItemRelation targetGitCommitHyperlinkRelation = GetGitCommitHyperlinkIfExistsOnTarget(targetWorkItem, adjustedUrl);

                    if (targetGitCommitHyperlinkRelation != null) // is on target
                    {
                        JsonPatchOperation gitCommitHyperlinkAddOperation = MigrationHelpers.GetRelationAddOperation(targetGitCommitHyperlinkRelation);
                        jsonPatchOperations.Add(gitCommitHyperlinkAddOperation);
                    }
                    else // is not on target
                    {
                        string comment = string.Empty;
                        if (sourceGitCommitLinkRelation.Attributes.ContainsKey(Constants.RelationAttributeComment))
                        {
                            comment = $"{sourceGitCommitLinkRelation.Attributes[Constants.RelationAttributeComment]}";
                        }

                        string adjustedComment = $"{Constants.RelationAttributeGitCommitCommentValue}{comment}";

                        WorkItemRelation newGitCommitLinkRelation = new WorkItemRelation();
                        newGitCommitLinkRelation.Rel = Constants.Hyperlink;
                        newGitCommitLinkRelation.Url = adjustedUrl;
                        newGitCommitLinkRelation.Attributes = new Dictionary<string, object>();
                        newGitCommitLinkRelation.Attributes[Constants.RelationAttributeComment] = adjustedComment;

                        JsonPatchOperation gitCommitHyperlinkAddOperation = MigrationHelpers.GetRelationAddOperation(newGitCommitLinkRelation);
                        jsonPatchOperations.Add(gitCommitHyperlinkAddOperation);
                    }
                }
            }

            return jsonPatchOperations;
        }

        private WorkItemRelation GetGitCommitHyperlinkIfExistsOnTarget(WorkItem targetWorkItem, string href)
        {
            if (targetWorkItem.Relations == null)
            {
                return null;
            }

            foreach (WorkItemRelation targetRelation in targetWorkItem.Relations)
            {
                if (targetRelation.Rel.Equals(Constants.Hyperlink) && targetRelation.Url.Equals(href, StringComparison.OrdinalIgnoreCase))
                {
                    return targetRelation;
                }
            }

            return null;
        }

        private object GetIdFromAttributes(WorkItemRelation relation)
        {
            if (relation.Attributes != null && relation.Attributes.ContainsKeyIgnoringCase(Constants.RelationAttributeId))
            {
                // get the key even if its letter case is different but it matches otherwise
                string idKeyFromFields = relation.Attributes.GetKeyIgnoringCase(Constants.RelationAttributeId);
                return relation.Attributes[idKeyFromFields];
            }
            else
            {
                return null;
            }
        }

        private static IEnumerable<WorkItemRelation> GetGitLinksRelationsFromWorkItem(WorkItem workItem, string linkType, string account)
        {
            IList<WorkItemRelation> result = new List<WorkItemRelation>();

            if (workItem.Relations != null)
            {
                foreach (WorkItemRelation relation in workItem.Relations)
                {
                    if (relation.Rel == linkType)
                    {
                        if (relation.Attributes != null && relation.Attributes.ContainsKey(Constants.RelationAttributeName) && relation.Attributes[Constants.RelationAttributeName].ToString() == Constants.RelationAttributeGitCommitNameValue)
                        {
                            result.Add(relation);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Convert the vstfs git commit link to a hyperlink which works
        /// </summary>
        public static string ConvertGitCommitLinkToHyperLink(int workItemId, string artifactLink, string account)
        {
            string hyperlink = null;
            if (!string.IsNullOrEmpty(artifactLink))
            {
                string decodedUrl = WebUtility.UrlDecode(artifactLink);
                Uri uri = new Uri(decodedUrl);
                var uriSegments = uri.Segments;
                if (uriSegments.Length == 6)
                {
                    //[3] =projectGuid, [4] = repoGuid, [5] = commitGuid
                    account = account.TrimEnd('/');
                    hyperlink = $"{account}/{uriSegments[3]}_git/{uriSegments[4]}commit/{uriSegments[5]}";
                }
                else
                {
                    Logger.LogWarning(LogDestination.File, $"{workItemId} has an invalid git commit link: {artifactLink}");
                }
            }
            else
            {
                Logger.LogWarning(LogDestination.File, $"{workItemId} git commit link is null");
            }
            return hyperlink;
        }
    }
}