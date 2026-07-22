using System.Globalization;
using ActivityDashboard.UI;
using NUnit.Framework;

namespace Dashboard.Tests
{
    [TestFixture]
    public class ProgressWidthConverterTests
    {
        private ProgressWidthConverter converter;

        [SetUp]
        public void SetUp()
        {
            converter = new ProgressWidthConverter();
        }

        [Test]
        public void Convert_ReturnsZero_WhenValueIsZero()
        {
            var result = converter.Convert(new object[] { 0.0, 100.0, 250.0 }, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(0.0, (double)result, 0.0001);
        }

        [Test]
        public void Convert_ReturnsFullWidth_WhenValueEqualsMaximum()
        {
            var result = converter.Convert(new object[] { 100.0, 100.0, 250.0 }, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(250.0, (double)result, 0.0001);
        }

        [Test]
        public void Convert_ReturnsProportionalWidth_ForPartialValue()
        {
            var result = converter.Convert(new object[] { 50.0, 100.0, 240.0 }, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(120.0, (double)result, 0.0001);
        }

        [Test]
        public void Convert_HandlesCustomMaximum()
        {
            var result = converter.Convert(new object[] { 3.0, 6.0, 200.0 }, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(100.0, (double)result, 0.0001);
        }

        [Test]
        public void Convert_ClampsAboveMaximum_ToContainerWidth()
        {
            var result = converter.Convert(new object[] { 150.0, 100.0, 200.0 }, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(200.0, (double)result, 0.0001);
        }

        [Test]
        public void Convert_ClampsNegativeValue_ToZero()
        {
            var result = converter.Convert(new object[] { -10.0, 100.0, 200.0 }, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(0.0, (double)result, 0.0001);
        }

        [Test]
        public void Convert_ReturnsZero_WhenMaximumIsZero()
        {
            var result = converter.Convert(new object[] { 50.0, 0.0, 200.0 }, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(0.0, (double)result, 0.0001);
        }

        [Test]
        public void Convert_ReturnsZero_WhenContainerWidthIsZero()
        {
            var result = converter.Convert(new object[] { 50.0, 100.0, 0.0 }, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(0.0, (double)result, 0.0001);
        }

        [Test]
        public void Convert_HandlesNullValues_Gracefully()
        {
            var result = converter.Convert(new object[] { null, null, null }, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(0.0, (double)result, 0.0001);
        }

        [Test]
        public void Convert_ReturnsZero_WhenValuesArrayIsTooShort()
        {
            var result = converter.Convert(new object[] { 50.0, 100.0 }, typeof(double), null, CultureInfo.InvariantCulture);
            Assert.AreEqual(0.0, (double)result, 0.0001);
        }

        [Test]
        public void ConvertBack_ThrowsNotSupported()
        {
            Assert.Throws<System.NotSupportedException>(() => converter.ConvertBack(null, null, null, CultureInfo.InvariantCulture));
        }
    }
}
