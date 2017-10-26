using System;
using Microsoft.Extensions.Logging;
using Logging;

namespace WiMigrator
{
    class Program
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<Program>();

        static void Main(string[] args)
        {
            try
            {
                CommandLine commandLine = new CommandLine(args);
                commandLine.Run();
            }
            catch (Exception ex)
            {
                Logger.LogError(0, ex, "Closing application due to an Exception: ");
            }
            finally
            {
                MigratorLogging.LogSummary();
            }
        }
    }
}