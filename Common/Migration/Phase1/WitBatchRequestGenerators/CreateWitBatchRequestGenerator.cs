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
    public class CreateWitBatchRequestGenerator : BaseWitBatchRequestGenerator
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<CreateWitBatchRequestGenerator>();

        public CreateWitBatchRequestGenerator(IMigrationContext migrationContext, IBatchMigrationContext batchContext) : base(migrationContext, batchContext)
        {
        }

        public async override Task Write()
        {
            var sourceIdToWitBatchRequests = new List<(int SourceId, WitBatchRequest WitBatchRequest)>();
            foreach (var sourceWorkItem in this.batchContext.SourceWorkItems)
            {
                if (WorkItemHasFailureState(sourceWorkItem))
                {
                    continue;
                }

                WitBatchRequest witBatchRequest = GenerateWitBatchRequestFromWorkItem(sourceWorkItem);
                if (witBatchRequest != null)
                {
                    sourceIdToWitBatchRequests.Add((sourceWorkItem.Id.Value, witBatchRequest));
                }

                DecrementIdWithinBatch(sourceWorkItem.Id);
            }

            var phase1ApiWrapper = new Phase1ApiWrapper();
            await phase1ApiWrapper.ExecuteWitBatchRequests(sourceIdToWitBatchRequests, this.migrationContext, batchContext, verifyOnFailure: true);
        }

        private WitBatchRequest GenerateWitBatchRequestFromWorkItem(WorkItem sourceWorkItem)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json-patch+json");

            JsonPatchDocument jsonPatchDocument = CreateJsonPatchDocumentFromWorkItemFields(sourceWorkItem);

            JsonPatchOperation insertIdAddOperation = GetInsertBatchIdAddOperation();
            jsonPatchDocument.Add(insertIdAddOperation);

            // add hyperlink to source WorkItem
            string sourceWorkItemApiEndpoint = ClientHelpers.GetWorkItemApiEndpoint(this.migrationContext.Config.SourceConnection.Account, sourceWorkItem.Id.Value);
            JsonPatchOperation addHyperlinkAddOperation = MigrationHelpers.GetHyperlinkAddOperation(sourceWorkItemApiEndpoint, sourceWorkItem.Rev.ToString());
            jsonPatchDocument.Add(addHyperlinkAddOperation);

            string json = JsonConvert.SerializeObject(jsonPatchDocument);

            string workItemType = jsonPatchDocument.Find(a => a.Path.Contains(FieldNames.WorkItemType)).Value as string;

            var witBatchRequest = new WitBatchRequest();
            witBatchRequest.Method = "PATCH";
            witBatchRequest.Headers = headers;
            witBatchRequest.Uri = $"/{this.migrationContext.Config.TargetConnection.Project}/_apis/wit/workItems/${workItemType}?{this.QueryString}";
            witBatchRequest.Body = json;

            return witBatchRequest;
        }
    }
}
