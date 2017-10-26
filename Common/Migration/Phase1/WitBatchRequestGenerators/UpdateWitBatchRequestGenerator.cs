using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Newtonsoft.Json;
using Common.ApiWrappers;
using Logging;

namespace Common.Migration
{
    public class UpdateWitBatchRequestGenerator : BaseWitBatchRequestGenerator
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<UpdateWitBatchRequestGenerator>();
        
        public UpdateWitBatchRequestGenerator()
        {
        }

        public UpdateWitBatchRequestGenerator(IMigrationContext migrationContext, IBatchMigrationContext batchContext) : base(migrationContext, batchContext)
        {
        }

        public async override Task Write()
        {
            var sourceIdToWitBatchRequests = new List<(int SourceId, WitBatchRequest WitBatchRequest)>();
            foreach (var targetIdToSourceWorkItem in this.batchContext.TargetIdToSourceWorkItemMapping)
            {
                int targetId = targetIdToSourceWorkItem.Key;
                WorkItem sourceWorkItem = targetIdToSourceWorkItem.Value;

                if (WorkItemHasFailureState(sourceWorkItem))
                {
                    continue;
                }

                WitBatchRequest witBatchRequest = GenerateWitBatchRequestFromWorkItem(sourceWorkItem, targetId);
                if (witBatchRequest != null)
                {
                    sourceIdToWitBatchRequests.Add((sourceWorkItem.Id.Value, witBatchRequest));
                }

                DecrementIdWithinBatch(sourceWorkItem.Id);
            }

            var phase1ApiWrapper = new Phase1ApiWrapper();
            await phase1ApiWrapper.ExecuteWitBatchRequests(sourceIdToWitBatchRequests, this.migrationContext, batchContext);
        }

        private WitBatchRequest GenerateWitBatchRequestFromWorkItem(WorkItem sourceWorkItem, int targetId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json-patch+json");

            JsonPatchDocument jsonPatchDocument = CreateJsonPatchDocumentFromWorkItemFields(sourceWorkItem);
            
            string hyperlink = this.migrationContext.WorkItemIdsUris[sourceWorkItem.Id.Value];
            object attributeId = migrationContext.TargetIdToSourceHyperlinkAttributeId[targetId];

            JsonPatchOperation addHyperlinkWithCommentOperation = MigrationHelpers.GetHyperlinkAddOperation(hyperlink, sourceWorkItem.Rev.ToString(), attributeId);
            jsonPatchDocument.Add(addHyperlinkWithCommentOperation);

            string json = JsonConvert.SerializeObject(jsonPatchDocument);
            var witBatchRequest = new WitBatchRequest();
            witBatchRequest.Method = "PATCH";
            witBatchRequest.Headers = headers;
            witBatchRequest.Uri = $"/_apis/wit/workItems/{targetId}?{this.QueryString}";
            witBatchRequest.Body = json;

            return witBatchRequest;
        }
    }
}
