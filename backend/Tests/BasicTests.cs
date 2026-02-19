using Xunit;

namespace UmiHealthPOS.Tests
{
    /// <summary>
    /// Basic test suite to verify test framework is working
    /// </summary>
    public class BasicTests
    {
        [Fact]
        public void Test_Framework_Works()
        {
            // Arrange
            var expected = 1;
            var actual = 1;

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Test_String_Operations()
        {
            // Arrange
            var testString = "Sepio AI Integration";

            // Act & Assert
            Assert.NotNull(testString);
            Assert.Contains("Sepio", testString);
            Assert.Contains("AI", testString);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        public void Test_Theory_EqualValues(int a, int b)
        {
            Assert.Equal(a, b);
        }
    }
}
