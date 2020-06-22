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
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Web;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;

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
        private Dictionary<string, string> sourceProjectReposDictionary = new Dictionary<string, string>();
        private Dictionary<string, GitRepository> targetProjectReposDictionary = new Dictionary<string, GitRepository>();

        public async Task Preprocess(IMigrationContext migrationContext, IBatchMigrationContext batchContext, IList<WorkItem> sourceWorkItems, IList<WorkItem> targetWorkItems)
        {
            //Build source repos dictionary
            var sourceGitClient = migrationContext.SourceClient.Connection.GetClient<GitHttpClient>();
            var sourceProjectRepositories = sourceGitClient.GetRepositoriesAsync(migrationContext.Config.SourceConnection.Project).Result;
            sourceProjectReposDictionary = sourceProjectRepositories.ToDictionary(
                r => r.Id.ToString(), r => r.Name);

            //Build target repos dictionary
            var targetGitClient = migrationContext.TargetClient.Connection.GetClient<GitHttpClient>();
            var targetProjectRepositories = targetGitClient.GetRepositoriesAsync(migrationContext.Config.TargetConnection.Project).Result;
            targetProjectReposDictionary = targetProjectRepositories.ToDictionary(
                r => r.Name, r => r);
        }

        public async Task<IEnumerable<JsonPatchOperation>> Process(IMigrationContext migrationContext, IBatchMigrationContext batchContext, WorkItem sourceWorkItem, WorkItem targetWorkItem)
        {
            IList<JsonPatchOperation> jsonPatchOperations = new List<JsonPatchOperation>();
            IEnumerable<WorkItemRelation> sourceGitCommitLinksRelations = GetGitLinksRelationsFromWorkItem(sourceWorkItem, Constants.RelationArtifactLink, migrationContext.Config.SourceConnection.Account);

            if (sourceGitCommitLinksRelations.Any())
            {
                foreach (WorkItemRelation sourceGitCommitLinkRelation in sourceGitCommitLinksRelations)
                {
                    if (!sourceGitCommitLinkRelation.Url.Contains("Git/Commit"))
                    {
                        continue;
                    }


                    //ArtifactLink format:
                    //vstfs:///Git/Commit/{sourceProjGuid}%2f{sourceRepoGuid}%2f47cb466c1d1f8dd8e1b43b40ed8c3a3fec67e20d
                    var gitGuidsOnly = HttpUtility.UrlDecode(sourceGitCommitLinkRelation.Url)
                        .Replace("vstfs:///Git/Commit/", string.Empty);

                    //Take the source GUIDs of Project and Repo
                    var sourceGitLinkProjectGuid = gitGuidsOnly.Split('/')[0];
                    var sourceGitLinkRepoGuid = gitGuidsOnly.Split('/')[1];

                    //Convert to Repo name from GUID
                    var sourceGitLinkRepoName = sourceProjectReposDictionary[sourceGitLinkRepoGuid];

                    //Find the target GUIDs of Project and Repo
                    var targetGitLinkRepoGuid = targetProjectReposDictionary[sourceGitLinkRepoName].Id.ToString();
                    var targetGitLinkProjectGuid = targetProjectReposDictionary[sourceGitLinkRepoName].ProjectReference.Id.ToString();


                    var newLinks = new Dictionary<string, object>
                            {
                                {
                                    "relations/-",
                                    new
                                    {
                                        rel = "ArtifactLink",
                                        url = sourceGitCommitLinkRelation.Url
                                        .Replace(sourceGitLinkProjectGuid, targetGitLinkProjectGuid)
                                        .Replace(sourceGitLinkRepoGuid, targetGitLinkRepoGuid),
                                        attributes = new
                                        {
                                            name = "Fixed in Commit",
                                            comment = "Added by the migration"
                                        }
                                    }
                                }
                            };

                    var gitCommitHyperlinkAddOperation = VssJsonPatchDocumentFactory.ConstructJsonPatchDocument(Operation.Add, newLinks);
                    jsonPatchOperations.Add(gitCommitHyperlinkAddOperation[0]);
                }
            }

            return jsonPatchOperations;
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