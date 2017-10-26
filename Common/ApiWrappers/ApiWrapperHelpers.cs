using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Common.Migration;
using Logging;

namespace Common.ApiWrappers
{
    public class ApiWrapperHelpers
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<ApiWrapperHelpers>();

        public static async Task<IList<WitBatchResponse>> ExecuteBatchRequest(WorkItemTrackingHttpClient targetWorkItemTrackingClient, IList<WitBatchRequest> witBatchRequests)
        {
            //we cannot add retrylogic here as we will need to reconstruct the entire content 
            try
            {
                return await targetWorkItemTrackingClient.ExecuteBatchRequest(witBatchRequests);
            }
            catch (Exception e)
            {
                Logger.LogError(LogDestination.File, $"Exception in MakeRequest {e.Message}");
                throw e;
                //we continue migration even if something failed 
            }
        }

        public static void HandleCriticalError(WitBatchResponse witBatchResponse, IEnumerable<int> sourceIds, IBatchMigrationContext batchContext)
        {
            CriticalExceptionResponse exceptionResponse = witBatchResponse.ParseBody<CriticalExceptionResponse>();
            string exceptionMessage = exceptionResponse.Value.Message;
            Logger.LogError(LogDestination.All, $"Exception during batch {batchContext.BatchId}: {exceptionMessage}");
            MarkBatchAsFailed(batchContext, sourceIds, FailureReason.CriticalError);
        }
        
        public static bool ResponsesLackExpectedData(IList<WitBatchResponse> witBatchResponses, IList<(int SourceId, WitBatchRequest WitBatchRequest)> sourceIdToWitBatchRequests)
        {
            return (witBatchResponses == null || witBatchResponses.Count == 0) && sourceIdToWitBatchRequests.Any();
        }

        public static void MarkBatchAsFailed(IBatchMigrationContext batchContext, IEnumerable<int> sourceIds, FailureReason failureReason)
        {
            foreach (var sourceId in sourceIds)
            {
                ClientHelpers.AddFailureReasonToWorkItemMigrationState(sourceId, failureReason, batchContext.WorkItemMigrationState);
            }
        }

        /// <summary>
        /// Handles any HttpStatusCode other than HttpStatusCode.OK (200) any other code such as 400, 403, 500 or 503 (technically we should not get 500 or 503 ) 
        /// </summary>
        /// <param name="witBatchResponse"></param>
        /// <param name="migrationContext"></param>
        /// <param name="statusCode"></param>
        /// <param name="failureReason"></param>
        public static void HandleUnsuccessfulWitBatchResponse(WitBatchResponse witBatchResponse, int batchIteration, IMigrationContext migrationContext, int statusCode, string workItemEndpoint)
        {
            string responseBodyDefault = string.Empty;
            string exceptionMessage;

            if (witBatchResponse.Body != null)
            {
                responseBodyDefault = witBatchResponse.Body;
            }

            exceptionMessage = $"Status Code: {statusCode}. A work item failed to be migrated during batch iteration {batchIteration}. Work item endpoint: {workItemEndpoint}. {responseBodyDefault}";

            Logger.LogError(LogDestination.File, exceptionMessage);
        }

        /// <summary>
        /// Handles any HttpStatusCode other than HttpStatusCode.OK (200) any other code such as 400, 403, 500 or 503 (technically we should not get 500 or 503 ) 
        /// </summary>
        /// <param name="witBatchResponse"></param>
        /// <param name="batchIdToWorkItemIdMapping"></param>
        /// <param name="batchIteration"></param>
        /// <param name="migrationContext"></param>
        /// <param name="statusCode"></param>
        /// <param name="failureReason"></param>
        public static void HandleUnsuccessfulWitBatchResponse(WitBatchResponse witBatchResponse, (int SourceId, WitBatchRequest WitBatchRequest) sourceIdToWitBatchRequest, IBatchMigrationContext batchContext, int statusCode, FailureReason failureReason)
        {
            string responseBodyDefault = string.Empty;
            if (witBatchResponse.Body != null)
            {
                responseBodyDefault = witBatchResponse.Body;
            }

            string exceptionMessage = $"Status Code: {statusCode}. Work item {sourceIdToWitBatchRequest.SourceId} failed during batch {batchContext.BatchId}.{responseBodyDefault}";

            ClientHelpers.AddFailureReasonToWorkItemMigrationState(sourceIdToWitBatchRequest.SourceId, failureReason, batchContext.WorkItemMigrationState);
            Logger.LogError(LogDestination.File, exceptionMessage);
        }

        /// <summary>
        /// for creating work items, the batch call is not idempotent so we need to 
        /// verify if on exception the call actually succeeded.  if it did, mark it
        /// as failed and throw a permanent exception to ensure we don't retry.  
        /// </summary>
        public async static Task<Exception> HandleBatchException(
            Guid requestId,
            Exception exception,
            IMigrationContext migrationContext,
            IBatchMigrationContext batchContext,
            IEnumerable<int> sourceIds)
        {
            // if it's permanent don't bother to check if it possibly succeeded
            if (exception is RetryPermanentException)
            {
                return exception;
            }
            else
            {
                Logger.LogInformation(LogDestination.File, $"Checking if exception for {requestId} resulted in work items being migrated.");

                var artifactUris = migrationContext.WorkItemIdsUris.Where(w => sourceIds.Contains(w.Key)).Select(w => w.Value);
                var queryResult = await ClientHelpers.QueryArtifactUriToGetIdsFromUris(migrationContext.TargetClient.WorkItemTrackingHttpClient, artifactUris);

                var anyWorkItemsCreated = false;
                foreach (var idToUri in migrationContext.WorkItemIdsUris.Where(w => sourceIds.Contains(w.Key)))
                {
                    if (ClientHelpers.GetMigratedWorkItemId(queryResult, idToUri, out int migratedId))
                    {
                        anyWorkItemsCreated = true;
                    }
                }

                if (anyWorkItemsCreated)
                {
                    // mark all in batch as failed if any work item was found
                    ApiWrapperHelpers.MarkBatchAsFailed(batchContext, sourceIds, FailureReason.CreateBatchFailureError);

                    return new RetryPermanentException($"Validated work items were migrated for {requestId} even though we got an exception, skipping any further processing.", exception);
                }
                else
                {
                    return exception;
                }
            }
        }
    }
}
