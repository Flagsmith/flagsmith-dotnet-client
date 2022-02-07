using FlagsmithEngine.Segment;
using FlagsmithEngine.Segment.Models;
using System;
using System.Collections.Generic;
using System.Text;
namespace EngineTest.Unit.Segments
{
    public class fixtures
    {
        public static string TraitKey1 => "email";
        public static string TraitValue1 => "user@example.com";

        public static string TraitKey2 => "num_purchase";
        public static string TraitValue2 => "12";

        public static string TraitKey3 => "date_joined";
        public static string TraitValue3 => "2021-01-01";
        public static SegmentModel emptySegment => new SegmentModel() { Id = 1, Name = "empty_segment" };
        public static SegmentModel SegmentSingleCondition => new SegmentModel()
        {
            Id = 2,
            Name = "segment_one_condition",
            Rules = new List<SegmentRuleModel> {
                new SegmentRuleModel {
                    Type = Constants.AllRule,
                    Conditions = new List<SegmentConditionModel>
                    {
                        new SegmentConditionModel { Operator = Constants.Equal, Property = TraitKey1, Value = TraitValue1 }
                    }

                }
            }

        };
        public static SegmentModel SegmentMultipleConditionsAll => new SegmentModel()
        {
            Id = 3,
            Name = "segment_multiple_conditions_all",
            Rules = new List<SegmentRuleModel> {
                new SegmentRuleModel {
                    Type = Constants.AllRule,
                    Conditions = new List<SegmentConditionModel>
                    {
                        new SegmentConditionModel { Operator = Constants.Equal, Property = TraitKey1, Value = TraitValue1 },
                        new SegmentConditionModel { Operator = Constants.Equal, Property = TraitKey2, Value = TraitValue2 }
                    }

                }
            }

        };
        public static SegmentModel SegmentMultipleConditionsAny => new SegmentModel()
        {
            Id = 4,
            Name = "segment_multiple_conditions_any",
            Rules = new List<SegmentRuleModel> {
                new SegmentRuleModel {
                    Type = Constants.AnyRule,
                    Conditions = new List<SegmentConditionModel>
                    {
                        new SegmentConditionModel { Operator = Constants.Equal, Property = TraitKey1, Value = TraitValue1 },
                        new SegmentConditionModel { Operator = Constants.Equal, Property = TraitKey2, Value = TraitValue2 }
                    }

                }
            }

        };
        public static SegmentModel SegmentNestedRules => new SegmentModel()
        {
            Id = 5,
            Name = "segment_nested_rules_all",
            Rules = new List<SegmentRuleModel> {
                new SegmentRuleModel {
                    Type = Constants.AllRule,
                    Conditions = new List<SegmentConditionModel>
                    {
                        new SegmentConditionModel { Operator = Constants.Equal, Property = TraitKey1, Value = TraitValue1 },
                        new SegmentConditionModel { Operator = Constants.Equal, Property = TraitKey2, Value = TraitValue2 }
                    }

                },
                new SegmentRuleModel {
                    Type = Constants.AllRule,
                    Conditions = new List<SegmentConditionModel>
                    {
                        new SegmentConditionModel { Operator = Constants.Equal, Property = TraitKey3, Value = TraitValue3 },
                    }

                }
            }

        };
        public static SegmentModel SegmentConditionsAndNestedRules => new SegmentModel()
        {
            Id = 6,
            Name = "segment_multiple_conditions_all_and_nested_rules",
            Rules = new List<SegmentRuleModel> {
                new SegmentRuleModel {
                    Type = Constants.AllRule,
                    Conditions = new List<SegmentConditionModel>
                    {
                        new SegmentConditionModel { Operator = Constants.Equal, Property = TraitKey1, Value = TraitValue1 },
                    },
                    Rules = new List<SegmentRuleModel>
                    {
                        new SegmentRuleModel {
                            Type = Constants.AllRule,
                            Conditions=new List<SegmentConditionModel>{
                                 new SegmentConditionModel { Operator = Constants.Equal, Property = TraitKey2, Value = TraitValue2 }
                            }
                        },
                        new SegmentRuleModel {
                            Type = Constants.AllRule,
                            Conditions=new List<SegmentConditionModel>{
                                 new SegmentConditionModel { Operator = Constants.Equal, Property = TraitKey3, Value = TraitValue3 }
                            }
                        }
                    }

                },
            }

        };
    }
}
