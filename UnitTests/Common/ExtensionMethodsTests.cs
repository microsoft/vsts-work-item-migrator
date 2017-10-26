using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common;

namespace UnitTests.Common
{
    [TestClass]
    public class ExtensionMethodsTests
    {
        [TestMethod]
        public void GetKeyIgnoringCase_DictionaryIsEmptyReturnsNull()
        {
            IDictionary<string, object> dictionary = new Dictionary<string, object>();

            string desiredKeyOfAnyCase = "desiredKEYofANYcase";

            string result = dictionary.GetKeyIgnoringCase(desiredKeyOfAnyCase);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetKeyIgnoringCase_DictionaryDoesNotContainReturnsNull()
        {
            IDictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("NotWhatWeWant", new object());

            string desiredKeyOfAnyCase = "desiredKEYofANYcase";

            string result = dictionary.GetKeyIgnoringCase(desiredKeyOfAnyCase);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetKeyIgnoringCase_DictionaryContainsReturnsCorrectKeyFromDictionary()
        {
            IDictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("desiredKeyOfAnyCase", new object());

            string desiredKeyOfAnyCase = "desiredKEYofANYcase";

            string expected = "desiredKeyOfAnyCase";

            string actual = dictionary.GetKeyIgnoringCase(desiredKeyOfAnyCase);

            Assert.AreEqual(expected, actual);
        }
    }
}
