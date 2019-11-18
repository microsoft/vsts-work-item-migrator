using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Identity.Client;
using Microsoft.VisualStudio.Services.Licensing.Client;
using static Microsoft.VisualStudio.Services.Graph.Constants;

namespace Common.Migration
{
    public class IdentityPreProcessor : IPhase1PreProcessor
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<IdentityPreProcessor>();
        private static string LicensedUsersGroup = SidIdentityHelper.ConstructWellKnownSid(0, 2048);
        private static SubjectDescriptor[] Groups = new[] { new SubjectDescriptor(SubjectType.VstsGroup, LicensedUsersGroup) };

        private IMigrationContext context;
        private GraphHttpClient graphClient;
        private LicensingHttpClient licensingHttpClient;
        private IdentityHttpClient identityHttpClient;

        public string Name => "Identity";

        public bool IsEnabled(ConfigJson config)
        {
            return config.EnsureIdentities;
        }

        public async Task Prepare(IMigrationContext context)
        {
            this.context = context;
            this.graphClient = context.TargetClient.Connection.GetClient<GraphHttpClient>();
            this.licensingHttpClient = context.TargetClient.Connection.GetClient<LicensingHttpClient>();
            this.identityHttpClient = context.TargetClient.Connection.GetClient<IdentityHttpClient>();
        }

        public async Task Process(IBatchMigrationContext batchContext)
        {
            object identityObject = null;
            string identityValue = null;
            HashSet<string> identitiesToProcess = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sourceWorkItem in batchContext.SourceWorkItems)
            {
                foreach (var field in context.IdentityFields)
                {
                    if (sourceWorkItem.Fields.TryGetValueIgnoringCase(field, out identityObject))
                    {
                        identityValue = (string)identityObject;
                        if (!string.IsNullOrEmpty(identityValue)
                            && identityValue.Contains("<") && identityValue.Contains(">") && (identityValue.Contains("@")))
                        {
                            // parse out email address from the combo string
                            identityValue = identityValue.Substring(identityValue.LastIndexOf("<") + 1, identityValue.LastIndexOf(">") - identityValue.LastIndexOf("<") - 1);

                            if (!identitiesToProcess.Contains(identityValue)
                                && !this.context.ValidatedIdentities.Contains(identityValue)
                                && !this.context.InvalidIdentities.Contains(identityValue))
                            {
                                Logger.LogTrace(LogDestination.File, $"Found identity {identityValue} in batch {batchContext.BatchId} which has not yet been validated for the target account");
                                identitiesToProcess.Add(identityValue);
                            }
                        }
                    }
                }
            }

            Logger.LogTrace(LogDestination.File, $"Adding {identitiesToProcess.Count} identities to the account for batch {batchContext.BatchId}");
            foreach (var identity in identitiesToProcess)
            {
                try
                {
                    var createUserResult = await RetryHelper.RetryAsync(async () =>
                    {
                        return await graphClient.CreateUserAsync(new GraphUserPrincipalNameCreationContext()
                        {
                            PrincipalName = identity
                        });
                    }, 5);

                    // using identity from createUserResult since the identity could be in a mangled format that ReadIdentities does not support
                    var identities = await RetryHelper.RetryAsync(async () =>
                    {
                        return await identityHttpClient.ReadIdentitiesAsync(IdentitySearchFilter.MailAddress, createUserResult.MailAddress);
                    }, 5);

                    if (identities.Count == 0)
                    {
                        Logger.LogWarning(LogDestination.File, $"Unable to add identity {identity} to the target account for batch {batchContext.BatchId}");
                        context.InvalidIdentities.Add(identity);
                    }
                    else
                    {
                        var assignResult = await RetryHelper.RetryAsync(async () =>
                        {
                            return await licensingHttpClient.AssignAvailableEntitlementAsync(identities[0].Id, dontNotifyUser: true);
                        }, 5);
                        context.ValidatedIdentities.Add(identity);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(LogDestination.File, ex, $"Unable to add identity {identity} to the target account for batch {batchContext.BatchId}");
                    context.InvalidIdentities.Add(identity);
                }
            }

            Logger.LogTrace(LogDestination.File, $"Completed adding {identitiesToProcess.Count} identities to the account for batch {batchContext.BatchId}");
        }
    }
}