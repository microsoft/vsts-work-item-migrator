using System;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Common;
using Common.Migration;
using Common.Validation;
using Common.Config;
using Logging;
using System.Threading.Tasks;

namespace WiMigrator
{
    public class CommandLine
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<CommandLine>();

        private CommandLineApplication commandLineApplication;
        private string[] args;

        public CommandLine(params string[] args)
        {
            InitCommandLine(args);
        }

        private void InitCommandLine(params string[] args)
        {
            commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: true);
            this.args = args;
            ConfigureCommandLineParserWithOptions();
        }

        private void ConfigureCommandLineParserWithOptions()
        {
            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.FullName = "Work Item Migrator tool assists with copying work items" +
                " from one Visual Studio Team Services account to another.";

            CommandOption validate = commandLineApplication.Option(
                "-v | --validate <configurationfilename>",
                "Readiness check of the work item migration" +
                " based on the configuration settings",
                CommandOptionType.SingleValue
                );

            CommandOption migrate = commandLineApplication.Option(
                "-m | --migrate <configurationfilename>",
                "Migrate the work items based" +
                " on the configuration settings",
                CommandOptionType.SingleValue
                );

            commandLineApplication.OnExecute(async () =>
            {
                if (validate.HasValue())
                {
                    await ExecuteValidation(validate);
                }
                else if (migrate.HasValue())
                {
                    await ExecuteMigration(migrate);
                }
                else
                {
                    commandLineApplication.ShowHelp();
                }

                return 0;
            });
        }

        private async Task ExecuteValidation(CommandOption validate)
        {
            bool showedHelp = false;
            ConfigJson configJson = null;
            try
            {
                string configFileName = validate.Value();
                ConfigReaderJson configReaderJson = new ConfigReaderJson(configFileName);
                configJson = configReaderJson.Deserialize();

                var validatorContext = new ValidationContext(configJson);
                using (var heartbeat = new ValidationHeartbeatLogger(validatorContext.WorkItemsMigrationState, validatorContext, validatorContext.Config.HeartbeatFrequencyInSeconds))
                {
                    await new Validator(validatorContext).Validate();
                    heartbeat.Beat();
                }
            }
            catch (CommandParsingException e)
            {
                Logger.LogError(LogDestination.All, e, "Invalid command line option(s):");
                commandLineApplication.ShowHelp();
                showedHelp = true;
            }
            catch (Exception e) when (e is ValidationException)
            {
                Logger.LogError(LogDestination.All, e, "Validation error:");
            }
            catch (Exception e)
            {
                Logger.LogError(LogDestination.All, e, "Unexpected error:");
            }
            finally
            {
                if (!showedHelp && configJson != null)
                {
                    SendSummaryEmail(configJson);
                }
            }
        }

        private async Task ExecuteMigration(CommandOption migrate)
        {
            bool showedHelp = false;
            ConfigJson configJson = null;
            try
            {
                string configFileName = migrate.Value();
                ConfigReaderJson configReaderJson = new ConfigReaderJson(configFileName);
                configJson = configReaderJson.Deserialize();

                var validatorContext = new ValidationContext(configJson);
                using (var heartbeat = new ValidationHeartbeatLogger(validatorContext.WorkItemsMigrationState, validatorContext, validatorContext.Config.HeartbeatFrequencyInSeconds))
                {
                    await new Validator(validatorContext).Validate();
                    heartbeat.Beat();
                }

                //TODO: Create a common method to take the validator context and created a migration context
                var migrationContext = new MigrationContext(configJson);

                migrationContext.WorkItemIdsUris = validatorContext.WorkItemIdsUris;
                migrationContext.WorkItemTypes = validatorContext.TargetTypesAndFields;
                migrationContext.IdentityFields = validatorContext.IdentityFields;
                migrationContext.TargetAreaPaths = validatorContext.TargetAreaPaths;
                migrationContext.TargetIterationPaths = validatorContext.TargetIterationPaths;
                migrationContext.WorkItemsMigrationState = validatorContext.WorkItemsMigrationState;
                migrationContext.TargetIdToSourceHyperlinkAttributeId = validatorContext.TargetIdToSourceHyperlinkAttributeId;
                migrationContext.ValidatedWorkItemLinkRelationTypes = validatorContext.ValidatedWorkItemLinkRelationTypes;
                migrationContext.SourceFields = validatorContext.SourceFields;
                migrationContext.FieldsThatRequireSourceProjectToBeReplacedWithTargetProject = validatorContext.FieldsThatRequireSourceProjectToBeReplacedWithTargetProject;

                using (var heartbeat = new MigrationHeartbeatLogger(migrationContext.WorkItemsMigrationState, migrationContext.Config.HeartbeatFrequencyInSeconds))
                {
                    await new Migrator(migrationContext).Migrate();
                    heartbeat.Beat();
                }
            }
            catch (CommandParsingException e)
            {
                Logger.LogError(LogDestination.All, e, "Invalid command line option(s):");
                commandLineApplication.ShowHelp();
                showedHelp = true;
            }
            catch (Exception e) when (e is ValidationException)
            {
                Logger.LogError(LogDestination.All, e, "Validation error:");
            }
            catch (Exception e) when (e is MigrationException)
            {
                Logger.LogError(LogDestination.All, e, "Migration error:");
            }
            catch (Exception e)
            {
                Logger.LogError(LogDestination.All, e, $"Unexpected error: {e}");
            }
            finally
            {
                if (!showedHelp && configJson != null)
                {
                    SendSummaryEmail(configJson);
                }
            }
        }

        /// <summary>
        /// Run the WiMigrator application
        /// </summary>
        public void Run()
        {
            commandLineApplication.Execute(args);
        }

        private void SendSummaryEmail(ConfigJson configJson)
        {
            string logSummaryText = MigratorLogging.GetLogSummaryText();
            Emailer emailer = new Emailer();
            emailer.SendEmail(configJson, logSummaryText);
        }
    }
}
