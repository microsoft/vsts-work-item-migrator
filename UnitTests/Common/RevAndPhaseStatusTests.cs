using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;

namespace UnitTests.Common
{
    [TestClass]
    public class RevAndPhaseStatusTests
    {
        [TestMethod]
        public void SetRevAndPhaseStatus_ReturnsCorrectValueForRegularCase()
        {
            string revAndPhaseStatusComment = "123;attachments;git commit links";

            ISet<string> expectedPhaseStatus = new HashSet<string>();
            expectedPhaseStatus.Add("attachments");
            expectedPhaseStatus.Add("git commit links");

            RevAndPhaseStatus expected = new RevAndPhaseStatus();
            expected.Rev = 123;
            expected.PhaseStatus = expectedPhaseStatus;

            RevAndPhaseStatus actual = new RevAndPhaseStatus();
            actual.SetRevAndPhaseStatus(revAndPhaseStatusComment);

            Assert.AreEqual(expected.Rev, actual.Rev);

            foreach (string item in expected.PhaseStatus)
            {
                Assert.IsTrue(actual.PhaseStatus.Contains(item));
            }
        }

        [TestMethod]
        public void SetRevAndPhaseStatus_ReturnsCorrectValueWhenOnlyRev()
        {
            string revAndPhaseStatusComment = "123";

            RevAndPhaseStatus expected = new RevAndPhaseStatus();
            expected.Rev = 123;
            expected.PhaseStatus = new HashSet<string>();

            RevAndPhaseStatus actual = new RevAndPhaseStatus();
            actual.SetRevAndPhaseStatus(revAndPhaseStatusComment);

            Assert.AreEqual(expected.Rev, actual.Rev);
            Assert.AreEqual(0, actual.PhaseStatus.Count);
        }

        [TestMethod]
        public void GetCommentRepresentation_ReturnsCorrectValue()
        {
            RevAndPhaseStatus revAndPhaseStatus = new RevAndPhaseStatus();
            revAndPhaseStatus.Rev = 123;

            ISet<string> phaseStatus = new HashSet<string>();
            phaseStatus.Add("attachments");
            phaseStatus.Add("git commit links");

            revAndPhaseStatus.PhaseStatus = phaseStatus;

            string expected = "123;attachments;git commit links";
            string actual = revAndPhaseStatus.GetCommentRepresentation();

            Assert.AreEqual(expected, actual);
        }
    }
}
