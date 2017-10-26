using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common.Migration;

namespace UnitTests.Migration.Preprocess
{
    [TestClass]
    public class PreprocessGitCommitLinksTests
    {
        [TestMethod]
        public void ConvertGitCommitLinkToHyperLink_NullTest()
        {
            string account = "https://mseng.visualstudio.com/";
            var actualHyperlink = GitCommitLinksProcessor.ConvertGitCommitLinkToHyperLink(1, null, account);
            Assert.AreEqual(null, actualHyperlink);
        }

        [TestMethod]
        public void ConvertGitCommitLinkToHyperLink_EmptyTest()
        {
            string account = "https://mseng.visualstudio.com/";
            var actualHyperlink = GitCommitLinksProcessor.ConvertGitCommitLinkToHyperLink(1, "", account);
            Assert.AreEqual(null, actualHyperlink);
        }

        [TestMethod]
        public void ConvertGitCommitLinkToHyperLink_CorrectArtifactLink()
        {
            var artifactLink = "vstfs:///Git/Commit/b924d696-3eae-4116-8443-9a18392d8544%2ffb240610-b309-4925-8502-65ff76312c40%2fb8b676f5ec7d5b88df15258bec81c8a2ded4a05a";
            string account = "https://mseng.visualstudio.com/";
            var actualHyperlink = GitCommitLinksProcessor.ConvertGitCommitLinkToHyperLink(1, artifactLink, account);
            string expectedHyperlink = "https://mseng.visualstudio.com/b924d696-3eae-4116-8443-9a18392d8544/_git/fb240610-b309-4925-8502-65ff76312c40/commit/b8b676f5ec7d5b88df15258bec81c8a2ded4a05a";
            Assert.AreEqual(expectedHyperlink, actualHyperlink);
        }

        [TestMethod]
        public void ConvertGitCommitLinkToHyperLink_BadArtifactLinkWithTwoGuids()
        {
            string account = "https://mseng.visualstudio.com/";
            var artifactLink = "vstfs:///Git/Commit/b924d696-3eae-4116-8443-9a18392d8544%2ffb240610-b309-4925-8502-65ff76312c40";
            var actualHyperlink = GitCommitLinksProcessor.ConvertGitCommitLinkToHyperLink(1, artifactLink, account);
            Assert.AreEqual(null, actualHyperlink);
        }
        
        [TestMethod]
        public void ConvertGitCommitLinkToHyperLink_BadArtifactLink()
        {
            string account = "https://mseng.visualstudio.com/";
            //3rd separator is a %2e instead of %2f which stands for /
            var artifactLink = "vstfs:///Git/Commit/b924d696-3eae-4116-8443-9a18392d8544%2ffb240610-b309-4925-8502-65ff76312c40%2eb8b676f5ec7d5b88df15258bec81c8a2ded4a05a";
            var actualHyperlink = GitCommitLinksProcessor.ConvertGitCommitLinkToHyperLink(1, artifactLink, account);
            Assert.AreEqual(null, actualHyperlink);
        }

    }
}
