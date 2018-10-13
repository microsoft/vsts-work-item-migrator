using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;
using Common.Migration;


namespace UnitTests.Common
{
    [TestClass]
    public class ClientHelperTests
    {
        [TestMethod]
        public void GetSourceWorkItemApiEndpoint_AccountEndingInSlashReturnsCorrectValue()
        {
            string account = "accountEndingInSlash/";
            int workItemId = 777;

            string expected = "accountEndingInSlash/_apis/wit/workItems/777";

            string actual = ClientHelpers.GetWorkItemApiEndpoint(account, workItemId);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetSourceWorkItemApiEndpoint_AccountWithoutSlashReturnsCorrectValue()
        {
            string account = "accountWithoutSlash";
            int workItemId = 777;

            string expected = "accountWithoutSlash/_apis/wit/workItems/777";

            string actual = ClientHelpers.GetWorkItemApiEndpoint(account, workItemId);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetWorkItemIdFromApiEndpoint_ReturnsCorrectResultWhenEndpointContainsIdAtEnd()
        {
            string endpointUri = "https://dev.azure.com/account/_apis/wit/workItems/3543";

            int expected = 3543;

            int actual = ClientHelpers.GetWorkItemIdFromApiEndpoint(endpointUri);

            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void GetWorkItemIdFromApiEndpoint_ReturnsCorrectResultWhenEndpointContainsIdFollowedBySlashAtEnd()
        {
            string endpointUri = "https://dev.azure.com/account/_apis/wit/workItems/3543/";

            int expected = 3543;

            int actual = ClientHelpers.GetWorkItemIdFromApiEndpoint(endpointUri);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetWorkItemIdFromApiEndpoint_ReturnsCorrectResultWhenEndpointContainsQueryString()
        {
            string endpointUri = "https://dev.azure.com/account/_apis/wit/workItems/3543?bypassRules=True&suppressNotifications=True&api-version=4.0";

            int expected = 3543;

            int actual = ClientHelpers.GetWorkItemIdFromApiEndpoint(endpointUri);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetNotMigratedWorkItemsFromWorkItemsMigrationState_ReturnsCorrectResult()
        {
            WorkItemMigrationState notMigratedState = new WorkItemMigrationState();
            notMigratedState.SourceId = 1;
            notMigratedState.FailureReason |= FailureReason.UnsupportedWorkItemType;

            WorkItemMigrationState migratedState = new WorkItemMigrationState();
            migratedState.SourceId = 2;

            ConcurrentBag<WorkItemMigrationState> workItemsMigrationState = new ConcurrentBag<WorkItemMigrationState>();
            workItemsMigrationState.Add(notMigratedState);
            workItemsMigrationState.Add(migratedState);

            Dictionary<int, FailureReason> result = ClientHelpers.GetNotMigratedWorkItemsFromWorkItemsMigrationState(workItemsMigrationState);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result.First().Key);
        }
    }
}
