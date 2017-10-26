using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Logging;

namespace UnitTests.Logging
{
    [TestClass]
    public class LogLevelOutputMappingTests
    {
        [TestMethod]
        public void Get_LogLevelTraceReturnsSuccess()
        {
            string expected = "Success";
            LogLevel logLevel = LogLevel.Trace;

            string actual = LogLevelOutputMapping.Get(logLevel);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Get_LogLevelDebugReturnsDebug()
        {
            string expected = "Debug";
            LogLevel logLevel = LogLevel.Debug;

            string actual = LogLevelOutputMapping.Get(logLevel);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Get_LogLevelInformationReturnsInfo()
        {
            string expected = "Info";
            LogLevel logLevel = LogLevel.Information;

            string actual = LogLevelOutputMapping.Get(logLevel);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Get_LogLevelWarningReturnsWarning()
        {
            string expected = "Warning";
            LogLevel logLevel = LogLevel.Warning;

            string actual = LogLevelOutputMapping.Get(logLevel);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Get_LogLevelErrorReturnsError()
        {
            string expected = "Error";
            LogLevel logLevel = LogLevel.Error;

            string actual = LogLevelOutputMapping.Get(logLevel);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Get_LogLevelCriticalReturnsCritical()
        {
            string expected = "Critical";
            LogLevel logLevel = LogLevel.Critical;

            string actual = LogLevelOutputMapping.Get(logLevel);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Get_LogLevelNoneReturnsNone()
        {
            string expected = "None";
            LogLevel logLevel = LogLevel.None;

            string actual = LogLevelOutputMapping.Get(logLevel);
            Assert.AreEqual(expected, actual);
        }
    }
}
