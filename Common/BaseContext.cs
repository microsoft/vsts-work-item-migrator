using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Common.Config;

namespace Common
{
    public abstract class BaseContext : IContext
    {
        public BaseContext(ConfigJson configJson)
        {
            this.Config = configJson;
            this.SourceClient = ClientHelpers.CreateClient(configJson.SourceConnection);
            this.TargetClient = ClientHelpers.CreateClient(configJson.TargetConnection);
        }

        /// <summary>
        /// Constructor for test purposes
        /// </summary>
        public BaseContext()
        { 
        }

        public ConfigJson Config { get; }

        public WorkItemClientConnection SourceClient { get; }

        public WorkItemClientConnection TargetClient { get; }

        public ConcurrentDictionary<int, string> WorkItemIdsUris { get; set; }

        public ConcurrentBag<WorkItemMigrationState> WorkItemsMigrationState { get; set; } = new ConcurrentBag<WorkItemMigrationState>();

        public ConcurrentDictionary<int, int> SourceToTargetIds { get; set; } = new ConcurrentDictionary<int, int>();

        public ConcurrentSet<string> RemoteLinkRelationTypes { get; set; } = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
