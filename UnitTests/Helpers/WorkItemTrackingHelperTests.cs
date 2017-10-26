using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Helpers
{
    [TestClass]
    public class WorkItemTrackingHelperTests
    {
        [TestMethod]
        public void ParseQueryForPaging_StripsOrderByClause()
        {
            var query = WorkItemTrackingHelpers.ParseQueryForPaging("SELECT * FROM WorkItems order BY System.Id", null);
            Assert.AreEqual("SELECT * FROM WorkItems", query);
        }

        [TestMethod]
        public void ParseQueryForPaging_NoOrderByClause()
        {
            var parsedQuery = WorkItemTrackingHelpers.ParseQueryForPaging("SELECT * FROM WorkItems", null);
            Assert.AreEqual("SELECT * FROM WorkItems", parsedQuery);
        }

        [TestMethod]
        public void GetPageableQuery_ValidWhereClause()
        {
            var query = WorkItemTrackingHelpers.GetPageableQuery("SELECT * FROM WorkItems WHERE System.Id = 1", 1, 1);
            Assert.AreEqual("SELECT * FROM WorkItems WHERE (System.Id = 1) AND ((System.Watermark > 1) OR (System.Watermark = 1 AND System.Id > 1)) ORDER BY System.Watermark, System.Id", query);
        }

        [TestMethod]
        public void GetPageableQuery_NoWhereClause()
        {
            var query = WorkItemTrackingHelpers.GetPageableQuery("SELECT * FROM WorkItems", 1, 1);
            Assert.AreEqual("SELECT * FROM WorkItems WHERE ((System.Watermark > 1) OR (System.Watermark = 1 AND System.Id > 1)) ORDER BY System.Watermark, System.Id", query);
        }

        [TestMethod]
        public void ParseQueryForPaging_InjectsPostMoveTag()
        {
            var query = WorkItemTrackingHelpers.ParseQueryForPaging("SELECT * FROM WorkItems order BY System.Id", "Migrated");
            Assert.AreEqual("SELECT * FROM WorkItems WHERE System.Tags NOT CONTAINS 'Migrated'", query);
        }

        [TestMethod]
        public void ParseQueryForPaging_InjectsPostMoveTagWithWhereClause()
        {
            var query = WorkItemTrackingHelpers.ParseQueryForPaging("SELECT * FROM WorkItems WHERE System.Id = 1 order BY System.Id", "Migrated");
            Assert.AreEqual("SELECT * FROM WorkItems WHERE (System.Id = 1) AND System.Tags NOT CONTAINS 'Migrated'", query);
        }
    }
}
