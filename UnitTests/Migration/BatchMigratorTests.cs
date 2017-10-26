using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Common.Migration;

namespace UnitTests.Migration
{
    [TestClass]
    public class BatchMigratorTests
    {
        private Mock<BaseWitBatchRequestGenerator> BatchMigratorMock;
        private Mock<IMigrationContext> MigrationContextMock;
        
        [TestInitialize]
        public void Initialize()
        {
            ISet<string> TargetAreaPathList = new HashSet<string>() {
                @"MyFirstProject",
                @"MyFirstProject\My new area aaaaaaaa-0000-aaaa-0000-aaaaaaaaaa (renamed)",
                @"MyFirstProject\Anitha Area1",
                @"MyFirstProject\Anitha Area 2\Anitha Area2 child",
                @"MyFirstProject\Anitha Area 2\Anitha Area2 child\a\aa\aaa\b"
            };

            ISet<string> TargetIterationPathList = new HashSet<string>() {
                @"MyFirstProject",
                @"MyFirstProject\Iteration 1",
                @"MyFirstProject\Iteration 1\Iteration 11",
                @"MyFirstProject\test iteration\test iteration 1"
            };

            ConcurrentDictionary<string, ISet<string>> TargetWorkItemTypes = new ConcurrentDictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase);

            ISet<string> bugList = new HashSet<string>();

            bugList.Add("itemA");
            bugList.Add("itemB");
            bugList.Add("itemC");

            TargetWorkItemTypes.TryAdd("Bug", bugList);

            ISet<string> epicList = new HashSet<string>();

            epicList.Add("itemA");
            epicList.Add("itemC");

            TargetWorkItemTypes.TryAdd("Epic", epicList);

            IList<string> UnsupportedFields = new List<string>() {
                "System.BoardColumn",
                "System.BoardColumnDone",
                "Kanban.Column",
                "Kanban.Column.Done"
            };

            IList<string> FieldsThatRequireSourceProjectToBeReplacedWithTargetProject = new List<string>() {
                "System.AreaPath",
                "System.IterationPath",
                "System.TeamProject"
            };

            this.MigrationContextMock = new Mock<IMigrationContext>();
            this.MigrationContextMock.SetupGet(a => a.TargetAreaPaths).Returns(TargetAreaPathList);
            this.MigrationContextMock.SetupGet(a => a.TargetIterationPaths).Returns(TargetIterationPathList);
            this.MigrationContextMock.SetupGet(a => a.WorkItemTypes).Returns(TargetWorkItemTypes);
            this.MigrationContextMock.SetupGet(a => a.UnsupportedFields).Returns(UnsupportedFields);
            this.MigrationContextMock.SetupGet(a => a.FieldsThatRequireSourceProjectToBeReplacedWithTargetProject).Returns(FieldsThatRequireSourceProjectToBeReplacedWithTargetProject);

            Mock<IBatchMigrationContext> BatchContextMock = new Mock<IBatchMigrationContext>();
            this.BatchMigratorMock = new Mock<BaseWitBatchRequestGenerator>(this.MigrationContextMock.Object, BatchContextMock.Object);
        }

        [TestMethod]
        public void ExistsInTargetAreaPathList_ReturnsTrueWhenFieldNameIsInListIgnoringCase()
        {
            string fieldName1 = @"MyFirstProject\My new area aaaaaaaa-0000-aaaa-0000-aaaaaaaaaa (renamed)";
            string fieldName2 = @"MYFIRSTPROJECT";
            string fieldName3 = @"MYFIRSTPROJECT\Anitha Area 2\Anitha Area2 child\a\aa\aaa\b";

            bool result1 = this.BatchMigratorMock.Object.ExistsInTargetAreaPathList(fieldName1);
            bool result2 = this.BatchMigratorMock.Object.ExistsInTargetAreaPathList(fieldName2);
            bool result3 = this.BatchMigratorMock.Object.ExistsInTargetAreaPathList(fieldName3);

            Assert.IsTrue(result1);
            Assert.IsTrue(result2);
            Assert.IsTrue(result3);
        }

        [TestMethod]
        public void ExistsInTargetAreaPathList_ReturnsFalseWhenFieldNameIsNotInList()
        {
            string fieldName = "DoesNotExist";
            bool result = this.BatchMigratorMock.Object.ExistsInTargetAreaPathList(fieldName);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ExistsInTargetIterationPathList_ReturnsTrueWhenFieldNameIsInListIgnoringCase()
        {
            string fieldName1 = @"MyFirstProject\Iteration 1\Iteration 11";
            string fieldName2 = @"MYFIRSTPROJECT";

            bool result1 = this.BatchMigratorMock.Object.ExistsInTargetIterationPathList(fieldName1);
            bool result2 = this.BatchMigratorMock.Object.ExistsInTargetIterationPathList(fieldName2);

            Assert.IsTrue(result1);
            Assert.IsTrue(result2);
        }

        [TestMethod]
        public void ExistsInTargetIterationPathList_ReturnsFalseWhenFieldNameIsNotInList()
        {
            string fieldName = "DoesNotExist";
            bool result = this.BatchMigratorMock.Object.ExistsInTargetIterationPathList(fieldName);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void FieldRequiresProjectNameUpdate_ReturnsTrueWhenFieldNameIsInListIgnoringCase()
        {
            string fieldName = "SYSTEM.AreaPath";
            bool result = this.BatchMigratorMock.Object.FieldRequiresProjectNameUpdate(fieldName);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void FieldRequiresProjectNameUpdate_ReturnsFalseWhenFieldNameIsNotInList()
        {
            string fieldName = "DoesNotExist";
            bool result = this.BatchMigratorMock.Object.FieldRequiresProjectNameUpdate(fieldName);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsFieldUnsupported_ReturnsTrueWhenFieldRefNameIsInUnsupportedFields()
        {
            string fieldRefName = "System.BoardColumn";
            bool result = this.BatchMigratorMock.Object.IsFieldUnsupported(fieldRefName);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsFieldUnsupported_ReturnsTrueWhenFieldRefNameContainsAStringFromUnsupportedFields()
        {
            string fieldRefName = "EXCESS_TEXT_System.BoardColumn_EXCESS_TEXT";
            bool result = this.BatchMigratorMock.Object.IsFieldUnsupported(fieldRefName);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsFieldUnsupported_ReturnsTrueWhenFieldRefNameIsInUnsupportedFieldsIgnoringCase()
        {
            string fieldRefName = "SYSTEM.boardcolumn";
            bool result = this.BatchMigratorMock.Object.IsFieldUnsupported(fieldRefName);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsFieldUnsupported_ReturnsFalseWhenFieldRefNameIsNotInUnsupportedFields()
        {
            string fieldRefName = "DoesNotExist";
            bool result = this.BatchMigratorMock.Object.IsFieldUnsupported(fieldRefName);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void FieldIsWithinType_ReturnsFalseWhenFieldIsNotWithinTypeButIsWithinAnotherType()
        {
            string fieldName = "itemB";
            string workItemType = "Epic";

            bool result = this.BatchMigratorMock.Object.FieldIsWithinType(fieldName, workItemType);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void FieldIsWithinType_ReturnsFalseWhenFieldIsNotWithinAnyType()
        {
            string fieldName = "itemDoesNotExist";
            string workItemType = "Epic";

            bool result = this.BatchMigratorMock.Object.FieldIsWithinType(fieldName, workItemType);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void FieldIsWithinType_ReturnsTrueWhenFieldIsWithinType()
        {
            string fieldName = "itemA";
            string workItemType = "Bug";

            bool result = this.BatchMigratorMock.Object.FieldIsWithinType(fieldName, workItemType);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void FieldIsWithinType_ReturnsTrueWhenFieldIsWithinTypeIgnoringCase()
        {
            string fieldName = "ITEMA";
            string workItemType = "Bug";

            bool result = this.BatchMigratorMock.Object.FieldIsWithinType(fieldName, workItemType);

            Assert.IsTrue(result);
        }
    }
}
