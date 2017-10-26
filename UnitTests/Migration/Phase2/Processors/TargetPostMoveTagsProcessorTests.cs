using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Common.Config;
using Common.Migration;

namespace UnitTests.Migration.Phase2.Processors
{
    [TestClass]
    public class TargetPostMoveTagsProcessorTests
    {
        private Mock<IMigrationContext> MigrationContextMock;

        [TestInitialize]
        public void Initialize()
        {
            this.MigrationContextMock = new Mock<IMigrationContext>();
        }

        [TestMethod]
        public void GetUpdatedTagsFieldWithPostMove_ReturnsCorrectValue()
        {
            ConfigJson Config = new ConfigJson();
            Config.TargetPostMoveTag = "sample-post-move-tag";
            this.MigrationContextMock.SetupGet(a => a.Config).Returns(Config);
            string tagFieldValue = "originalTag";
            string expected = "originalTag; sample-post-move-tag";

            TargetPostMoveTagsProcessor targetPostMoveTagsProcessor = new TargetPostMoveTagsProcessor();
            string actual = targetPostMoveTagsProcessor.GetUpdatedTagsFieldWithPostMove(this.MigrationContextMock.Object, tagFieldValue);

            Assert.AreEqual(expected, actual);
        }
    }
}
