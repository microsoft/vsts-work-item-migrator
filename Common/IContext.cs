using System.Collections.Concurrent;
using Common.Config;

namespace Common
{
    public interface IContext
    {
        ConfigJson Config { get; }

        WorkItemClientConnection SourceClient { get; }

        WorkItemClientConnection TargetClient { get; }

        //Mapping of source work items to their url on the source
        ConcurrentDictionary<int, string> WorkItemIdsUris { get; set; }

        //State of all work items to migrate
        ConcurrentBag<WorkItemMigrationState> WorkItemsMigrationState { get; set; }

        //remote relation types, do not need to exist on target since they're 
        //recreated as hyperlinks
        ConcurrentSet<string> RemoteLinkRelationTypes { get; set; }
    }
}
