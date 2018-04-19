using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.Validation
{
    [RunOrder(3)]
    public class ValidateSourceQuery : IConfigurationValidator
    {
        private ILogger Logger { get; } = MigratorLogging.CreateLogger<ValidateSourceQuery>();

        public string Name => "Source query";

        public async Task Validate(IValidationContext context)
        {
            await VerifyQueryExistsAndIsValid(context);
            await RunQuery(context);
        }

        private async Task VerifyQueryExistsAndIsValid(IValidationContext context)
        {
            Logger.LogInformation(LogDestination.File, "Checking if the migration query exists in the source project");
            QueryHierarchyItem query;
            try
            {
                query = await WorkItemTrackingHelpers.GetQueryAsync(context.SourceClient.WorkItemTrackingHttpClient, context.Config.SourceConnection.Project, context.Config.Query);
            }
            catch (Exception e)
            {
                throw new ValidationException("Unable to read the migration query", e);
            }

            if (query.QueryType != QueryType.Flat)
            {
                throw new ValidationException("Only flat queries are supported for migration");
            }
        }

        private async Task RunQuery(IValidationContext context)
        {
            Logger.LogInformation(LogDestination.File, "Running the migration query in the source project");

            try
            {
                var workItemUris = await WorkItemTrackingHelpers.GetWorkItemIdAndReferenceLinksAsync(
                    context.SourceClient.WorkItemTrackingHttpClient,
                    context.Config.SourceConnection.Project,
                    context.Config.Query,
                    context.Config.SourcePostMoveTag,
                    context.Config.QueryPageSize - 1 /* Have to subtract -1 from the page size due to a bug in how query interprets page size */);

                    context.WorkItemIdsUris = new ConcurrentDictionary<int, string>(workItemUris);
            }
            catch (Exception e)
            {
                throw new ValidationException("Unable to run the migration query", e);
            }
        }
    }
}
