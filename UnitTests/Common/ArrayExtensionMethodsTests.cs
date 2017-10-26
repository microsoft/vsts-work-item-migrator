using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Common
{
    [TestClass]
    public class ArrayExtensionMethodsTests
    {
        [TestMethod]
        public void SubArray_ReturnsCorrectValueForRegularCase()
        {
            int[] array = { 0, 1, 2, 3, 4, 5, 6, 7 };
            int index = 3;
            int length = 4;

            int[] actual = ArrayExtensions.SubArray(array, index, length);
            int[] expected = { 3, 4, 5, 6 };

            for (int i = 0; i < length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void SubArray_ReturnsCorrectValueForBeginningToMiddle()
        {
            int[] array = { 0, 1, 2, 3, 4, 5, 6, 7 };
            int index = 0;
            int length = 4;

            int[] actual = ArrayExtensions.SubArray(array, index, length);
            int[] expected = { 0, 1, 2, 3 };

            for (int i = 0; i < length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void SubArray_ReturnsCorrectValueForLength1()
        {
            int[] array = { 0, 1, 2, 3, 4, 5, 6, 7 };
            int index = 3;
            int length = 1;

            int[] actual = ArrayExtensions.SubArray(array, index, length);
            int[] expected = { 3 };

            for (int i = 0; i < length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void SubArray_ReturnsCorrectValueForLength0()
        {
            int[] array = { 0, 1, 2, 3, 4, 5, 6, 7 };
            int index = 3;
            int length = 0;

            int[] actual = ArrayExtensions.SubArray(array, index, length);
            int[] expected = { };

            for (int i = 0; i < length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void SubArray_ReturnsCorrectValueForFullLength()
        {
            int[] array = { 0, 1, 2, 3, 4, 5, 6, 7 };
            int index = 0;
            int length = 8;

            int[] actual = ArrayExtensions.SubArray(array, index, length);
            int[] expected = { 0, 1, 2, 3, 4, 5, 6, 7 };

            for (int i = 0; i < length; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
    }
}
