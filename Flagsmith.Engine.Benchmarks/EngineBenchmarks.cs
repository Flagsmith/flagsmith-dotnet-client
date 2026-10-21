using BenchmarkDotNet.Attributes;
using FlagsmithEngine;

namespace Flagsmith.Engine.Benchmarks
{
    [MemoryDiagnoser]
    public class EngineBenchmarks
    {
        private readonly FlagsmithEngine.Engine _engine = new FlagsmithEngine.Engine();
        private EvaluationContext<object, object> _environmentContext;
        private EvaluationContext<object, object> _traitSegmentsContext;
        private EvaluationContext<object, object> _jsonPathSegmentsContext;
        private EvaluationContext<object, object> _flagDependencySegmentsContext;
        private EvaluationContext<object, object> _repeatedTraitSegmentsContext;
        private EvaluationContext<object, object>[] _engineTestDataCases;
        private EvaluationContext<object, object>[] _engineTestDataFlagDependencyCases;

        [GlobalSetup]
        public void Setup()
        {
            _environmentContext = Workloads.TraitSegments(withIdentity: false);
            _traitSegmentsContext = Workloads.TraitSegments(withIdentity: true);
            _jsonPathSegmentsContext = Workloads.JsonPathSegments();
            _flagDependencySegmentsContext = Workloads.FlagDependencySegments();
            _repeatedTraitSegmentsContext = Workloads.RepeatedTraitSegments();
            _engineTestDataCases = Workloads.EngineTestDataCases(flagDependency: false);
            _engineTestDataFlagDependencyCases = Workloads.EngineTestDataCases(flagDependency: true);
        }

        [Benchmark(Description = "Environment flags")]
        public object EnvironmentFlags() => _engine.GetEvaluationResult(_environmentContext);

        [Benchmark(Description = "Identity flags, trait segments")]
        public object TraitSegments() => _engine.GetEvaluationResult(_traitSegmentsContext);

        [Benchmark(Description = "Identity flags, JSONPath segments")]
        public object JsonPathSegments() => _engine.GetEvaluationResult(_jsonPathSegmentsContext);

        [Benchmark(Description = "Identity flags, segments depending on flags")]
        public object FlagDependencySegments() => _engine.GetEvaluationResult(_flagDependencySegmentsContext);

        [Benchmark(Description = "Identity flags, segments repeating trait rules")]
        public object RepeatedTraitSegments() => _engine.GetEvaluationResult(_repeatedTraitSegmentsContext);

        [Benchmark(Description = "engine-test-data, all but flag_dependency cases")]
        public int EngineTestDataCases() => EvaluateAll(_engineTestDataCases);

        [Benchmark(Description = "engine-test-data, flag_dependency cases")]
        public int EngineTestDataFlagDependencyCases() => EvaluateAll(_engineTestDataFlagDependencyCases);

        private int EvaluateAll(EvaluationContext<object, object>[] contexts)
        {
            var flagCount = 0;
            foreach (var context in contexts)
                flagCount += _engine.GetEvaluationResult(context).Flags.Count;
            return flagCount;
        }
    }
}
