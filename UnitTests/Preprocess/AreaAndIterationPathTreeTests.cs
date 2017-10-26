using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;

namespace UnitTests.Preprocess
{
    [TestClass]
    public class AreaAndIterationPathTreeTests
    {
        [TestMethod]
        public void AreaAndIterationPathTree_NullTest()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                AreaAndIterationPathTree tree = new AreaAndIterationPathTree(null);
            });
        }

        [TestMethod]
        public void AreaAndIterationPathTree_RootNodesOnlyTest()
        {
            IList<WorkItemClassificationNode> nodeList = new List<WorkItemClassificationNode>
            {
                new WorkItemClassificationNode(){Name = "DefaultArea", StructureType = TreeNodeStructureType.Area},
                new WorkItemClassificationNode(){Name = "DefaultIteration", StructureType = TreeNodeStructureType.Iteration}
            };

            ISet<string> expectedAreaPathList = new HashSet<string> { "DefaultArea" };
            ISet<string> expectedIterationPathList = new HashSet<string> { "DefaultIteration" };

            AreaAndIterationPathTree tree = new AreaAndIterationPathTree(nodeList);
            Assert.IsTrue(expectedAreaPathList.SetEquals(tree.AreaPathList));
            Assert.IsTrue(expectedIterationPathList.SetEquals(tree.IterationPathList));
        }

        [TestMethod]
        public void AreaAndIterationPathTree_AreaPathWithChildrenTest()
        {
            IList<WorkItemClassificationNode> nodeList = new List<WorkItemClassificationNode>
            {
                new WorkItemClassificationNode()
                {
                    Name = "1",
                    StructureType = TreeNodeStructureType.Area,
                    Children = new List<WorkItemClassificationNode>()
                    {
                                new WorkItemClassificationNode(){Name = "11"},
                                new WorkItemClassificationNode(){Name = "12"},
                                new WorkItemClassificationNode(){Name = "13"}
                    } },
                new WorkItemClassificationNode(){
                    Name = "DefaultIteration",
                    StructureType = TreeNodeStructureType.Iteration}
                };

            ISet<string> expectedAreaPathList = new HashSet<string> { "1", "1\\11", "1\\12", "1\\13" };
            ISet<string> expectedIterationPathList = new HashSet<string> { "DefaultIteration" };
            AreaAndIterationPathTree tree = new AreaAndIterationPathTree(nodeList);
            Assert.IsTrue(expectedAreaPathList.SetEquals(tree.AreaPathList));
            Assert.IsTrue(expectedIterationPathList.SetEquals(tree.IterationPathList));
        }

        [TestMethod]
        public void AreaAndIterationPathTree_IterationPathWithChildrenTest()
        {
            IList<WorkItemClassificationNode> nodeList = new List<WorkItemClassificationNode>
            {
                new WorkItemClassificationNode()
                {
                    Name = "A",
                    StructureType = TreeNodeStructureType.Area,
                    Children = new List<WorkItemClassificationNode>()
                    {
                                new WorkItemClassificationNode(){Name = "11"},
                                new WorkItemClassificationNode(){Name = "12"},
                                new WorkItemClassificationNode(){Name = "13"}
                    }
                },
                new WorkItemClassificationNode()
                {
                    Name = "I",
                    StructureType = TreeNodeStructureType.Iteration,
                    Children = new List<WorkItemClassificationNode>()
                    {
                                new WorkItemClassificationNode(){Name = "11"},
                                new WorkItemClassificationNode(){Name = "12"},
                                new WorkItemClassificationNode(){Name = "13"}
                    }
                }
            };

            ISet<string> expectedAreaPathList = new HashSet<string> { "A", "A\\11", "A\\12", "A\\13" };
            ISet<string> expectedIterationPathList = new HashSet<string> { "I", "I\\11", "I\\12", "I\\13" };

            AreaAndIterationPathTree tree = new AreaAndIterationPathTree(nodeList);
            Assert.IsTrue(expectedAreaPathList.SetEquals(tree.AreaPathList));
            Assert.IsTrue(expectedIterationPathList.SetEquals(tree.IterationPathList));
        }

        [TestMethod]
        public void AreaAndIterationPathTree_AreaPathMultiLevelTest()
        {
            IList<WorkItemClassificationNode> nodeList = new List<WorkItemClassificationNode>
            {
                new WorkItemClassificationNode()
                {
                    Name = "1",
                    StructureType = TreeNodeStructureType.Area,
                    Children = new List<WorkItemClassificationNode>()
                    {
                        new WorkItemClassificationNode()
                        {
                            Name = "11",
                            Children = new List<WorkItemClassificationNode>()
                            {
                                new WorkItemClassificationNode()
                                {
                                    Name = "111"
                                }
                            }
                        }
                    }
                },
                new WorkItemClassificationNode(){
                    Name = "DefaultIteration",
                    StructureType = TreeNodeStructureType.Iteration
                }
            };

            ISet<string> expectedAreaPathList = new HashSet<string> { "1", "1\\11", "1\\11\\111" };
            ISet<string> expectedIterationPathList = new HashSet<string> { "DefaultIteration" };
            AreaAndIterationPathTree tree = new AreaAndIterationPathTree(nodeList);
            Assert.IsTrue(expectedAreaPathList.SetEquals(tree.AreaPathList));
            Assert.IsTrue(expectedIterationPathList.SetEquals(tree.IterationPathList));
        }

        [TestMethod]
        public void ReplaceLeadingProjectName_ReplacesWhenProjectNameAtBeginningCaseInsensitive()
        {
            string input = @"sourceProjectName\aaa";
            string sourceProject = "SOURCEprojectName";
            string targetProject = "targetProjectName";

            string expected = @"targetProjectName\aaa";

            string actual = AreaAndIterationPathTree.ReplaceLeadingProjectName(input, sourceProject, targetProject);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ReplaceLeadingProjectName_ReplacesWhenProjectNameAtBeginningAndSourceProjectNameEqualsTargetProjectName()
        {
            string input = @"sourceProjectName\aaa";
            string sourceProject = "sourceProjectName";
            string targetProject = "sourceProjectName";

            string expected = @"sourceProjectName\aaa";

            string actual = AreaAndIterationPathTree.ReplaceLeadingProjectName(input, sourceProject, targetProject);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ReplaceLeadingProjectName_ReplacesWhenProjectNameAtBeginningAndSourceProjectNameEqualsTargetProjectNameCaseInsensitive()
        {
            string input = @"sourceProjectName\aaa";
            string sourceProject = "SOURCEprojectName";
            string targetProject = "SOUrcePROJECTName";

            string expected = @"SOUrcePROJECTName\aaa";

            string actual = AreaAndIterationPathTree.ReplaceLeadingProjectName(input, sourceProject, targetProject);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ReplaceLeadingProjectName_OnlyReplacesProjectNameAtBeginningCaseInsensitive()
        {
            string input = @"sourceProjectName\sourceProjectName\aaa";
            string sourceProject = "SOURCEProjectName";
            string targetProject = "targetProjectName";

            string expected = @"targetProjectName\sourceProjectName\aaa";

            string actual = AreaAndIterationPathTree.ReplaceLeadingProjectName(input, sourceProject, targetProject);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Could not replace leading ProjectName. Input must be a valid TeamProject, AreaPath, or IterationPath.")]
        public void ReplaceLeadingProjectName_ThrowsArgumentExceptionWhenProjectNameIsNotAtBeginning()
        {
            string input = @"xxx\sourceProjectName\aaa";
            string sourceProject = "sourceProjectName";
            string targetProject = "targetProjectName";

            string actual = AreaAndIterationPathTree.ReplaceLeadingProjectName(input, sourceProject, targetProject);
        }

        [TestMethod]
        public void ReplaceLeadingProjectName_ReturnsTargetProjectNameWhenOnlySourceProjectNameWasGivenCaseInsensitive()
        {
            string input = "sourceProjectName";
            string sourceProject = "SOURCEProjectName";
            string targetProject = "targetProjectName";

            string expected = "targetProjectName";

            string actual = AreaAndIterationPathTree.ReplaceLeadingProjectName(input, sourceProject, targetProject);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ReplaceLeadingProjectName_ReturnsTargetProjectNameWhenOnlySourceProjectNameWasGivenAndSourceProjectNameEqualsTargetProjectName()
        {
            string input = "sourceProjectName";
            string sourceProject = "sourceProjectName";
            string targetProject = "sourceProjectName";

            string expected = "sourceProjectName";

            string actual = AreaAndIterationPathTree.ReplaceLeadingProjectName(input, sourceProject, targetProject);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ReplaceLeadingProjectName_ReturnsTargetProjectNameWhenOnlySourceProjectNameWasGivenAndSourceProjectNameEqualsTargetProjectNameCaseSensitive()
        {
            string input = "sourceProjectName";
            string sourceProject = "SOURCEProjectName";
            string targetProject = "SOUrceProjectName";

            string expected = "SOUrceProjectName";

            string actual = AreaAndIterationPathTree.ReplaceLeadingProjectName(input, sourceProject, targetProject);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Could not replace leading ProjectName. Input must be a valid TeamProject, AreaPath, or IterationPath.")]
        public void ReplaceLeadingProjectName_ThrowsArgumentExceptionWhenEmptyInputWasGiven()
        {
            string input = string.Empty;
            string sourceProject = "sourceProjectName";
            string targetProject = "targetProjectName";

            string actual = AreaAndIterationPathTree.ReplaceLeadingProjectName(input, sourceProject, targetProject);

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Could not replace leading ProjectName. Input must be a valid TeamProject, AreaPath, or IterationPath.")]
        public void ReplaceLeadingProjectName_ThrowsArgumentExceptionWhenRandomInputWasGiven()
        {
            string input = "RandomText";
            string sourceProject = "sourceProjectName";
            string targetProject = "targetProjectName";

            string actual = AreaAndIterationPathTree.ReplaceLeadingProjectName(input, sourceProject, targetProject);
        }
    }
}
