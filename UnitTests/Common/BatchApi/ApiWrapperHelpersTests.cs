using System.Collections.Generic;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common.ApiWrappers;

namespace UnitTests.Common.BatchApi
{
    [TestClass]
    public class ApiWrapperHelpersTests
    {
        [TestMethod]
        public void ResponsesLackExpectedData_WitBatchResponsesIsNullAndBatchIdToWorkItemIdMappingIsPopulatedReturnsTrue()
        {
            List<WitBatchResponse> witBatchResponses = null;

            List<(int SourceId, WitBatchRequest WitBatchRequest)> batchIdToWorkItemIdMapping = new List<(int SourceId, WitBatchRequest WitBatchRequest)>();
            batchIdToWorkItemIdMapping.Add((0, null));

            bool result = ApiWrapperHelpers.ResponsesLackExpectedData(witBatchResponses, batchIdToWorkItemIdMapping);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ResponsesLackExpectedData_WitBatchResponsesIsEmptyAndBatchIdToWorkItemIdMappingIsPopulatedReturnsTrue()
        {
            List<WitBatchResponse> witBatchResponses = new List<WitBatchResponse>();

            List<(int SourceId, WitBatchRequest WitBatchRequest)> batchIdToWorkItemIdMapping = new List<(int SourceId, WitBatchRequest WitBatchRequest)>();
            batchIdToWorkItemIdMapping.Add((0, null));

            bool result = ApiWrapperHelpers.ResponsesLackExpectedData(witBatchResponses, batchIdToWorkItemIdMapping);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ResponsesLackExpectedData_WitBatchResponsesIsPopulatedAndBatchIdToWorkItemIdMappingIsPopulatedReturnsFalse()
        {
            List<WitBatchResponse> witBatchResponses = new List<WitBatchResponse>();
            witBatchResponses.Add(new WitBatchResponse());

            List<(int SourceId, WitBatchRequest WitBatchRequest)> batchIdToWorkItemIdMapping = new List<(int SourceId, WitBatchRequest WitBatchRequest)>();
            batchIdToWorkItemIdMapping.Add((0, null));

            bool result = ApiWrapperHelpers.ResponsesLackExpectedData(witBatchResponses, batchIdToWorkItemIdMapping);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ResponsesLackExpectedData_WitBatchResponsesIsNullAndBatchIdToWorkItemIdMappingIsEmptyReturnsFalse()
        {
            List<WitBatchResponse> witBatchResponses = null;

            List<(int SourceId, WitBatchRequest WitBatchRequest)> batchIdToWorkItemIdMapping = new List<(int SourceId, WitBatchRequest WitBatchRequest)>();

            bool result = ApiWrapperHelpers.ResponsesLackExpectedData(witBatchResponses, batchIdToWorkItemIdMapping);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ResponsesLackExpectedData_WitBatchResponsesIsEmptyAndBatchIdToWorkItemIdMappingIsEmptyReturnsFalse()
        {
            List<WitBatchResponse> witBatchResponses = new List<WitBatchResponse>();

            List<(int SourceId, WitBatchRequest WitBatchRequest)> batchIdToWorkItemIdMapping = new List<(int SourceId, WitBatchRequest WitBatchRequest)>();

            bool result = ApiWrapperHelpers.ResponsesLackExpectedData(witBatchResponses, batchIdToWorkItemIdMapping);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ResponsesLackExpectedData_WitBatchResponsesIsPopulatedAndBatchIdToWorkItemIdMappingIsEmptyReturnsFalse()
        {
            List<WitBatchResponse> witBatchResponses = new List<WitBatchResponse>();
            witBatchResponses.Add(new WitBatchResponse());

            List<(int SourceId, WitBatchRequest WitBatchRequest)> batchIdToWorkItemIdMapping = new List<(int SourceId, WitBatchRequest WitBatchRequest)>();

            bool result = ApiWrapperHelpers.ResponsesLackExpectedData(witBatchResponses, batchIdToWorkItemIdMapping);
            Assert.IsFalse(result);
        }
    }
}
