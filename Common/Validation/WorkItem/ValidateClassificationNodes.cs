using System;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.Validation
{
    public class ValidateClassificationNodes : IWorkItemValidator
    {
        private static ILogger Logger { get; } = MigratorLogging.CreateLogger<ValidateClassificationNodes>();

        public string Name => "Classification nodes";

        public async Task Prepare(IValidationContext context)
        {
            context.RequestedFields.Add(FieldNames.AreaPath);
            context.RequestedFields.Add(FieldNames.IterationPath);

            Logger.LogInformation("Reading the area and iteration paths from the source and target accounts");

            try
            {
                var classificationNodes = await WorkItemTrackingHelpers.GetClassificationNodes(context.TargetClient.WorkItemTrackingHttpClient, context.Config.TargetConnection.Project);
                var nodes = new AreaAndIterationPathTree(classificationNodes);
                context.TargetAreaPaths = nodes.AreaPathList;
                context.TargetIterationPaths = nodes.IterationPathList;
            }
            catch (Exception e)
            {
                throw new ValidationException("Unable to read the classification nodes on the source", e);
            }
        }

        public async Task Validate(IValidationContext context, WorkItem workItem)
        {
            var areaPath = (string)workItem.Fields[FieldNames.AreaPath];
            var iterationPath = (string)workItem.Fields[FieldNames.IterationPath];

            if (!AreaAndIterationPathTree.TryReplaceLeadingProjectName(areaPath, context.Config.SourceConnection.Project, context.Config.TargetConnection.Project, out areaPath))
            {
                // This is a fatal error because this implies the query is cross project which we do not support, so bail out immediately
                throw new ValidationException($"Could not find source project from area path {workItem.Fields[FieldNames.AreaPath]} for work item with id {workItem.Id}");
            }

            if (!AreaAndIterationPathTree.TryReplaceLeadingProjectName(iterationPath, context.Config.SourceConnection.Project, context.Config.TargetConnection.Project, out iterationPath))
            {
                // This is a fatal error because this implies the query is cross project which we do not support, so bail out immediately
                throw new ValidationException($"Could not find source project from iteration path {workItem.Fields[FieldNames.IterationPath]} for work item with id {workItem.Id}");
            }

            if (!context.ValidatedAreaPaths.Contains(areaPath) && !context.SkippedAreaPaths.Contains(areaPath))
            {
                if (!context.TargetAreaPaths.Contains(areaPath))
                {
                    // only log if we've added this for the first time
                    if (context.SkippedAreaPaths.Add(areaPath))
                    {
                        Logger.LogWarning(LogDestination.File, $"Area path {areaPath} does not exist on the target for work item with id {workItem.Id}");
                    }
                }
                else
                {
                    context.ValidatedAreaPaths.Add(areaPath);
                }
            }

            if (!context.ValidatedIterationPaths.Contains(iterationPath) && !context.SkippedIterationPaths.Contains(iterationPath))
            {
                if (!context.TargetIterationPaths.Contains(iterationPath))
                {
                    // only log if we've added this for the first time
                    if (context.SkippedIterationPaths.Add(iterationPath))
                    {
                        Logger.LogWarning(LogDestination.File, $"Iteration path {iterationPath} does not exist on the target for work item with id {workItem.Id}");
                    }
                }
                else
                {
                    context.ValidatedIterationPaths.Add(iterationPath);
                }
            }

            // If we're skipping instead of using default values, add the work item to the skipped list
            // if it's area or iteration path does not exist on the target.
            if ((context.Config.SkipWorkItemsWithMissingIterationPath && context.SkippedIterationPaths.Contains(iterationPath)) ||
                (context.Config.SkipWorkItemsWithMissingAreaPath && context.SkippedAreaPaths.Contains(areaPath)))
            {
                context.SkippedWorkItems.Add(workItem.Id.Value);
            }
        }
    }
}
