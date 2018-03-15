using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Identity.Client;
using Microsoft.VisualStudio.Services.Security.Client;
using Logging;
using System.Threading.Tasks;

namespace Common.Validation
{
    public class ValidationHelpers
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<ValidationHelpers>();

        //WORK_ITEM_READ has an int value of 16  
        public const int ReadPermission = 16;

        //WORK_ITEM_WRITE has an int value of 32  
        public const int WritePermission = 32;

        //WORK_ITEM_WRITE has an int value of 1048576  
        public const int BypassRulesPermission = 1048576;

        //WORK_ITEM_WRITE has an int value of 2097152  
        public const int SuppressNotificationsPermission = 2097152;

        public async static Task CheckConnection(WorkItemClientConnection client, string project, int requestedPermission)
        {
            Logger.LogInformation($"Checking security permissions for {client.Connection.AuthorizedIdentity.DisplayName} in {project}");
            bool hasPermission = false;

            SecurityHttpClient securityHttpClient = null;
            WorkItemClassificationNode result = null;
            try
            {
                securityHttpClient = client.Connection.GetClient<SecurityHttpClient>();
                result = await WorkItemTrackingHelpers.GetClassificationNode(client.WorkItemTrackingHttpClient, project, TreeStructureGroup.Areas);
            }
            catch (Exception e) when (e.InnerException is VssUnauthorizedException)
            {
                throw new ValidationException(client.Connection.Uri.ToString(), (VssUnauthorizedException)e.InnerException);
            }
            catch (Exception e)
            {
                throw new ValidationException("An unexpected error occurred while reading the classification nodes to validate project permissions", e);
            }

            //construct the token by appending the id
            string token = $"vstfs:///Classification/Node/{result.Identifier}";

            //WORK_ITEM guid is hardcoded below
            //securityNameSpaceId for WORK_ITEM is 83e28ad4-2d72-4ceb-97b0-c7726d5502c3 
            try
            {
                hasPermission = await securityHttpClient.HasPermissionAsync(
                    new Guid("83e28ad4-2d72-4ceb-97b0-c7726d5502c3"),
                    token,
                    requestedPermission,
                    false);
            }
            catch (Exception e)
            {
                throw new ValidationException($"An unexpected error occurred while trying to check permissions for project {project}", e);
            }

            if (hasPermission)
            {
                Logger.LogSuccess(LogDestination.All, $"Verified security permissions for {project} project");
            }
            else
            {
                throw new ValidationException($"You do not have the necessary security permissions for {project}, work item {(requestedPermission == WritePermission ? "write" : "read")} permissions are required.");
            }
        }

        public async static Task CheckIdentity(WorkItemClientConnection client, string project)
        {
            IdentityHttpClient targetIdentityClient = null;
            var currentUserIdentity = client.Connection.AuthorizedIdentity;
            IdentityDescriptor adminId = null;
            Logger.LogInformation($"Checking administrative permissions for {client.Connection.AuthorizedIdentity.DisplayName} in {project}");
            try
            {
                targetIdentityClient = client.Connection.GetClient<IdentityHttpClient>();

                var targetIdentity = (await targetIdentityClient.ReadIdentitiesAsync(IdentitySearchFilter.General, "Project Collection Administrators", queryMembership: QueryMembership.Expanded)).FirstOrDefault();
                if (targetIdentity != null)
                {
                    //Check if the current user account running the tool is a member of project collection administrators 
                    #region HackRegion
                    adminId = targetIdentity.Members.Where(a => string.Equals(a.Identifier, currentUserIdentity.Descriptor.Identifier, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    //We can replace the above code with the following two statements when they work
                    //var me = targetIdentityClient.GetIdentitySelfAsync().Result;
                    //var isMember = targetIdentityClient.IsMember(targetIdentity.Descriptor, currentUserIdentity.Descriptor).Result; 
                    #endregion
                }
            }
            catch (Exception e) when (e.InnerException is VssUnauthorizedException)
            {
                throw new ValidationException(client.Connection.Uri.ToString(), (VssUnauthorizedException)e.InnerException);
            }
            catch (Exception e)
            {
                throw new ValidationException("An unexpected error occurred while checking administrative permissions", e);
            }

            if (adminId != null)
            {
                Logger.LogSuccess(LogDestination.All, $"Verified {client.Connection.AuthorizedIdentity.DisplayName} is a Project Collection Administrator in {project}");
            }
            else
            {
                throw new ValidationException($"{currentUserIdentity.Descriptor.Identifier} is not a Project Collection Administrator in {project}. Please follow https://www.visualstudio.com/en-us/docs/setup-admin/add-administrator-tfs on how to add the account to the project collection administrators");
            }
        }
    }
}