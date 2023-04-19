using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Logging
{
    public class BulkLogger
    {
        // we can only let one thread in at a time to log to the file
        private static object lockObject = new object();
        private ConcurrentQueue<LogItem> bulkLoggerLogItems;
        private Stopwatch stopwatch;
        private Timer bulkLoggerCheckTimer;
        private string filePath;

        public BulkLogger()
        {
            this.bulkLoggerLogItems = new ConcurrentQueue<LogItem>();
            this.bulkLoggerCheckTimer = new Timer(BulkLoggerCheck, "Some state", TimeSpan.FromSeconds(LoggingConstants.CheckInterval), TimeSpan.FromSeconds(LoggingConstants.CheckInterval));
            this.stopwatch = Stopwatch.StartNew();
            this.filePath = GetFilePathBasedOnTime();
            Console.WriteLine($"Detailed logging sent to file: {Directory.GetCurrentDirectory()}\\{filePath}");
        }

        public void WriteToQueue(LogItem logItem)
        {
            if (logItem != null)
            {
                bulkLoggerLogItems.Enqueue(logItem);
            }
        }

        private string GetFilePathBasedOnTime()
        {
            try
            {
                string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                return $"WiMigrator_Migrate_{currentDateTime}.log";
            }
            catch (Exception ex)
            {
                string defaultFileName = "WiMigrator_Migrate_LogFile";
                Console.WriteLine($"Could not give log file a special name due to below Exception. Naming the file: \"{defaultFileName}\" instead.\n{ex}");
                return defaultFileName;
            }
        }

        private void BulkLoggerCheck(object state)
        {
            if (LogIntervalHasElapsed() || LogItemsHasReachedLimit())
            {
                BulkLoggerLog();
            }
        }

        private bool LogIntervalHasElapsed()
        {
            long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return elapsedMilliseconds >= LoggingConstants.LogInterval * 1000;
        }

        private bool LogItemsHasReachedLimit()
        {
            return bulkLoggerLogItems.Count >= LoggingConstants.LogItemsUnloggedLimit;
        }

        public void BulkLoggerLog()
        {
            stopwatch.Restart();
            StringBuilder outputBatchSB = new StringBuilder();

            if (bulkLoggerLogItems.Count > 0)
            {
                // We will only dequeue and write the count of items determined at the beginning of iteration.
                // Then we will have a predictable end in the case that items are being enqueued during the iteration.
                int startingLength = bulkLoggerLogItems.Count;

                for (int i = 0; i < startingLength; i++)
                {
                    if (bulkLoggerLogItems.TryDequeue(out LogItem logItem))
                    {
                        string output = logItem.OutputFormat(false, true);

                        if (logItem.Exception != null)
                        {
                            output = $"{output}\n{logItem.Exception}";
                        }
                        outputBatchSB.Append(output);
                        outputBatchSB.AppendLine();
                    }
                    else
                    {
                        continue;
                    }
                }

                WriteToFile(outputBatchSB.ToString());
            }
        }

        private void WriteToFile(string content)
        {
            try
            {
                LoggingRetryHelper.Retry(() =>
                {
                    AppendToFile(content);
                }, 5);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Cannot write to the log file because you are not authorized to access it. Please try running this application as administrator or moving it to a folder location that does not require special access.");
                throw;
            }
            catch (PathTooLongException)
            {
                Console.WriteLine($"Cannot write to the log file because the file path is too long. Please store your files for this WiMigrator application in a folder location with a shorter path name.");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot write to the log file: {ex.Message}");
            }
        }

        private void AppendToFile(string content)
        {
            // since we support multithreading, ensure only one thread
            // accesses the file at a time.
            lock (lockObject)
            {
                using (var streamWriter = File.AppendText(this.filePath))
                {
                    streamWriter.Write(content);
                }
            }
        }
    }
}
