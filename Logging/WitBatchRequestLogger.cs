using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Logging
{
    public static class WitBatchRequestLogger
    {
        public static void Log(IList<WitBatchRequest> witBatchRequests, IList<WitBatchResponse> witBatchResponses, int batchId)
        {
            try
            {
                string filePath = GetFilePathBasedOnTime(batchId);
                var canUseWitBatchResponses = witBatchRequests.Count == witBatchResponses?.Count;

                using (var streamWriter = File.AppendText(filePath))
                {
                    for (int i = 0; i < witBatchRequests.Count; i++)
                    {
                        var witBatchRequest = witBatchRequests[i];
                        streamWriter.WriteLine($"WIT BATCH REQUEST {i+1}:");
                        streamWriter.WriteLine();
                        streamWriter.WriteLine($"METHOD: {witBatchRequest.Method}");
                        streamWriter.WriteLine($"URI: {witBatchRequest.Uri}");
                        streamWriter.WriteLine();
                        streamWriter.WriteLine("BODY:");
                        streamWriter.WriteLine(witBatchRequest.Body);
                        streamWriter.WriteLine();

                        if (canUseWitBatchResponses)
                        {
                            var witBatchResponse = witBatchResponses[i];
                            streamWriter.WriteLine("RESPONSE CODE:");
                            streamWriter.WriteLine(witBatchResponse.Code);
                            streamWriter.WriteLine();
                            streamWriter.WriteLine("RESPONSE BODY:");
                            streamWriter.WriteLine(witBatchResponse.Body);
                            streamWriter.WriteLine();
                        }
                    }
                }
            }
            catch
            {
                // Do nothing because we don't want this to block Program execution.
            }
        }

        public static string GetFilePathBasedOnTime(int batchId)
        {
            try
            {
                string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-FFF");
                return $"WitBatchRequestsFromBatch{batchId}WithFailure{currentDateTime}.log";
            }
            catch (Exception ex)
            {
                string defaultFileName = "WitBatchRequestsFromBatchWithFailure_LogFile";
                Console.WriteLine($"Could not give log file a special name due to below Exception. Naming the file: \"{defaultFileName}\" instead.\n{ex}");
                return defaultFileName;
            }
        }
    }
}
