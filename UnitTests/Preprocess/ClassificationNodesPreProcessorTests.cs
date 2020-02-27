using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;
using Common.Migration;
using System.Collections.Concurrent;
using Common.Config;
using Moq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace UnitTests.Preprocess
{
    [TestClass]
    public class ClassificationNodesPreProcessorTests
    {
        [TestMethod]
        public async Task ProcessAreaPaths_Test()
        {
            var processorMock = new Mock<ClassificationNodesPreProcessor>();

            // populate source area paths
            AreaAndIterationPathTree sourceTree = new AreaAndIterationPathTree(new List<WorkItemClassificationNode>()
            {
                new WorkItemClassificationNode()
                {
                    Name = "Root Node",
                    Children = new List<WorkItemClassificationNode>()
                    {
                        new WorkItemClassificationNode()
                        {
                            Name = "Child Node 1",
                        },
                        new WorkItemClassificationNode()
                        {
                            Name = "Child Node 2",
                        },
                        new WorkItemClassificationNode()
                        {
                            Name = "Child Node 3",
                        },
                    }
                }
            });

            // Partial target tree
            AreaAndIterationPathTree targetTree = new AreaAndIterationPathTree(new List<WorkItemClassificationNode>()
            {
                new WorkItemClassificationNode()
                {
                    Name = "Root Node",
                    Children = new List<WorkItemClassificationNode>()
                    {
                        new WorkItemClassificationNode()
                        {
                            Name = "Child Node 1",
                        },
                    }
                }
            });

            var contextMock = new Mock<IMigrationContext>();
            var batchContextMock = new Mock<IBatchMigrationContext>();
            contextMock.SetupGet(ctx => ctx.Config).Returns(new ConfigJson()
            {
                SourceConnection = new ConfigConnection()
                {
                    Project = "Test Src Project"
                },
                TargetConnection = new ConfigConnection()
                {
                    Project = "Test Target Project"
                }
            });
            contextMock.SetupGet(ctx => ctx.SourceAreaAndIterationTree).Returns(sourceTree);
            contextMock.SetupGet(ctx => ctx.TargetAreaAndIterationTree).Returns(targetTree);
            batchContextMock.SetupGet(ctx => ctx.SourceWorkItems).Returns(new List<WorkItem>() {
                new WorkItem()
                {
                    Fields = new Dictionary<string, object>()
                    {
                        { "System.AreaPath", "Root Node\\Child Node 3"}
                    }
                },
                new WorkItem()
                {
                    Fields = new Dictionary<string, object>()
                    {
                        { "System.AreaPath", "Root Node\\Child Node 1"}
                    }
                },
                new WorkItem()
                {
                    Fields = new Dictionary<string, object>()
                    {
                        { "System.AreaPath", "Root Node"}
                    }
                },
            });

            await processorMock.Object.Prepare(contextMock.Object);
            int modified = await processorMock.Object.ProcessAreaPaths(batchContextMock.Object, 0);

            processorMock.Verify(x => x.CreateAreaPath(It.IsIn("Root Node\\Child Node 3")), Times.Once);
            Assert.IsTrue(modified == 1);

        }

        [TestMethod]
        public async Task ProcessIterationPaths_Test()
        {
            var processorMock = new Mock<ClassificationNodesPreProcessor>();
            var sprint2Node = new WorkItemClassificationNode()
            {
                Name = "Sprint 2",
            };
            // populate source area paths
            AreaAndIterationPathTree sourceTree = new AreaAndIterationPathTree(new List<WorkItemClassificationNode>()
            {
                        new WorkItemClassificationNode()
                        {
                            Name = "Test Src Project",
                            StructureType = TreeNodeStructureType.Iteration,
                            Children = new List<WorkItemClassificationNode>()
                            {
                                new WorkItemClassificationNode()
                                {
                                    Name = "Sprint 1",
                                    StructureType = TreeNodeStructureType.Iteration
                                },
                                sprint2Node,
                            }
                        },
        });

            // Partial target tree
            AreaAndIterationPathTree targetTree = new AreaAndIterationPathTree(new List<WorkItemClassificationNode>()
            {
                        new WorkItemClassificationNode()
                        {
                            Name = "Test Target Project",
                            StructureType = TreeNodeStructureType.Iteration,
                            Children = new List<WorkItemClassificationNode>()
                            {
                                new WorkItemClassificationNode()
                                {
                                    Name = "Sprint 1",
                                    StructureType = TreeNodeStructureType.Iteration
                                },
                            }
                        },
            });

            var contextMock = new Mock<IMigrationContext>();
            var batchContextMock = new Mock<IBatchMigrationContext>();
            contextMock.SetupGet(ctx => ctx.Config).Returns(new ConfigJson()
            {
                SourceConnection = new ConfigConnection()
                {
                    Project = "Test Src Project"
                },
                TargetConnection = new ConfigConnection()
                {
                    Project = "Test Target Project"
                }
            });
            contextMock.SetupGet(ctx => ctx.SourceAreaAndIterationTree).Returns(sourceTree);
            contextMock.SetupGet(ctx => ctx.TargetAreaAndIterationTree).Returns(targetTree);
            batchContextMock.SetupGet(ctx => ctx.SourceWorkItems).Returns(new List<WorkItem>() {
                new WorkItem()
                {
                    Fields = new Dictionary<string, object>()
                    {
                        { "System.IterationPath", "Test Src Project\\Sprint 1"}
                    }
                },
                new WorkItem()
                {
                    Fields = new Dictionary<string, object>()
                    {
                        { "System.IterationPath", "Test Src Project\\Sprint 2"}
                    }
                }
            });

            await processorMock.Object.Prepare(contextMock.Object);
            int modified = await processorMock.Object.ProcessIterationPaths(batchContextMock.Object, 0);

            processorMock.Verify(x => x.CreateIterationPath(It.IsIn("Test Src Project\\Sprint 2"), It.IsAny<WorkItemClassificationNode>()), Times.Once);
            Assert.AreEqual(modified, 1);

        }
    }

}