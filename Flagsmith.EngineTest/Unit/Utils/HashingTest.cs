using Xunit;
using System.Collections.Generic;
using FlagsmithEngine.Utils;
using System;
using System.Linq;
using System.Collections;
using Moq;
using System.Text;
using FlagsmithEngine.Interfaces;

namespace EngineTest.Unit.Utils
{
    public class HashingTest
    {
        [Theory]
        [InlineData(new object[] { new string[] { "12", "93" } })]
        [InlineData(new object[] { new string[] { "0DEC57D2-D8F2-4F1F-BD1C-E6BF10B1B47B", "99" } })]
        [InlineData(new object[] { new string[] { "99", "0DEC57D2-D8F2-4F1F-BD1C-E6BF10B1B47B" } })]
        [InlineData(new object[] { new string[] { "FE25DB15-F618-40F2-9F18-796D29EDF61F", "0DEC57D2-D8F2-4F1F-BD1C-E6BF10B1B47B" } })]
        public void TestGetHashedPercentageForObjectIdsINumberBetween0IncAnd100Exc(string[] objectIds)
        {
            var percentage = new Hashing().GetHashedPercentageForObjectIds(objectIds.ToList());
            Assert.True(100 > percentage && percentage >= 0);
        }
        [Theory]
        [InlineData(new object[] { new string[] { "12", "93" } })]
        [InlineData(new object[] { new string[] { "0DEC57D2-D8F2-4F1F-BD1C-E6BF10B1B47B", "99" } })]
        [InlineData(new object[] { new string[] { "99", "0DEC57D2-D8F2-4F1F-BD1C-E6BF10B1B47B" } })]
        [InlineData(new object[] { new string[] { "FE25DB15-F618-40F2-9F18-796D29EDF61F", "0DEC57D2-D8F2-4F1F-BD1C-E6BF10B1B47B" } })]
        public void TestGetHashedPercentageForObjectIdsIsTheSameEachTime(string[] objectIds)
        {
            var result1 = new Hashing().GetHashedPercentageForObjectIds(objectIds.ToList());
            var result2 = new Hashing().GetHashedPercentageForObjectIds(objectIds.ToList());
            Assert.Equal(result1, result2);
        }
        [Fact]
        public void TestPercentageValueIsUniqueForDifferentIdentities()
        {
            var result1 = new Hashing().GetHashedPercentageForObjectIds(new List<string> { "14", "106" });
            var result2 = new Hashing().GetHashedPercentageForObjectIds(new List<string> { "53", "200" });
            Assert.NotEqual(result1, result2);
        }
        [Fact]
        public void TestGetHashedPercentageForObjectIdsShouldBeEvenlyDistributed()
        {
            var testSample = 500;
            var numTestBuckets = 50;
            var testBucketSize = testSample / numTestBuckets;
            var error_factor = 0.1;
            var objectIdPairs = Enumerable.Range(0, testSample).ToList()
                .Select(x => Enumerable.Range(0, testSample).ToList()
                     .Select(y => new List<string> { x.ToString(), y.ToString() })).SelectMany(x => x);
            var hashedValue = objectIdPairs.Select(x => new Hashing().GetHashedPercentageForObjectIds(x)).ToList();
            hashedValue.Sort();
            foreach (var i in Enumerable.Range(0, numTestBuckets))
            {
                var bucketStart = i * testBucketSize;
                var bucketEnd = (i + 1) * testBucketSize;
                var bucketValueLimit = Math.Min(((double)(i + 1) / numTestBuckets) + error_factor * ((i + 1) / numTestBuckets), 1);
                var x1 = hashedValue.GetRange(bucketStart, bucketEnd - bucketStart);
                Assert.True(x1.All(x => x <= bucketValueLimit));
            }

        }
        [Fact]
        public void TestGetHashedPercentageDoesNotReturn1()
        {

            var hashStringToReturn1 = "270e";
            var hashStringToReturn0 = "270f";
            var objectIDs = new List<string>() { "12", "93" };
            var HasingMock = new Mock<Hashing>();
            HasingMock.CallBase = true;
            var mockSetup = HasingMock.SetupSequence(p => p.HashBytesToString(It.IsAny<byte[]>()))
                .Returns(hashStringToReturn1)
                .Returns(hashStringToReturn0);
            var val = HasingMock.Object.GetHashedPercentageForObjectIds(objectIDs);

            Assert.True(val == 0);

            HasingMock.Verify(s => s.ComputeHash(It.IsAny<string>()), Times.Exactly(2));

            var expressionBytes1 = String.Join(",", HasingMock.Object.repeatIdsList(objectIDs, 1));
            var expressionBytes2 = String.Join(",", HasingMock.Object.repeatIdsList(objectIDs, 2));

            HasingMock.Verify(s => s.ComputeHash(expressionBytes1), Times.Exactly(1));
            HasingMock.Verify(s => s.ComputeHash(expressionBytes2), Times.Exactly(1));
        }
    }
}
