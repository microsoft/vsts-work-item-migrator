using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Logging
{
    public class LoggingRetryHelper
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<LoggingRetryHelper>();

        public static void Retry(Action function, int retryCount, int secsDelay = 1)
        {
            Exception exception = null;
            bool succeeded = true;
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    succeeded = true;
                    function();
                    return;
                }
                catch (Exception ex)
                {
                    exception = ex;

                    succeeded = false;
                    Logger.LogInformation(LogDestination.Console, $"Sleeping for {secsDelay} seconds and retrying again for logging");

                    var task = Task.Delay(secsDelay * 1000);
                    task.Wait();
                    
                    // add 1 second to delay so that each delay is slightly incrementing in wait time
                    secsDelay += 1;
                }
                finally
                {
                    if (succeeded && i >= 1)
                    {
                        Logger.LogSuccess(LogDestination.File, $"Logging request succeeded.");
                    }
                }
            }

            if (exception is null)
            {
                throw new LoggingRetryExhaustedException($"Retry count exhausted for logging request.");
            }
            else
            {
                throw exception;
            }
        }
    }
}
