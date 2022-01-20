﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Flagsmith_engine.Segment
{
    internal static class Constants
    {
        public const string AllRule = "ALL";
        public const string AnyRule = "ANY";
        public const string NoneRule = "NONE";

        public const string Equal = "EQUAL";
        public const string GreaterThan = "GREATER_THAN";
        public const string LessThan = "LESS_THAN";
        public const string LessThanInclusive = "LESS_THAN_INCLUSIVE";
        public const string Contains = "CONTAINS";
        public const string GreaterThanInclusive = "GREATER_THAN_INCLUSIVE";
        public const string NotContains = "NOT_CONTAINS";
        public const string NotEqual = "NOT_EQUAL";
        public const string Regex = "REGEX";
        public const string PercentageSplit = "PERCENTAGE_SPLIT";
    }
}
