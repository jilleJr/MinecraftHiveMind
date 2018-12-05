using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MinecraftNetwork.Protocol;

namespace MinecraftNetwork.Tests.Protocol
{
    [TestClass]
    public class VarIntTests
    {
        [TestMethod]

        [DataRow(0x0, new byte[] { 0b0000_0000 },
            DisplayName = "FromStreamZero")]

        [DataRow(0x0, new byte[] { 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b0000_0000 },
            DisplayName = "FromStreamZeroLong")]

        [DataRow(0x1, new byte[] { 0b0000_0001 },
            DisplayName = "FromStreamOne")]

        [DataRow(0x1, new byte[] { 0b1000_0001, 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b0000_0000 },
            DisplayName = "FromStreamOneLong")]

        [DataRow(0x0, new byte[] { 0b0000_0000, 0x29, 0xBA },
            DisplayName = "FromStreamZeroFollowedByUnusedBytes")]

        [DataRow(0x1, new byte[] { 0b0000_0001, 0x65, 0xAE },
            DisplayName = "FromStreamOneFollowedByUnusedBytes")]

        [DataRow(0x80, new byte[] { 0b1000_0000, 0b0000_0001 },
            DisplayName = "FromStreamMultipleBytes")]

        [DataRow(0x81, new byte[] { 0b1000_0001, 0b0000_0001, 0xAF, 0x9C },
            DisplayName = "FromStreamMultipleBytesFollowedByUnusedBytes")]

        [DataRow(0x7FFF_FFFF, new byte[] { 0b1111_1111, 0b1111_1111, 0b1111_1111, 0b1111_1111, 0b0000_0111 },
            DisplayName = "FromStreamMax")]

        [DataRow(-0x8000_0000, new byte[] { 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b0000_1000 },
            DisplayName = "FromStreamMin")]

        [DataRow(-0x1, new byte[] { 0b1111_1111, 0b1111_1111, 0b1111_1111, 0b1111_1111, 0b0000_1111 },
            DisplayName = "FromStreamNegativeOne")]

        [DataRow(0, new byte[] { 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b0111_0000 },
            DisplayName = "FromStreamUnusedLastBytes")]

        public void FromStream(int expected, byte[] bytes)
        {
            // Arrange
            VarInt varInt;

            using (var stream = new MemoryStream(bytes))
            {
                // Act
                varInt = VarInt.FromStream(stream);
            }

            // Assert
            Assert.AreEqual(expected, varInt.Value, $"Expected: 0x{expected:x4}, actual: 0x{varInt.Value:x4}.\nInput: [{string.Join(" ", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')))}]");
        }

        [TestMethod]
        public void FromStreamTooManyBytes()
        {
            // Arrange
            var bytes = new byte[] { 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b1000_0000 };
            // Act
            Assert.ThrowsException<OverflowException>(() =>
            {
                using (var stream = new MemoryStream(bytes))
                {
                    VarInt.FromStream(stream);
                }
            });
        }

        [TestMethod]
        public void FromStreamBytesShort()
        {
            // Arrange
            var input = new byte[] { 0b0000_0011, 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b1000_0000 };
            var expected = new byte[] { 0b0000_0011 };
            VarInt result;

            // Act
            using (var stream = new MemoryStream(input))
            {
                result = VarInt.FromStream(stream);
            }

            // Assert
            Assert.AreEqual(3, result.Value);
            CollectionAssert.AreEqual(expected, result.Bytes);
        }

        [TestMethod]
        public void FromStreamBytesExcessivelyLong()
        {
            // Arrange
            var input = new byte[] { 0b1000_0011, 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b0000_0000, 0b1000_1010, 0b1100_0000 };
            var expected = new byte[] { 0b1000_0011, 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b0000_0000 };
            VarInt result;

            // Act
            using (var stream = new MemoryStream(input))
            {
                result = VarInt.FromStream(stream);
            }

            // Assert
            Assert.AreEqual(3, result.Value);
            CollectionAssert.AreEqual(expected, result.Bytes, $"Expected: [{string.Join(" ", expected.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')))}], Actual: [{string.Join(" ", result.Bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')))}]\nInput: [{string.Join(", ", input.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')))}]");
        }

        [TestMethod]

        [DataRow(0, new byte[] { 0b0000_0000 })]
        [DataRow(1, new byte[] { 0b0000_0001 })]
        [DataRow(2, new byte[] { 0b0000_0010 })]
        [DataRow(127, new byte[] { 0b0111_1111 })]
        [DataRow(128, new byte[] { 0b1000_0000, 0b0000_0001 })]
        [DataRow(255, new byte[] { 0b1111_1111, 0b0000_0001 })]
        [DataRow(2147483647, new byte[] { 0b1111_1111, 0b1111_1111, 0b1111_1111, 0b1111_1111, 0b0000_0111 })]
        [DataRow(-1, new byte[] { 0b1111_1111, 0b1111_1111, 0b1111_1111, 0b1111_1111, 0b0000_1111 })]
        [DataRow(-2147483648, new byte[] { 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b1000_0000, 0b0000_1000 })]
        public void ToBytes(int input, byte[] expected)
        {
            // Arrange
            byte[] actual = new VarInt(input).Bytes;

            // Assert
            CollectionAssert.AreEqual(expected, actual, $"Expected: [{string.Join(" ", expected.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')))}], Actual: [{string.Join(" ", actual.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')))}]\nInput: 0x{input:x8}");
        }

    }
}