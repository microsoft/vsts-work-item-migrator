using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Logging;

namespace UnitTests.Logging
{
    [TestClass]
    public class LogItemTests
    {
        [TestMethod]
        public void OutputFormat_FormatIsCorrectWhenIncludeExceptionMessageIsFalseAndIncludeLogLevelTimeStampIsTrueAndExceptionIsNull()
        {
            string expected = "[Error] [00.00.00.000] message";
            bool includeExceptionMessage = false;
            bool includeLogLevelTimeStamp = true;
            int logDestination = LogDestination.All;

            // constructor parameters
            LogLevel logLevel = LogLevel.Error;
            DateTime dateTimeStamp = new DateTime(2010, 1, 1, 0, 0, 0);
            string message = "message";

            var logItemMock = new Mock<LogItem>(logLevel, dateTimeStamp, message, logDestination);
            logItemMock.Setup(a => a.DateTimeStampString()).Returns("00.00.00.000");
            logItemMock.Setup(u => u.LogLevelName()).Returns("Error");

            string actual = logItemMock.Object.OutputFormat(includeExceptionMessage, includeLogLevelTimeStamp);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void OutputFormat_FormatIsCorrectWhenIncludeExceptionMessageIsFalseAndIncludeLogLevelTimeStampIsTrueAndExceptionExistsWithNullMessage()
        {
            string expected = "[Error] [00.00.00.000] message";
            bool includeExceptionMessage = false;
            bool includeLogLevelTimeStamp = true;
            int logDestination = LogDestination.All;

            // constructor parameters
            LogLevel logLevel = LogLevel.Error;
            DateTime dateTimeStamp = new DateTime(2010, 1, 1, 0, 0, 0);
            string message = "message";
            Exception exception = new Exception(null);

            var logItemMock = new Mock<LogItem>(logLevel, dateTimeStamp, message, exception, logDestination);
            logItemMock.Setup(a => a.DateTimeStampString()).Returns("00.00.00.000");
            logItemMock.Setup(u => u.LogLevelName()).Returns("Error");

            string actual = logItemMock.Object.OutputFormat(includeExceptionMessage, includeLogLevelTimeStamp);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void OutputFormat_FormatIsCorrectWhenIncludeExceptionMessageIsFalseAndIncludeLogLevelTimeStampIsTrueAndExceptionExistsWithMessage()
        {
            string expected = "[Error] [00.00.00.000] message";
            bool includeExceptionMessage = false;
            bool includeLogLevelTimeStamp = true;
            int logDestination = LogDestination.All;

            // constructor parameters
            LogLevel logLevel = LogLevel.Error;
            DateTime dateTimeStamp = new DateTime(2010, 1, 1, 0, 0, 0);
            string message = "message";
            Exception exception = new Exception("This is sample Exception Message.");

            var logItemMock = new Mock<LogItem>(logLevel, dateTimeStamp, message, exception, logDestination);
            logItemMock.Setup(a => a.DateTimeStampString()).Returns("00.00.00.000");
            logItemMock.Setup(u => u.LogLevelName()).Returns("Error");

            string actual = logItemMock.Object.OutputFormat(includeExceptionMessage, includeLogLevelTimeStamp);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void OutputFormat_FormatIsCorrectWhenIncludeExceptionMessageIsTrueAndIncludeLogLevelTimeStampIsTrueAndExceptionIsNull()
        {
            string expected = "[Error] [00.00.00.000] message";
            bool includeExceptionMessage = true;
            bool includeLogLevelTimeStamp = true;
            int logDestination = LogDestination.All;

            // constructor parameters
            LogLevel logLevel = LogLevel.Error;
            DateTime dateTimeStamp = new DateTime(2010, 1, 1, 0, 0, 0);
            string message = "message";

            var logItemMock = new Mock<LogItem>(logLevel, dateTimeStamp, message, logDestination);
            logItemMock.Setup(a => a.DateTimeStampString()).Returns("00.00.00.000");
            logItemMock.Setup(u => u.LogLevelName()).Returns("Error");

            string actual = logItemMock.Object.OutputFormat(includeExceptionMessage, includeLogLevelTimeStamp);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void OutputFormat_FormatIsCorrectWhenIncludeExceptionMessageIsTrueAndIncludeLogLevelTimeStampIsTrueAndExceptionExistsWithNullMessage()
        {
            string expected = "[Error] [00.00.00.000] message. Exception of type 'System.Exception' was thrown.";
            bool includeExceptionMessage = true;
            bool includeLogLevelTimeStamp = true;
            int logDestination = LogDestination.All;

            // constructor parameters
            LogLevel logLevel = LogLevel.Error;
            DateTime dateTimeStamp = new DateTime(2010, 1, 1, 0, 0, 0);
            string message = "message";
            Exception exception = new Exception(null);

            var logItemMock = new Mock<LogItem>(logLevel, dateTimeStamp, message, exception, logDestination);
            logItemMock.Setup(a => a.DateTimeStampString()).Returns("00.00.00.000");
            logItemMock.Setup(u => u.LogLevelName()).Returns("Error");

            string actual = logItemMock.Object.OutputFormat(includeExceptionMessage, includeLogLevelTimeStamp);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void OutputFormat_FormatIsCorrectWhenIncludeExceptionMessageIsTrueAndIncludeLogLevelTimeStampIsTrueAndExceptionExistsWithMessage()
        {
            string expected = "[Error] [00.00.00.000] message. This is sample Exception Message.";
            bool includeExceptionMessage = true;
            bool includeLogLevelTimeStamp = true;
            int logDestination = LogDestination.All;

            // constructor parameters
            LogLevel logLevel = LogLevel.Error;
            DateTime dateTimeStamp = new DateTime(2010, 1, 1, 0, 0, 0);
            string message = "message";
            Exception exception = new Exception("This is sample Exception Message.");

            var logItemMock = new Mock<LogItem>(logLevel, dateTimeStamp, message, exception, logDestination);
            logItemMock.Setup(a => a.DateTimeStampString()).Returns("00.00.00.000");
            logItemMock.Setup(u => u.LogLevelName()).Returns("Error");

            string actual = logItemMock.Object.OutputFormat(includeExceptionMessage, includeLogLevelTimeStamp);

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void OutputFormat_FormatIsCorrectWhenIncludeExceptionMessageIsFalseAndIncludeLogLevelTimeStampIsFalseAndExceptionIsNull()
        {
            string expected = "message";
            bool includeExceptionMessage = false;
            bool includeLogLevelTimeStamp = false;
            int logDestination = LogDestination.All;

            // constructor parameters
            LogLevel logLevel = LogLevel.Error;
            DateTime dateTimeStamp = new DateTime(2010, 1, 1, 0, 0, 0);
            string message = "message";

            var logItemMock = new Mock<LogItem>(logLevel, dateTimeStamp, message, logDestination);
            logItemMock.Setup(a => a.DateTimeStampString()).Returns("00.00.00.000");
            logItemMock.Setup(u => u.LogLevelName()).Returns("Error");

            string actual = logItemMock.Object.OutputFormat(includeExceptionMessage, includeLogLevelTimeStamp);

            Assert.AreEqual(expected, actual);
        }
    }
}
