// Runs every SDK benchmark in a single process, measuring only the benchmarked calls.
//
// Under the CodSpeed runner, each call is measured with instrument-hooks, so .NET start-up,
// JIT compilation and building the client stay out of the results. Elsewhere, it prints timings.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Flagsmith;
using Flagsmith.Benchmarks;

const int LocalRevolutions = 100;

var environmentsDir = Environment.GetEnvironmentVariable("SDK_BENCHMARKS_ENVIRONMENTS_DIR") ?? "";
if (environmentsDir == "")
{
    Console.Error.WriteLine("Usage: SDK_BENCHMARKS_ENVIRONMENTS_DIR=<dir> dotnet Flagsmith.Benchmarks.dll");
    return 2;
}

CodSpeed? codSpeed = null;
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CODSPEED_ENV")))
{
    codSpeed = new CodSpeed();
    if (!codSpeed.IsInstrumented)
    {
        Console.Error.WriteLine("CodSpeed: not running under instrumentation");
        return 1;
    }
    var version = typeof(FlagsmithClient).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
    var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
    var skippedObjects = new[] { Environment.ProcessPath!, Path.Combine(runtimeDir, "libcoreclr.so"), Path.Combine(runtimeDir, "libclrjit.so") };
    codSpeed.Setup("flagsmith-dotnet-client", version, skippedObjects.Where(File.Exists));
}

foreach (var environment in SdkBenchmark.Environments)
{
    var sdkBenchmark = new SdkBenchmark(environmentsDir, environment);
    foreach (var (name, run) in SdkBenchmark.Benchmarks)
    {
        Func<object?> benchmark = () => run(sdkBenchmark);
        benchmark(); // Warm up caches and compile the benchmarked code
        if (codSpeed != null)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            codSpeed.Measure($"Flagsmith.Benchmarks/SdkBenchmark.cs::{name}[{environment}]", benchmark);
        }
        else
        {
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < LocalRevolutions; i++) benchmark();
            Console.WriteLine(FormattableString.Invariant($"{$"{name}[{environment}]",-50} {stopwatch.Elapsed.TotalMilliseconds / LocalRevolutions,10:F3} ms"));
        }
    }
}

return 0;
