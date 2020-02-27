using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Common.Config;
using Logging;

namespace Common.Migration
{
    public class ClassificationNodesPreProcessor : IPhase1PreProcessor
    {
        static ILogger Logger { get; } = MigratorLogging.CreateLogger<ClassificationNodesPreProcessor>();
        private IMigrationContext context;

        public string Name => "Classification Nodes (Area Paths/Iterations)";

        public bool IsEnabled(ConfigJson config)
        {
            return true;
        }

        public async Task Prepare(IMigrationContext context)
        {
            this.context = context;
        }
        public async Task Process(IBatchMigrationContext batchContext)
        {
            int modificationCount = 0;
            if (context.Config.MoveAreaPaths || context.Config.MoveIterations)
            {
                await Migrator.ReadSourceNodes(context, context.Config.SourceConnection.Project);
                await Migrator.ReadTargetNodes(context, context.Config.TargetConnection.Project);
            }

            #region Process area paths ..
            if (context.Config.MoveAreaPaths)
            {
                Logger.LogInformation(LogDestination.All, $"Identified {context.SourceAreaAndIterationTree.AreaPathList.Count} area paths in source project.");

                foreach (var ap in context.SourceAreaAndIterationTree.AreaPathList)
                {
                    string areaPath = ap.Item1.Replace(context.Config.SourceConnection.Project, context.Config.TargetConnection.Project);

                    // If the area path is not found in the work items we're currently processing then just ignore it.
                    if (!batchContext.SourceWorkItems.Any(w => w.Fields.ContainsKey("System.AreaPath") && w.Fields["System.AreaPath"].ToString().ToLower().EndsWith(ap.Item1.ToLower())))
                        continue;

                    if (context.TargetAreaAndIterationTree.AreaPathList.Any(p => p.Item1 == areaPath))
                    {
                        Logger.LogInformation(LogDestination.All, $"[Exists] {areaPath}.");
                    }
                    else
                    {
                        modificationCount += 1;
                        await WorkItemTrackingHelpers.CreateAreaPathAsync(context.TargetClient.WorkItemTrackingHttpClient, context.Config.TargetConnection.Project, areaPath);
                        Logger.LogSuccess(LogDestination.All, $"[Created] {areaPath}.");
                    }
                }

                Logger.LogInformation(LogDestination.All, $"Area paths synchronized.");
            }
            #endregion

            #region Process iterations ..
            if (context.Config.MoveIterations)
            {
                Logger.LogInformation(LogDestination.All, $"Identified {context.SourceAreaAndIterationTree.AreaPathList.Count} iterations in source project.");

                foreach (var it in context.SourceAreaAndIterationTree.IterationPathList)
                {
                    string iteration = it.Item1.Replace(context.Config.SourceConnection.Project, context.Config.TargetConnection.Project);

                    // If the iteration path is not found in the work items we're currently processing then just ignore it.
                    if (!batchContext.SourceWorkItems.Any(w => w.Fields.ContainsKey("System.IterationPath") && w.Fields["System.IterationPath"].ToString().ToLower().EndsWith(it.Item1.ToLower())))
                        continue;

                    if (context.TargetAreaAndIterationTree.IterationPathList.Any(i => i.Item1 == iteration))
                    {
                        Logger.LogInformation(LogDestination.All, $"[Exists] {iteration}.");
                    }
                    else
                    {
                        modificationCount += 1;
                        await WorkItemTrackingHelpers.CreateIterationAsync(
                            context.TargetClient.WorkItemTrackingHttpClient, 
                            context.Config.TargetConnection.Project, 
                            it.Item1.Split("\\").Last(), it.Item2.Attributes == null ? null : (DateTime?)it.Item2.Attributes?["startDate"], 
                            it.Item2.Attributes == null ? null : (DateTime?)it.Item2.Attributes["finishDate"]);

                        Logger.LogSuccess(LogDestination.All, $"[Created] {iteration}.");
                    }
                }

                Logger.LogInformation(LogDestination.All, $"Iterations synchronized.");
            }
            #endregion

            if (modificationCount > 0)
            {
                await Migrator.ReadTargetNodes(context, context.Config.TargetConnection.Project);
            }
        }
        
    }
}
