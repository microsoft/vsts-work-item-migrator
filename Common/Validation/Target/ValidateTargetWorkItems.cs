using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Common.Migration;
using Logging;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;

namespace Common.Validation
{
    //Purpose of this class is to read the work items in the target account that have been migrated before. Check their rev number in the comments field to
    //to see if any of them have been updated in the source. Also, we use this method to read the existing relations and save them for further processing. 
    //For example, we read all git commit links in the target work item and save it. We use it later when we need to move git commit links for target work items
    [RunOrder(7)]
    public class ValidateTargetWorkItems : ITargetValidator
    {
        private ILogger Logger { get; } = MigratorLogging.CreateLogger<ValidateTargetWorkItems>();
        private IValidationContext ValidationContext { get; set; }
        private ConcurrentSet<int> sourceWorkItemIdsThatHaveBeenUpdated = new ConcurrentSet<int>();

        public string Name => "Validate target work item";

        public async Task Validate(IValidationContext validationContext)
        {
            this.ValidationContext = validationContext;
           
            if (!validationContext.Config.SkipExisting)
            {
                var stopwatch = Stopwatch.StartNew();
                Logger.LogInformation(LogDestination.File, "Started querying the target account to determine if the previously migrated work items have been updated on the source");

                await PopulateWorkItemMigrationState();

                stopwatch.Stop();
                Logger.LogInformation(LogDestination.File, $"Completed querying the target account in {stopwatch.Elapsed.TotalSeconds}s, {this.ValidationContext.WorkItemsMigrationState.Count(w => w.MigrationState == WorkItemMigrationState.State.Existing && w.Requirement.HasFlag(WorkItemMigrationState.RequirementForExisting.UpdatePhase1))} work item(s) have been updated in the source");
            }
        }

        private async Task PopulateWorkItemMigrationState()
        {
            //dictionary of target workitem id to source id - these workitems have been migrated before
            var existingWorkItems = ValidationContext.WorkItemsMigrationState.Where(wi => wi.MigrationState == WorkItemMigrationState.State.Existing);
            var totalNumberOfBatches = ClientHelpers.GetBatchCount(existingWorkItems.Count(), Constants.BatchSize);

            await existingWorkItems.Batch(Constants.BatchSize).ForEachAsync(ValidationContext.Config.Parallelism, async (batchWorkItemMigrationState, batchId) =>
            {
                var stopwatch = Stopwatch.StartNew();
                Logger.LogInformation(LogDestination.File, $"{Name} batch {batchId} of {totalNumberOfBatches}: Started");

                Dictionary<int, WorkItemMigrationState> targetToWorkItemMigrationState = batchWorkItemMigrationState.ToDictionary(k => k.TargetId.Value, v => v);

                //read the target work items 
                IList<WorkItem> targetWorkItems = await WorkItemTrackingHelpers.GetWorkItemsAsync(ValidationContext.TargetClient.WorkItemTrackingHttpClient, batchWorkItemMigrationState.Select(a => a.TargetId.Value).ToList(), expand: WorkItemExpand.Relations);

                IDictionary<int, WorkItemRelation> targetIdToHyperlinkToSourceRelationMapping = GetTargetIdToHyperlinkToSourceRelationMapping(targetWorkItems, targetToWorkItemMigrationState);

                ProcessUpdatedSourceWorkItems(targetWorkItems, targetToWorkItemMigrationState, targetIdToHyperlinkToSourceRelationMapping);

                StoreWorkItemBatchRelationInformationOnContext(targetWorkItems, targetToWorkItemMigrationState, targetIdToHyperlinkToSourceRelationMapping);

                stopwatch.Stop();
                Logger.LogInformation(LogDestination.File, $"{Name} batch {batchId} of {totalNumberOfBatches}: Completed in {stopwatch.Elapsed.TotalSeconds}s");
            });
        }

        private IDictionary<int, WorkItemRelation> GetTargetIdToHyperlinkToSourceRelationMapping(IList<WorkItem> targetWorkItems, IDictionary<int, WorkItemMigrationState> targetToWorkItemMigrationState)
        {
            Dictionary<int, WorkItemRelation> result = new Dictionary<int, WorkItemRelation>();

            foreach (WorkItem targetWorkItem in targetWorkItems)
            {
                if (targetWorkItem.Relations == null)
                {
                    throw new ValidationException($"Target work item with id: {targetWorkItem.Id} does not have any relations.");
                }

                foreach (WorkItemRelation relation in targetWorkItem.Relations)
                {
                    //check for hyperlink to the source - used for incremental updates
                    int sourceId = targetToWorkItemMigrationState[targetWorkItem.Id.Value].SourceId;
                    int targetId = targetWorkItem.Id.Value;
                    if (RelationHelpers.IsRelationHyperlinkToSourceWorkItem(ValidationContext, relation, sourceId))
                    {
                        result.Add(targetId, relation);
                        break;
                    }
                }
            }

            return result;
        }

        private void StoreWorkItemBatchRelationInformationOnContext(IList<WorkItem> targetWorkItems, Dictionary<int, WorkItemMigrationState> targetToWorkItemMigrationState, IDictionary<int, WorkItemRelation> targetIdToHyperlinkToSourceRelationMapping)
        {
            foreach (var targetWorkItem in targetWorkItems)
            {
                if (WorkItemHasBeenUpdatedOnSource(targetWorkItem, targetToWorkItemMigrationState[targetWorkItem.Id.Value]))
                {
                    int sourceId = targetToWorkItemMigrationState[targetWorkItem.Id.Value].SourceId;
                    StoreWorkItemRelationInformationOnContext(sourceId, targetWorkItem, targetIdToHyperlinkToSourceRelationMapping[targetWorkItem.Id.Value]);
                }
            }
        }

        private void StoreWorkItemRelationInformationOnContext(int sourceId, WorkItem workItem, WorkItemRelation hyperlinkToSourceRelation)
        {
            if (hyperlinkToSourceRelation.Attributes != null && hyperlinkToSourceRelation.Attributes.ContainsKeyIgnoringCase(Constants.RelationAttributeId))
            {
                // get the key even if its letter case is different but it matches otherwise
                string keyFromFields = hyperlinkToSourceRelation.Attributes.GetKeyIgnoringCase(Constants.RelationAttributeId);
                var id = (Int64)hyperlinkToSourceRelation.Attributes[keyFromFields];
                ValidationContext.TargetIdToSourceHyperlinkAttributeId.TryAdd(workItem.Id.Value, id);
            }
            else
            {
                throw new ValidationException($"Attribute does not have a Id for {workItem.Id} in the target. Relation to check is {hyperlinkToSourceRelation.Url}");
            }
        }

        /// <summary>
        /// Store the source WorkItems that have been updated in this.sourceWorkItemIdsThatHaveBeenUpdated
        /// </summary>
        /// <param name="targetWorkItems"></param>
        /// <param name="targetToWorkItemMigrationState"></param>
        private void ProcessUpdatedSourceWorkItems(IList<WorkItem> targetWorkItems, Dictionary<int, WorkItemMigrationState> targetToWorkItemMigrationState, IDictionary<int, WorkItemRelation> targetIdToHyperlinkToSourceRelationMapping)
        {
            foreach (WorkItem targetWorkItem in targetWorkItems)
            {
                var workItemMigrationState = targetToWorkItemMigrationState[targetWorkItem.Id.Value];
                if (targetWorkItem.Relations == null)
                {
                    Logger.LogError(LogDestination.File, $"Target work item with Id: {targetWorkItem.Id} has no relations. Migration will be skipped for it.");
                    workItemMigrationState.MigrationState = WorkItemMigrationState.State.Error;
                    return;
                }

                int targetWorkItemId = targetWorkItem.Id.Value;

                ProcessUpdatedSourceWorkItem(targetWorkItem, workItemMigrationState, targetIdToHyperlinkToSourceRelationMapping[targetWorkItemId]);
                StoreTargetRelationPhaseStatusHyperlinkDataFromWorkItem(workItemMigrationState, targetIdToHyperlinkToSourceRelationMapping[targetWorkItemId]);
            }
        }

        private void ProcessUpdatedSourceWorkItem(WorkItem targetWorkItem, WorkItemMigrationState workItemMigrationState, WorkItemRelation hyperlinkToSourceRelation)
        {
            //get the source rev from the revision dictionary - populated by PostValidateWorkitems
            int sourceId = workItemMigrationState.SourceId;
            int sourceRev = ValidationContext.SourceWorkItemRevision[sourceId];
            string sourceUrl = ValidationContext.WorkItemIdsUris[sourceId];
            int targetRev = GetRev(this.ValidationContext, targetWorkItem, sourceId, hyperlinkToSourceRelation);

            if (IsDifferenceInRevNumbers(sourceId, targetWorkItem, hyperlinkToSourceRelation, targetRev))
            {
                Logger.LogInformation(LogDestination.File, $"Source workItem {sourceId} Rev {sourceRev} Target workitem {targetWorkItem.Id} Rev {targetRev}");
                this.sourceWorkItemIdsThatHaveBeenUpdated.Add(sourceId);
                workItemMigrationState.Requirement |= WorkItemMigrationState.RequirementForExisting.UpdatePhase1;
                workItemMigrationState.Requirement |= WorkItemMigrationState.RequirementForExisting.UpdatePhase2;
            }
            else if (IsPhase2UpdateRequired(workItemMigrationState, targetWorkItem))
            {
                workItemMigrationState.Requirement |= WorkItemMigrationState.RequirementForExisting.UpdatePhase2;
            }
            else
            {
                workItemMigrationState.Requirement |= WorkItemMigrationState.RequirementForExisting.None;
            }
        }

        private bool IsPhase2UpdateRequired(WorkItemMigrationState workItemMigrationState, WorkItem targetWorkItem)
        {
            IEnumerable<IPhase2Processor> phase2Processors = ClientHelpers.GetProcessorInstances<IPhase2Processor>(ValidationContext.Config);
            workItemMigrationState.RevAndPhaseStatus = GetRevAndPhaseStatus(targetWorkItem, workItemMigrationState.SourceId);

            // find out if Enabled, see if matches comment from target
            ISet<string> enabledPhaseStatuses = System.Linq.Enumerable.ToHashSet(phase2Processors.Where(a => a.IsEnabled(ValidationContext.Config)).Select(b => b.Name));
            enabledPhaseStatuses.Remove(Constants.RelationPhaseClearAllRelations);

            if (enabledPhaseStatuses.IsSubsetOf(workItemMigrationState.RevAndPhaseStatus.PhaseStatus)) // enabled relation phases are already complete for current work item
            {
                return false;
            }

            return true;
        }

        private RevAndPhaseStatus GetRevAndPhaseStatus(WorkItem targetWorkItem, int sourceWorkItemId)
        {
            if (targetWorkItem.Relations != null)
            {
                foreach (WorkItemRelation relation in targetWorkItem.Relations)
                {
                    if (RelationHelpers.IsRelationHyperlinkToSourceWorkItem(ValidationContext, relation, sourceWorkItemId))
                    {
                        // get the key even if its letter case is different but it matches otherwise
                        string keyFromFields = relation.Attributes.GetKeyIgnoringCase(Constants.RelationAttributeComment);
                        string relationRevAndPhaseStatusComment = relation.Attributes[keyFromFields].ToString();

                        RevAndPhaseStatus revAndPhaseStatus = new RevAndPhaseStatus(relationRevAndPhaseStatusComment);
                        return revAndPhaseStatus;
                    }
                }
            }

            throw new Exception($"Could not find comment in relation hyperlink to source work item on target work item with id: {targetWorkItem.Id.Value}. Expected source work item id: {sourceWorkItemId}");
        }

        private void StoreTargetRelationPhaseStatusHyperlinkDataFromWorkItem(WorkItemMigrationState workItemMigrationState, WorkItemRelation hyperlinkToSourceRelation)
        {
            var targetAttributes = hyperlinkToSourceRelation.Attributes;
            targetAttributes.TryGetValue(Constants.RelationAttributeComment, out string targetRelationPhaseStatus);

            workItemMigrationState.RevAndPhaseStatus = new RevAndPhaseStatus(targetRelationPhaseStatus);
        }

        private bool WorkItemHasBeenUpdatedOnSource(WorkItem targetWorkItem, WorkItemMigrationState workItemMigrationState)
        {
            int correspondingSource = workItemMigrationState.SourceId;
            return this.sourceWorkItemIdsThatHaveBeenUpdated.Contains(correspondingSource);
        }

        private bool IsDifferenceInRevNumbers(int sourceWorkItemId, WorkItem targetWorkItem, WorkItemRelation hyperlinkToSourceRelation, int targetRev)
        {
            int sourceRev = ValidationContext.SourceWorkItemRevision[sourceWorkItemId];
            int targetWorkItemId = targetWorkItem.Id.Value;

            return sourceRev != targetRev;

        }

        private int GetRev(IValidationContext context, WorkItem targetWorkItem, int sourceWorkItemId, WorkItemRelation hyperlinkToSourceRelation)
        {
            var targetAttributes = hyperlinkToSourceRelation.Attributes;
            targetAttributes.TryGetValue(Constants.RelationAttributeComment, out string targetRelationPhaseStatus);

            string revNumberString = targetRelationPhaseStatus.SplitBySemicolonToHashSet().First();
            return Convert.ToInt32(revNumberString);
        }
    }
}
