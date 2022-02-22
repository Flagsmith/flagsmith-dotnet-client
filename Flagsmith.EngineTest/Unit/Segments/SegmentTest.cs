using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using FlagsmithEngine.Segment.Models;
using FlagsmithEngine.Segment;
using System.Linq;

namespace EngineTest.Unit.Segments
{
    public class SegmentTest
    {

        [Theory]
        [InlineData(new object[] { new bool[] { }, true })]
        [InlineData(new object[] { new bool[] { false }, true })]
        [InlineData(new object[] { new bool[] { false, false }, true })]
        [InlineData(new object[] { new bool[] { false, true }, false })]
        [InlineData(new object[] { new bool[] { true, true }, false })]
        public void TestSegmentRuleNone(bool[] iteratable, bool expectedResult)
        {
            var condition = new SegmentRuleModel() { Type = Constants.NoneRule };
            Assert.Equal(condition.MatchingFunction(iteratable.ToList()), expectedResult);
        }
        [Theory]
        [InlineData(new object[] { Constants.AllRule, new bool[] { true, true }, true })]
        [InlineData(new object[] { Constants.AllRule, new bool[] { false, true }, false })]
        [InlineData(new object[] { Constants.AnyRule, new bool[] { true, false }, true })]
        [InlineData(new object[] { Constants.AnyRule, new bool[] { false, false }, false })]
        [InlineData(new object[] { Constants.NoneRule, new bool[] { true, true }, false })]
        public void TestSegmentRuleMatchingFunction(string rule, bool[] iteratable, bool expectedResult)
        {
            var condition = new SegmentRuleModel() { Type = rule };
            Assert.Equal(condition.MatchingFunction(iteratable.ToList()), expectedResult);
        }

    }
}
