using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Common.Migration
{
    public class HyperLinksProcessor : IPhase2Processor
    {
        private static ILogger Logger { get; } = MigratorLogging.CreateLogger<HyperLinksProcessor>();
        public string Name => Constants.RelationPhaseHyperLinks;

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

            IEnumerable<WorkItemRelation> sourceHyperLinks = sourceWorkItem.Relations?.Where(r => r.Rel.Equals(Constants.Hyperlink, StringComparison.OrdinalIgnoreCase));

            if (sourceHyperLinks != null && sourceHyperLinks.Any())
            {
                foreach (var sourceHyperLink in sourceHyperLinks)
                {
                    var url = sourceHyperLink.Url;

                    // skip if the hyperlink is in the exclude list
                    if (migrationContext.Config.HyperLinkExcludes.Any(e => url.IndexOf(e, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        continue;
                    }

                    WorkItemRelation targetRemoteLinkHyperlinkRelation = GetHyperlinkIfExistsOnTarget(targetWorkItem, url);

                    if (targetRemoteLinkHyperlinkRelation != null) // is on target
                    {
                        JsonPatchOperation remoteLinkHyperlinkAddOperation = MigrationHelpers.GetRelationAddOperation(targetRemoteLinkHyperlinkRelation);
                        jsonPatchOperations.Add(remoteLinkHyperlinkAddOperation);
                    }
                    else // is not on target
                    {
                        var hyperlink = MigrationHelpers.GetHyperlinkAddOperation(url);
                        jsonPatchOperations.Add(hyperlink);
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
    }
}