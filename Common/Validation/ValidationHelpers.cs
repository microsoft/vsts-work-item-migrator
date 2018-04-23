using System;
using System.Linq;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Identity.Client;
using Microsoft.VisualStudio.Services.Security.Client;

namespace Common.Validation
{
    public class ValidationHelpers
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<ValidationHelpers>();

        //WORK_ITEM_READ has an int value of 16  
        private const int ReadPermission = 16;

        //WORK_ITEM_WRITE has an int value of 32  
        private const int WritePermission = 32;

        //BYPASS_RULES has an int value of 1048576  
        private const int BypassRulesPermission = 1048576;

        //SUPPRESS_NOTIFICATIONS has an int value of 2097152  
        private const int SuppressNotificationsPermission = 2097152;

        private static readonly Guid ProjectSecurityNamespace = new Guid("52d39943-cb85-4d7f-8fa8-c6baac873819");

        private static readonly Guid CssSecurityNamespace = new Guid("83e28ad4-2d72-4ceb-97b0-c7726d5502c3");

        public async static Task CheckReadPermission(WorkItemClientConnection client, string project)
        {
            await CheckPermission(client, project, CssSecurityNamespace, ReadPermission);
        }

        public async static Task CheckBypassRulesPermission(WorkItemClientConnection client, string project)
        {
            try
            {
                var securityHttpClient = client.Connection.GetClient<SecurityHttpClient>();
                var namespaces = await securityHttpClient.QuerySecurityNamespacesAsync(ProjectSecurityNamespace);
                if (namespaces.SelectMany(n => n.Actions).Any(a => a.Bit == BypassRulesPermission))
                {
                    await CheckPermission(client, project, ProjectSecurityNamespace, BypassRulesPermission);
                    await CheckPermission(client, project, ProjectSecurityNamespace, SuppressNotificationsPermission);
                    Logger.LogSuccess(LogDestination.All, $"Verified {client.Connection.AuthorizedIdentity.DisplayName} has bypass rules permission in {project}");
                    return;
                }
            }
            catch (ValidationException)
            {
                // no op, fallback to the legacy check
            }
            catch (Exception e) when (e.InnerException is VssUnauthorizedException)
            {
                throw new ValidationException(client.Connection.Uri.ToString(), (VssUnauthorizedException)e.InnerException);
            }
            catch (Exception e)
            {
                throw new ValidationException("An unexpected error occurred while validating project permissions", e);
            }

            // granular permissions not available, or the check failed.  falling back to legacy PCA check
            await CheckLegacyBypassRulesPermission(client, project);
        }

        /// <summary>
        /// Permission check for bypass rules for TFS < 2018 which only used project collection administrator to 
        /// check if you could bypass rules
        /// </summary>
        private static async Task CheckLegacyBypassRulesPermission(WorkItemClientConnection client, string project)
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
                    adminId = targetIdentity.Members.Where(a => string.Equals(a.Identifier, currentUserIdentity.Descriptor.Identifier, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
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

        private async static Task CheckPermission(WorkItemClientConnection client, string project, Guid securityNamespace, int requestedPermission)
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

            try
            {
                hasPermission = await securityHttpClient.HasPermissionAsync(
                    securityNamespace,
                    token,
                    requestedPermission,
                    false);
            }
            catch (Exception e)
            {
                throw new ValidationException($"An unexpected error occurred while trying to check permissions for project {project} in namespace {securityNamespace}", e);
            }

            if (hasPermission)
            {
                Logger.LogSuccess(LogDestination.All, $"Verified security permissions for {project} project");
            }
            else
            {
                throw new ValidationException($"You do not have the necessary security permissions for {project}, work item permission: {requestedPermission} is required.");
            }
        }
    }
}