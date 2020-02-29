using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.Migration
{

    public class ClassificationNodesPreProcessor : IPhase1PreProcessor
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<ClassificationNodesPreProcessor>();
        private IMigrationContext context;

        public string Name => "Classification Nodes (Area Paths/Iterations)";

        public bool IsEnabled(ConfigJson config)
        {
            return config.MoveAreaPaths || config.MoveIterations;
        }

        public async Task Prepare(IMigrationContext context)
        {
            this.context = context;
        }

        public async Task Process(IBatchMigrationContext batchContext)
        {
            int modificationCount = 0;
            await Task.WhenAll(
                Migrator.ReadSourceNodes(context, context.Config.SourceConnection.Project),
                Migrator.ReadTargetNodes(context, context.Config.TargetConnection.Project));

            if (context.Config.MoveAreaPaths)
            {
                modificationCount += await ProcessAreaPaths(batchContext);
            }

            if (context.Config.MoveIterations)
            {
                modificationCount += await ProcessIterationPaths(batchContext);
            }

            if (modificationCount > 0)
            {
                await Migrator.ReadTargetNodes(context, context.Config.TargetConnection.Project);
            }
        }

        public async Task<int> ProcessIterationPaths(IBatchMigrationContext batchContext)
        {
            int modificationCount = 0;
            Logger.LogInformation(LogDestination.All, $"Identified {context.SourceAreaAndIterationTree.IterationPathList.Count} iterations in source project.");

            foreach (var iterationPath in context.SourceAreaAndIterationTree.IterationPathList)
            {
                string iterationPathInTarget = iterationPath.Replace(context.Config.SourceConnection.Project, context.Config.TargetConnection.Project);

                // If the iteration path is not found in the work items we're currently processing then just ignore it.
                if (!batchContext.SourceWorkItems.Any(w => w.Fields.ContainsKey("System.IterationPath") && w.Fields["System.IterationPath"].ToString().ToLower().Equals(iterationPath.ToLower())))
                    continue;

                if (context.TargetAreaAndIterationTree.IterationPathListLookup.ContainsKey(iterationPathInTarget))
                {
                    Logger.LogInformation(LogDestination.All, $"[Exists] {iterationPathInTarget}.");
                }
                else
                {
                    var sourceIterationNode = context.SourceAreaAndIterationTree.IterationPathListLookup[iterationPath];
                    modificationCount += 1;
                    await CreateIterationPath(iterationPath, sourceIterationNode);

                    Logger.LogSuccess(LogDestination.All, $"[Created] {iterationPathInTarget}.");
                }
            }

            Logger.LogInformation(LogDestination.All, $"Iterations synchronized.");
            return modificationCount;
        }

        public async virtual Task CreateIterationPath(string iterationPath, WorkItemClassificationNode sourceIterationNode)
        {
            await WorkItemTrackingHelpers.CreateIterationAsync(
                context.TargetClient.WorkItemTrackingHttpClient,
                context.Config.TargetConnection.Project,
                iterationPath,
                sourceIterationNode.Attributes == null ? null : (DateTime?)sourceIterationNode.Attributes?["startDate"],
                sourceIterationNode.Attributes == null ? null : (DateTime?)sourceIterationNode.Attributes["finishDate"]);
        }

        public async Task<int> ProcessAreaPaths(IBatchMigrationContext batchContext)
        {
            int modificationCount = 0;
            Logger.LogInformation(LogDestination.All, $"Identified {context.SourceAreaAndIterationTree.AreaPathListLookup.Count} area paths in source project.");

            foreach (var areaPath in context.SourceAreaAndIterationTree.AreaPathList)
            {
                string areaPathInTarget = areaPath.Replace(context.Config.SourceConnection.Project, context.Config.TargetConnection.Project);

                // If the area path is not found in the work items we're currently processing then just ignore it.
                if (!batchContext.SourceWorkItems.Any(w => w.Fields.ContainsKey("System.AreaPath") && w.Fields["System.AreaPath"].ToString().ToLower().Equals(areaPath.ToLower())))
                {
                    continue;
                }
                else if (context.TargetAreaAndIterationTree.AreaPathList.Any(a => a.ToLower() == areaPathInTarget.ToLower()))
                {
                    Logger.LogInformation(LogDestination.All, $"[Exists] {areaPathInTarget}.");
                }
                else
                {
                    modificationCount += 1;
                    await CreateAreaPath(areaPathInTarget); 
                    Logger.LogSuccess(LogDestination.All, $"[Created] {areaPathInTarget}.");
                }
            }

            Logger.LogInformation(LogDestination.All, $"Area paths synchronized.");
            return modificationCount;
        }

        public async virtual Task CreateAreaPath(string areaPathInTarget)
        {
            await WorkItemTrackingHelpers.CreateAreaPathAsync(context.TargetClient.WorkItemTrackingHttpClient, context.Config.TargetConnection.Project, areaPathInTarget);
        }
    }
}
