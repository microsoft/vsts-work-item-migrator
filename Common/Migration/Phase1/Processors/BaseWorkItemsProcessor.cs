using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Common.Config;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;

namespace Common.Migration
{
    public abstract class BaseWorkItemsProcessor : IPhase1Processor
    {
        protected abstract ILogger Logger { get; }

        public abstract string Name { get; }

        public abstract bool IsEnabled(ConfigJson config);

        public abstract IList<WorkItemMigrationState> GetWorkItemsAndStateToMigrate(IMigrationContext context);

        public abstract void PrepareBatchContext(IBatchMigrationContext batchContext, IList<WorkItemMigrationState> workItemsAndStateToMigrate);

        public abstract int GetWorkItemsToProcessCount(IBatchMigrationContext batchContext);

        public abstract BaseWitBatchRequestGenerator GetWitBatchRequestGenerator(IMigrationContext context, IBatchMigrationContext batchContext);

        public async Task ProcessNodes(IMigrationContext context)
        {
            await Migrator.ReadSourceNodes(context, context.Config.SourceConnection.Project);
            await Migrator.ReadTargetNodes(context, context.Config.TargetConnection.Project);

            #region Process area paths ..
            if (context.Config.MoveAreaPaths)
            {
                Logger.LogInformation(LogDestination.All, $"Identified {context.SourceAreaAndIterationTree.AreaPathList.Count} area paths in source project.");

                foreach (var ap in context.SourceAreaAndIterationTree.AreaPathList)
                {
                    string areaPath = ap.Item1.Replace(context.Config.SourceConnection.Project, context.Config.TargetConnection.Project);

                    if (context.TargetAreaAndIterationTree.AreaPathList.Any(p => p.Item1 == areaPath))
                    {
                        Logger.LogInformation(LogDestination.All, $"[Exists] {areaPath}.");
                    }
                    else
                    {
                        await WorkItemTrackingHelpers.CreateAreaPathAsync(context.TargetClient.WorkItemTrackingHttpClient, context.Config.TargetConnection.Project, areaPath);
                        Logger.LogInformation(LogDestination.All, $"[Created] {areaPath}.");
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

                    if (context.TargetAreaAndIterationTree.IterationPathList.Any(i => i.Item1 == iteration))
                    {
                        Logger.LogInformation(LogDestination.All, $"[Exists] {iteration}.");
                    }
                    else
                    {
                        await WorkItemTrackingHelpers.CreateIterationAsync(context.TargetClient.WorkItemTrackingHttpClient, context.Config.TargetConnection.Project, it.Item1.Split("\\").Last(), (DateTime)it.Item2.Attributes["startDate"], (DateTime)it.Item2.Attributes["finishDate"]);
                        Logger.LogInformation(LogDestination.All, $"[Created] {iteration}.");
                    }
                }

                Logger.LogInformation(LogDestination.All, $"Iterations synchronized.");
            }
            #endregion
        }

        public async Task Process(IMigrationContext context)
        {
            await ProcessNodes(context);

            var workItemsAndStateToMigrate = this.GetWorkItemsAndStateToMigrate(context);
            var totalNumberOfBatches = ClientHelpers.GetBatchCount(workItemsAndStateToMigrate.Count, Constants.BatchSize);

            if (!workItemsAndStateToMigrate.Any())
            {
                Logger.LogInformation(LogDestination.File, $"No work items to process for {this.Name}");
                return;
            }

            Logger.LogInformation(LogDestination.All, $"{this.Name} will process {workItemsAndStateToMigrate.Count} work items on the target");
            var preprocessors = ClientHelpers.GetProcessorInstances<IPhase1PreProcessor>(context.Config);
            foreach (var preprocessor in preprocessors)
            {
                await preprocessor.Prepare(context);
            }

            await workItemsAndStateToMigrate.Batch(Constants.BatchSize).ForEachAsync(context.Config.Parallelism, async (batchWorkItemsAndState, batchId) =>
            {
                var batchStopwatch = Stopwatch.StartNew();
                Logger.LogInformation(LogDestination.File, $"{this.Name} batch {batchId} of {totalNumberOfBatches}: Starting");

                IBatchMigrationContext batchContext = new BatchMigrationContext(batchId, batchWorkItemsAndState);
                //read the work items 
                var stepStopwatch = Stopwatch.StartNew();

                Logger.LogTrace(LogDestination.File, $"{this.Name} batch {batchId} of {totalNumberOfBatches}: Reading source work items");
                await Migrator.ReadSourceWorkItems(context, batchWorkItemsAndState.Select(w => w.SourceId), batchContext);
                Logger.LogTrace(LogDestination.File, $"{this.Name} batch {batchId} of {totalNumberOfBatches}: Completed reading source work items in {stepStopwatch.Elapsed.Seconds}s");

                this.PrepareBatchContext(batchContext, batchWorkItemsAndState);

                foreach (var preprocessor in preprocessors)
                {
                    stepStopwatch.Restart();
                    Logger.LogTrace(LogDestination.File, $"{this.Name} batch {batchId} of {totalNumberOfBatches}: Starting {preprocessor.Name}");
                    await preprocessor.Process(batchContext);
                    Logger.LogTrace(LogDestination.File, $"{this.Name} batch {batchId} of {totalNumberOfBatches}: Completed {preprocessor.Name} in {stepStopwatch.Elapsed.Seconds}s");
                }

                var workItemsToUpdateCount = this.GetWorkItemsToProcessCount(batchContext);

                Logger.LogInformation(LogDestination.File, $"{this.Name} batch {batchId} of {totalNumberOfBatches}: Number of work items to migrate: {workItemsToUpdateCount}");

                //migrate the batch of work items
                if (workItemsToUpdateCount == 0)
                {
                    batchStopwatch.Stop();
                    Logger.LogWarning(LogDestination.File, $"{this.Name} batch {batchId} of {totalNumberOfBatches}: No work items to migrate");
                }
                else
                {
                    stepStopwatch.Restart();
                    Logger.LogTrace(LogDestination.File, $"{this.Name} batch {batchId} of {totalNumberOfBatches}: Saving the target work items");
                    var witBatchRequestGenerator = this.GetWitBatchRequestGenerator(context, batchContext);
                    await witBatchRequestGenerator.Write();
                    Logger.LogTrace(LogDestination.File, $"{this.Name} batch {batchId} of {totalNumberOfBatches}: Completed saving the target work items in {stepStopwatch.Elapsed.Seconds}s");

                    batchStopwatch.Stop();
                    Logger.LogInformation(LogDestination.File, $"{this.Name} batch {batchId} of {totalNumberOfBatches}: Completed in {batchStopwatch.Elapsed.TotalSeconds}s");
                }
            });
        }
    }
}
