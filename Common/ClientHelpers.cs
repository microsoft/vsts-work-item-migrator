using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Common.Config;
using Common.Migration;
using Logging;

namespace Common
{
    public class ClientHelpers
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<ClientHelpers>();

        public static WorkItemClientConnection CreateClient(ConfigConnection connection)
        {
            Uri url = new Uri(connection.Account.TrimEnd('/'));

            VssCredentials credentials;
            if (connection.UseIntegratedAuth)
            {
                credentials = new VssCredentials(true);
            }
            else
            {
                credentials = new VssBasicCredential("", connection.AccessToken);
            }

            return new WorkItemClientConnection(url, credentials);
        }

        public static string GetWorkItemApiEndpoint(string account, int workItemId)
        {
            account = account.TrimEnd('/');
            return $"{account}/_apis/wit/workItems/{workItemId}";
        }

        /// <summary>
        /// Returns the work item id from the end of endpoint uri string.
        /// </summary>
        /// <param name="endpointUri"></param>
        /// <returns></returns>
        public static int GetWorkItemIdFromApiEndpoint(string endpointUri)
        {
            string[] parts = endpointUri.Split('/');
            int lastIndex = parts.Length - 1;

            string result = parts[lastIndex];

            if (!result.Equals(string.Empty))
            {
                if (result.Contains("?"))
                {
                    string[] splitResult = result.Split('?');
                    return int.Parse(splitResult[0]);
                }
                return int.Parse(result);
            }
            else
            {
                result = parts[lastIndex - 1];
                return int.Parse(result);
            }
        }
        
        public static IEnumerable<T> GetProcessorInstances<T>(ConfigJson config) where T : IProcessor
        {
            return GetInstances<T>().Where(p => p.IsEnabled(config)).ToList();
        }

        public static IEnumerable<T> GetInstances<T>()
        {
            var commonAssemblyName = DependencyContext.Default.GetDefaultAssemblyNames().Where(a => a.Name.Equals("Common", StringComparison.OrdinalIgnoreCase)).First();
            var commonAssembly = Assembly.Load(commonAssemblyName);

            return commonAssembly.GetExportedTypes().Where(a => !a.GetTypeInfo().IsAbstract && a.GetConstructor(Type.EmptyTypes) != null && !a.GetConstructor(Type.EmptyTypes).ContainsGenericParameters)
                .OrderBy(b => b.GetTypeInfo().GetCustomAttribute<RunOrderAttribute>()?.Order ?? int.MaxValue)
                .Select(Activator.CreateInstance).OfType<T>().ToList();
        }

        public static async Task<ArtifactUriQueryResult> QueryArtifactUriToGetIdsFromUris(WorkItemTrackingHttpClient client, IEnumerable<string> artifactUris)
        {
            ArtifactUriQuery artifactUriQuery = new ArtifactUriQuery
            {
                ArtifactUris = artifactUris
            };
            
            return await WorkItemTrackingHelpers.GetIdsForUrisAsync(client, artifactUriQuery);
        }
        
        public static async Task<WorkItemQueryResult> QueryProjectToGetIds(WorkItemTrackingHttpClient client, string project, ArtifactUriQueryResult queryResult)
        {

            var ids = queryResult.ArtifactUrisQueryResult.SelectMany(x => x.Value).Select(x => x.Id).ToList();

            var query = new Wiql()
            {
                Query = $"Select [System.Id] From WorkItems Where [System.TeamProject] = '{project}' AND [System.Id] In ({string.Join(",", ids)})",
            };

            return await WorkItemTrackingHelpers.GetIdsForQueryAsync(client, query);
        }

        public static bool GetMigratedWorkItemId(ArtifactUriQueryResult queryResult, KeyValuePair<int, string> workItem, out int id)
        {
            IEnumerable<WorkItemReference> link = null;
            id = 0;
            if (queryResult.ArtifactUrisQueryResult.ContainsKey(workItem.Value))
            {
                link = queryResult.ArtifactUrisQueryResult[workItem.Value];
                if (link.Count() > 1)
                {
                    throw new Exception($"Found more than one work item with link {workItem.Value} in the target for workitem {workItem.Key}");
                }
                if (link.Count() == 1)
                    id = link.First().Id;
            }
            return link.Any();
        }

        public static string GetFirstPartOfLinkName(string name)
        {
            var parts = Regex.Split(name, "-");
            return parts[0];
        }

        public static int GetBatchCount(int items, int batchSize = Constants.BatchSize)
        {
            return (int)GetBatchCount((long)items, batchSize);
        }

        public static long GetBatchCount(long items, int batchSize = Constants.BatchSize)
        {
            return items / batchSize + (items % batchSize == 0 ? 0 : 1);
        }

        public static void ExecuteOnlyOnce(string key, Action action)
        {
            // we only want to validate a key once, so create a mutex of key
            // if it's new, execute the action, otherwise just grab and release the mutex
            // so that we can wait until the mutex has completed it's action.
            bool shouldRelease = true;
            bool isNewMutex;
            var mutex = new Mutex(true, key, out isNewMutex);
            try
            {
                // we got the mutex, and it belongs to this thread
                if (isNewMutex)
                {
                    // just because we got the ownership of the mutex, it doesn't mean the 
                    // key hasn't been validate.
                    action();
                }
                else
                {
                    // wait, just to immediately release since another thread was
                    // already validating this type.
                    shouldRelease = mutex.WaitOne();
                }
            }
            finally
            {
                if (shouldRelease)
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        public async static Task CopyStreamAsync(Stream source, Stream target)
        {
            var cancellationToken = new CancellationTokenSource();
            // cancel after 4 hours, primarily to capture cases where there is deadlock
            cancellationToken.CancelAfter(TimeSpan.FromHours(4));

            //81920 is the default used by System.IO.Stream.CopyTo
            await source.CopyToAsync(target, 81920, cancellationToken.Token);
        }

        // UNIT TEST
        public static void AddFailureReasonToWorkItemMigrationState(int sourceWorkItemId, FailureReason failureReason, IEnumerable<WorkItemMigrationState> workItemMigrationState)
        {
            WorkItemMigrationState state = workItemMigrationState.FirstOrDefault(a => a.SourceId == sourceWorkItemId);
            if (state == null)
            {
                // unexpected, every work item we process should have a work item migration state
                throw new Exception($"Unable to find {sourceWorkItemId} in the batch work item migration state");
            }
            else
            {
                state.MigrationState = WorkItemMigrationState.State.Error;
                state.FailureReason |= failureReason;
            }
        }

        public static Dictionary<int, FailureReason> GetNotMigratedWorkItemsFromWorkItemsMigrationState(IEnumerable<WorkItemMigrationState> workItemMigrationState)
        {
            return workItemMigrationState.Where(a => a.FailureReason != FailureReason.None).ToDictionary(k => k.SourceId, v => v.FailureReason);
        }
    }
}
