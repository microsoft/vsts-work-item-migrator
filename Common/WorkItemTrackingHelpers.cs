using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common.Migration;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;

namespace Common
{
    public class WorkItemTrackingHelpers
    {
        private static ILogger Logger { get; } = MigratorLogging.CreateLogger<WorkItemTrackingHelpers>();

        private readonly static string[] queryFields = new string[] { FieldNames.Watermark };

        public static Task<List<WorkItemField>> GetFields(WorkItemTrackingHttpClient client)
        {
            Logger.LogInformation(LogDestination.File, $"Getting fields for {client.BaseAddress.Host}");
            return RetryHelper.RetryAsync(async () =>
            {
                return await client.GetFieldsAsync();
            }, 5);
        }

        public static Task<List<WorkItemType>> GetWorkItemTypes(WorkItemTrackingHttpClient client, string project)
        {
            Logger.LogInformation(LogDestination.File, $"Getting work item types for {client.BaseAddress.Host}");
            return RetryHelper.RetryAsync(async () =>
            {
                return await client.GetWorkItemTypesAsync(project);
            }, 5);
        }

        /// <summary>
        /// Verify if the query exists in the database. Also a check used to validate connection
        /// </summary>
        /// <param name="client"></param>
        /// <param name="project"></param>
        /// <param name="queryName"></param>
        /// <returns></returns>
        public static Task<QueryHierarchyItem> GetQueryAsync(WorkItemTrackingHttpClient client, string project, string queryName)
        {
            Logger.LogInformation(LogDestination.File, $"Getting query for {client.BaseAddress.Host}");
            return RetryHelper.RetryAsync(async () =>
            {
                return await client.GetQueryAsync(project, queryName, expand: QueryExpand.Wiql);
            }, 5);
        }

        /// <summary>
        /// Given an int array, get the list of workitems. Retries 5 times for connection timeouts etc.  
        /// </summary>
        /// <param name="client"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async static Task<IList<WorkItem>> GetWorkItemsAsync(WorkItemTrackingHttpClient client, IEnumerable<int> ids, IEnumerable<string> fields = null, WorkItemExpand? expand = null)
        {
            Logger.LogDebug(LogDestination.File, $"Getting work items for {client.BaseAddress.Host}");

            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return await RetryHelper.RetryAsync(async () =>
            {
                return await client.GetWorkItemsAsync(ids, fields: fields, expand: expand);
            }, 5);
        }

        public async static Task<List<WorkItemUpdate>> GetWorkItemUpdatesAsync(WorkItemTrackingHttpClient client, int id, int skip = 0)
        {
            return await RetryHelper.RetryAsync(async () =>
            {
                return await client.GetUpdatesAsync(id, Constants.PageSize, skip: skip);
            }, 5);
        }

        public async static Task<List<WorkItemRelationType>> GetRelationTypesAsync(WorkItemTrackingHttpClient client)
        {
            return await RetryHelper.RetryAsync(async () =>
            {
                return await client.GetRelationTypesAsync();
            }, 5);
        }

        public async static Task<AttachmentReference> CreateAttachmentAsync(WorkItemTrackingHttpClient client, MemoryStream uploadStream)
        {
            return await RetryHelper.RetryAsync(async () =>
            {
                // clone the stream since if upload fails it disploses the underlying stream
                using (var clonedStream = new MemoryStream())
                {
                    await uploadStream.CopyToAsync(clonedStream);

                    // reset position for both streams
                    uploadStream.Position = 0;
                    clonedStream.Position = 0;

                    return await client.CreateAttachmentAsync(clonedStream);
                }
            }, 5);
        }

        public async static Task<AttachmentReference> CreateAttachmentChunkedAsync(WorkItemTrackingHttpClient client, VssConnection connection, MemoryStream uploadStream, int chunkSizeInBytes)
        {
            // it's possible for the attachment to be empty, if so we can't used the chunked upload and need
            // to fallback to the normal upload path.
            if (uploadStream.Length == 0)
            {
                return await CreateAttachmentAsync(client, uploadStream);
            }

            var requestSettings = new VssHttpRequestSettings
            {
                SendTimeout = TimeSpan.FromMinutes(5)
            };
            var httpClient = new HttpClient(new VssHttpMessageHandler(connection.Credentials, requestSettings));

            // first create the attachment reference.  
            // can't use the WorkItemTrackingHttpClient since it expects either a file or a stream.
            var attachmentReference = await RetryHelper.RetryAsync(async () =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{connection.Uri}_apis/wit/attachments?uploadType=chunked&api-version=3.2");
                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsAsync<AttachmentReference>();
                }
                else
                {
                    var exceptionResponse = await response.Content.ReadAsAsync<ExceptionResponse>();
                    throw new Exception(exceptionResponse.Message);
                }
            }, 5);

            // now send up each chunk
            var totalNumberOfBytes = uploadStream.Length;

            // if number of chunks divides evenly, no need to add an extra chunk
            var numberOfChunks = ClientHelpers.GetBatchCount(totalNumberOfBytes, chunkSizeInBytes);
            for (var i = 0; i < numberOfChunks; i++)
            {
                var chunkBytes = new byte[chunkSizeInBytes];
                // offset is always 0 since read moves position forward
                var chunkLength = uploadStream.Read(chunkBytes, 0, chunkSizeInBytes);

                var result = await RetryHelper.RetryAsync(async () =>
                {
                    // manually create the request since the WorkItemTrackingHttpClient does not support chunking
                    var content = new ByteArrayContent(chunkBytes, 0, chunkLength);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    content.Headers.ContentLength = chunkLength;
                    content.Headers.ContentRange = new ContentRangeHeaderValue(i * chunkSizeInBytes, i * chunkSizeInBytes + chunkLength - 1, totalNumberOfBytes);

                    var chunkRequest = new HttpRequestMessage(HttpMethod.Put, $"{connection.Uri}_apis/wit/attachments/" + attachmentReference.Id + "?uploadType=chunked&api-version=3.2") { Content = content };
                    var chunkResponse = await httpClient.SendAsync(chunkRequest);
                    if (!chunkResponse.IsSuccessStatusCode)
                    {
                        // there are two formats for the exception, so detect both.
                        var responseContentAsString = await chunkResponse.Content.ReadAsStringAsync();
                        var criticalException = JsonConvert.DeserializeObject<CriticalExceptionResponse>(responseContentAsString);
                        var exception = JsonConvert.DeserializeObject<ExceptionResponse>(responseContentAsString);

                        throw new Exception(criticalException.Value?.Message ?? exception.Message);
                    }

                    return chunkResponse.StatusCode;
                }, 5);
            }

            return attachmentReference;
        }

        public async static Task<Stream> GetAttachmentAsync(WorkItemTrackingHttpClient client, Guid id)
        {
            return await RetryHelper.RetryAsync(async () =>
            {
                return await client.GetAttachmentContentAsync(id);
            }, 5);
        }

        /// <summary>
        /// Gets all the work item ids for the query, with special handling for the case when the query results
        /// can exceed the result cap.
        /// </summary>
        public async static Task<IDictionary<int, string>> GetWorkItemIdAndReferenceLinksAsync(WorkItemTrackingHttpClient client, string project, string queryName, string postMoveTag, int queryPageSize)
        {
            Logger.LogInformation(LogDestination.File, $"Getting work item ids for {client.BaseAddress.Host}");

            var queryHierarchyItem = await GetQueryAsync(client, project, queryName);
            var workItemIdsUris = new Dictionary<int, string>();

            var baseQuery = ParseQueryForPaging(queryHierarchyItem.Wiql, postMoveTag);
            var watermark = 0;
            var id = 0;
            var page = 0;

            while (true)
            {
                Logger.LogInformation(LogDestination.File, $"Getting work item ids page {page++} with last id {id} for {client.BaseAddress.Host}");

                var wiql = new Wiql
                {
                    Query = GetPageableQuery(baseQuery, watermark, id)
                };

                var queryResult = await RetryHelper.RetryAsync(async () =>
                {
                    return await client.QueryByWiqlAsync(wiql, project: project, top: queryPageSize);
                }, 5);

                workItemIdsUris.AddRange(queryResult.WorkItems.Where(w => !workItemIdsUris.ContainsKey(w.Id)).ToDictionary(k => k.Id, v => v.Url));

                Logger.LogTrace(LogDestination.File, $"Getting work item ids page {page} with last id {id} for {client.BaseAddress.Host} returned {queryResult.WorkItems.Count()} results and total result count is {workItemIdsUris.Count}");
                
                //keeping a list as well because the Dictionary doesnt guarantee ordering 
                List<int> workItemIdsPage = queryResult.WorkItems.Select(k => k.Id).ToList();
                if (!workItemIdsPage.Any())
                {
                    break;
                }
                else
                {
                    id = workItemIdsPage.Last();
                    var workItem = await RetryHelper.RetryAsync(async () =>
                    {
                        return await client.GetWorkItemAsync(id, queryFields);
                    }, 5);

                    watermark = (int)(long)workItem.Fields[FieldNames.Watermark];
                }
            }

            return workItemIdsUris;
        }

        /// <summary>
        /// Strips the ORDER BY from the query so we can append our own order by clause
        /// and injects the tag into the where clause to skip any work items that have
        /// been completely migrated.
        /// </summary>
        public static string ParseQueryForPaging(string query, string postMoveTag)
        {
            var lastOrderByIndex = query.LastIndexOf(" ORDER BY ", StringComparison.OrdinalIgnoreCase);
            var queryWithNoOrderByClause = query.Substring(0, lastOrderByIndex > 0 ? lastOrderByIndex : query.Length);

            if (!string.IsNullOrEmpty(postMoveTag))
            {
                var postMoveTagClause = !string.IsNullOrEmpty(postMoveTag) ? $"System.Tags NOT CONTAINS '{postMoveTag}'" : string.Empty;
                return $"{InjectWhereClause(queryWithNoOrderByClause, postMoveTagClause)}";
            }
            else
            {
                return queryWithNoOrderByClause;
            }
        }

        /// <summary>
        /// Adds the watermark and id filter and order clauses
        /// </summary>
        public static string GetPageableQuery(string query, int watermark, int id)
        {
            var pageableClause = $"((System.Watermark > {watermark}) OR (System.Watermark = {watermark} AND System.Id > {id}))";
            return $"{InjectWhereClause(query, pageableClause)} ORDER BY System.Watermark, System.Id";
        }

        private static string InjectWhereClause(string query, string clause) 
        {
            var lastWhereClauseIndex = query.LastIndexOf(" WHERE ", StringComparison.OrdinalIgnoreCase);
            if (lastWhereClauseIndex > 0)
            {
                query = $"{query.Substring(0, lastWhereClauseIndex)} WHERE ({query.Substring(lastWhereClauseIndex + " WHERE ".Length)}) AND ";
            }
            else
            {
                query = $"{query} WHERE ";
            }

            return $"{query}{clause}";
        }

        public async static Task<WorkItemClassificationNode> GetClassificationNode(WorkItemTrackingHttpClient client, string project, TreeStructureGroup structureGroup)
        {
            Logger.LogInformation(LogDestination.File, $"Getting classification node for {client.BaseAddress.Host}");
            return await RetryHelper.RetryAsync(async () =>
            {
                return await client.GetClassificationNodeAsync(project, structureGroup, depth: int.MaxValue);
            }, 5);
        }

        public async static Task<List<WorkItemClassificationNode>> GetClassificationNodes(WorkItemTrackingHttpClient client, string project)
        {
            Logger.LogInformation(LogDestination.File, $"Getting all classification nodes for {client.BaseAddress.Host}");
            return await RetryHelper.RetryAsync(async () =>
            {
                return await client.GetRootNodesAsync(project, depth: int.MaxValue);
            }, 5);
        }

        public async static Task<ArtifactUriQueryResult> GetIdsForUrisAsync(WorkItemTrackingHttpClient client, ArtifactUriQuery query)
        {
            return await RetryHelper.RetryAsync(async () =>
             {
                 return await client.GetWorkItemIdsForArtifactUrisAsync(query);
             }, 5);
        }
    }
}
