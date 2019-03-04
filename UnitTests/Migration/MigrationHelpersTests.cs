using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common.Migration;
using System;

namespace UnitTests.Migration
{
    [TestClass]
    public class MigrationHelpersTests
    {
        [TestMethod]
        public void GetInlineImageUrlsFromField_ReturnsEmptyHashSetWhenHtmlFieldContentIsNull()
        {
            string fieldHtmlContent = null;
            HashSet<string> actual = MigrationHelpers.GetInlineImageUrlsFromField(fieldHtmlContent, "https://dev.azure.com/account/");
            Assert.IsTrue(actual.Count == 0);
        }

        [TestMethod]
        public void GetInlineImageUrlsFromField_ReturnsEmptyHashSetWhenHtmlFieldContentIsEmptyString()
        {
            string fieldHtmlContent = string.Empty;
            HashSet<string> actual = MigrationHelpers.GetInlineImageUrlsFromField(fieldHtmlContent, "https://dev.azure.com/account/");
            Assert.IsTrue(actual.Count == 0);
        }

        [TestMethod]
        public void GetInlineImageHtmlTags_ReturnsEmptyHashSetWhenAccountUrlDoesNotMatch()
        {
            string input = "In the workItemRelations api, we return link information, but do not tell them if it's a forward/reverse link, nor do we tell them what the opposite direction of the link is.<div><br></div><div>This is handy information to know so that consumers don't have to do string parsing based on if the reference name ends in -Forward/-Reverse to figure it out.</div><div><br></div><div><img src=\"https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\" style=\"width:779.45px;\"><br>&nbsp;<br></div>";
            IList<string> actual = MigrationHelpers.GetInlineImageHtmlTags(input, "https://dev.azure.com/account2/");
            Assert.IsTrue(actual.Count == 0);
        }

        [TestMethod]
        public void GetInlineImageHtmlTags_ReturnsCorrectValueForNoInlineImageHtmlTag()
        {
            string input = "In the workItemRelations api, we return link information, but do not tell them if it's a forward/reverse link, nor do we tell them what the opposite direction of the link is.<div><br></div><div>This is handy information to know so that consumers don't have to do string parsing based on if the reference name ends in -Forward/-Reverse to figure it out.</div><div><br></div><div><br>&nbsp;<br></div>";

            IList<string> expected = new List<string>();

            IList<string> actual = MigrationHelpers.GetInlineImageHtmlTags(input, "https://dev.azure.com/account/");
            Assert.AreEqual(expected.Count, actual.Count);
            Assert.IsTrue(expected.Intersect(actual).Count() == expected.Count);
        }

        [TestMethod]
        public void GetInlineImageHtmlTags_ReturnsCorrectValueForOneInlineImageHtmlTag()
        {
            string input = "In the workItemRelations api, we return link information, but do not tell them if it's a forward/reverse link, nor do we tell them what the opposite direction of the link is.<div><br></div><div>This is handy information to know so that consumers don't have to do string parsing based on if the reference name ends in -Forward/-Reverse to figure it out.</div><div><br></div><div><img src=\"https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\" style=\"width:779.45px;\"><br>&nbsp;<br></div>";

            IList<string> expected = new List<string>();
            expected.Add("<img src=\"https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\" style=\"width:779.45px;\">");

            IList<string> actual = MigrationHelpers.GetInlineImageHtmlTags(input, "https://dev.azure.com/account/");
            Assert.AreEqual(expected.Count, actual.Count);
            Assert.IsTrue(expected.Intersect(actual).Count() == expected.Count);
        }

        [TestMethod]
        public void GetInlineImageHtmlTags_ReturnsCorrectValueForOneInlineImageHtmlTagWithStyleBeforeSrc()
        {
            string input = "In the workItemRelations api, we return link information, but do not tell them if it's a forward/reverse link, nor do we tell them what the opposite direction of the link is.<div><br></div><div>This is handy information to know so that consumers don't have to do string parsing based on if the reference name ends in -Forward/-Reverse to figure it out.</div><div><br></div><div><img style=\"width:779.45px;\" src=\"https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\"><br>&nbsp;<br></div>";

            IList<string> expected = new List<string>();
            expected.Add("<img style=\"width:779.45px;\" src=\"https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\">");

            IList<string> actual = MigrationHelpers.GetInlineImageHtmlTags(input, "https://dev.azure.com/account/");
            Assert.AreEqual(expected.Count, actual.Count);
            Assert.IsTrue(expected.Intersect(actual).Count() == expected.Count);
        }

        [TestMethod]
        public void GetInlineImageHtmlTags_ReturnsCorrectValueForTwoInlineImageHtmlTags()
        {
            string input = "In <img src=\"https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=bbbbbbbb-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\" style=\"width:779.45px;\"> the workItemRelations api, we return link information, but do not tell them if it's a forward/reverse link, nor do we tell them what the opposite direction of the link is.<div><br></div><div>This is handy information to know so that consumers don't have to do string parsing based on if the reference name ends in -Forward/-Reverse to figure it out.</div><div><br></div><div><img src=\"https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\" style=\"width:779.45px;\"><br>&nbsp;<br></div>";

            IList<string> expected = new List<string>();
            expected.Add("<img src=\"https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=bbbbbbbb-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\" style=\"width:779.45px;\">");
            expected.Add("<img src=\"https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\" style=\"width:779.45px;\">");

            IList<string> actual = MigrationHelpers.GetInlineImageHtmlTags(input, "https://dev.azure.com/account/");
            Assert.AreEqual(expected.Count, actual.Count);
            Assert.IsTrue(expected.Intersect(actual).Count() == expected.Count);
        }

        [TestMethod]
        public void GetInlineImageHtmlTags_ReturnsCorrectValueForOneCorrectAndOneInvalidInlineImageHtmlTags()
        {
            string input = "In <img src=\"https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=bbbbbbbb-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\" style=\"width:779.45px;\"> the workItemRelations api, we return link information, but do not tell them if it's a forward/reverse link, nor do we tell them what the opposite direction of the link is.<div><br></div><div>This is handy information to know so that consumers don't have to do string parsing based on if the reference name ends in -Forward/-Reverse to figure it out.</div><div><br></div><div><img src=\"https://dev.azure.com/account2/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\" style=\"width:779.45px;\"><br>&nbsp;<br></div>";

            IList<string> expected = new List<string>();
            expected.Add("<img src=\"https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=bbbbbbbb-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\" style=\"width:779.45px;\">");

            IList<string> actual = MigrationHelpers.GetInlineImageHtmlTags(input, "https://dev.azure.com/account/");
            Assert.AreEqual(expected.Count, actual.Count);
            Assert.IsTrue(expected.Intersect(actual).Count() == expected.Count);
        }

        [TestMethod]
        public void GetUrlFromHtmlTag_ReturnsCorrectValue()
        {
            string input = "<img src=\"https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png\" style=\"width:779.45px;\">";
            string expected = "https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&amp;FileName=temp1498074488622.png";
            string actual = MigrationHelpers.GetUrlFromHtmlTag(input);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetAttachmentUrlGuid_ReturnsCorrectValue()
        {
            string url = "https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&FileName=temp149807448ZZZZ.png";
            Guid expected = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            Guid actual = MigrationHelpers.GetAttachmentUrlGuid(url);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ReplaceAttachmentUrlGuid_ReplacesCorretly()
        {
            string url = "https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&FileName=temp149807448ZZZZ.png";
            string newGuid = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
            string expected = "https://dev.azure.com/account/WorkItemTracking/v1.0/AttachFileHandler.ashx?FileNameGuid=bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb&FileName=temp149807448ZZZZ.png";
            string actual = MigrationHelpers.ReplaceAttachmentUrlGuid(url, newGuid);
            Assert.AreEqual(expected, actual);
        }
    }
}
