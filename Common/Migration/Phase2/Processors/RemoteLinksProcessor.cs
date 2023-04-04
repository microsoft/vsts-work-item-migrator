using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Common.Migration
{
    public class RemoteLinksProcessor : IPhase2Processor
    {
        private static ILogger Logger { get; } = MigratorLogging.CreateLogger<RemoteLinksProcessor>();
        public string Name => Constants.RelationPhaseRemoteLinks;

        public bool IsEnabled(ConfigJson config)
        {
            return config.MoveLinks;
        }

        public async Task Preprocess(IMigrationContext migrationContext, IBatchMigrationContext batchContext, IList<WorkItem> sourceWorkItems, IList<WorkItem> targetWorkItems)
        {

        }

        public async Task<IEnumerable<JsonPatchOperation>> Process(IMigrationContext migrationContext, IBatchMigrationContext batchContext, WorkItem sourceWorkItem, WorkItem targetWorkItem)
        {
            IList<JsonPatchOperation> jsonPatchOperations = new List<JsonPatchOperation>();
            IEnumerable<WorkItemRelation> sourceRemoteLinks = sourceWorkItem.Relations?.Where(r => RelationHelpers.IsRemoteLinkType(migrationContext, r.Rel));

            if (sourceRemoteLinks != null && sourceRemoteLinks.Any())
            {
                foreach (WorkItemRelation sourceRemoteLink in sourceRemoteLinks)
                {
                    if (migrationContext.Config.MigrateRemoteLinkAsHyperLink   // save remote link as hyperlink
                        || sourceRemoteLink.Url.Contains(migrationContext.Config.TargetConnection.Account)) // exclude same organization
                    {
                        string url = ConvertRemoteLinkToHyperlink(sourceRemoteLink.Url);
                        WorkItemRelation targetRemoteLinkHyperlinkRelation = GetHyperlinkIfExistsOnTarget(targetWorkItem, url);

                        if (targetRemoteLinkHyperlinkRelation != null) // is on target
                        {
                            JsonPatchOperation remoteLinkHyperlinkAddOperation = MigrationHelpers.GetRelationAddOperation(targetRemoteLinkHyperlinkRelation);
                            jsonPatchOperations.Add(remoteLinkHyperlinkAddOperation);
                        }
                        else // is not on target
                        {
                            string comment = string.Empty;
                            if (sourceRemoteLink.Attributes.ContainsKey(Constants.RelationAttributeComment))
                            {
                                comment = $"{sourceRemoteLink.Attributes[Constants.RelationAttributeComment]}";
                            }

                            WorkItemRelation newRemoteLinkHyperlinkRelation = new WorkItemRelation();
                            newRemoteLinkHyperlinkRelation.Rel = Constants.Hyperlink;
                            newRemoteLinkHyperlinkRelation.Url = url;
                            newRemoteLinkHyperlinkRelation.Attributes = new Dictionary<string, object>();
                            newRemoteLinkHyperlinkRelation.Attributes[Constants.RelationAttributeComment] = comment;

                            JsonPatchOperation remoteLinkHyperlinkAddOperation = MigrationHelpers.GetRelationAddOperation(newRemoteLinkHyperlinkRelation);
                            jsonPatchOperations.Add(remoteLinkHyperlinkAddOperation);
                        }
                    }
                    else // save remote link as remote link
                    {
                        JsonPatchOperation remoteLinkHyperlinkAddOperation = MigrationHelpers.GetRelationAddOperation(sourceRemoteLink);
                        jsonPatchOperations.Add(remoteLinkHyperlinkAddOperation);
                    }
                }
            }

            return jsonPatchOperations;
        }

        private WorkItemRelation GetHyperlinkIfExistsOnTarget(WorkItem targetWorkItem, string href)
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


        /// <summary>
        /// Remote links returned refer to the REST reference, and we want the web reference.
        /// Format: https://account.visualstudio.com/ad443396-8473-4678-a2ba-0b1cf7cc8837/_apis/wit/workItems/3915636
        /// Web: https://account.visualstudio.com/ad443396-8473-4678-a2ba-0b1cf7cc8837/_workitems/edit/3915636
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string ConvertRemoteLinkToHyperlink(string url)
        {
            return url.Replace("_apis/wit/workitems", "_workitems/edit", StringComparison.OrdinalIgnoreCase);
        }
    }
}