using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Flagsmith.Benchmarks
{
    /// <summary>
    /// .NET bindings for CodSpeed's instrument-hooks, measuring a delegate in-process.
    /// </summary>
    public sealed class CodSpeed
    {
        private const string InstrumentHooks = "instrument_hooks";
        private const ulong FeatureDisableCallgrindMarkers = 0;

        private readonly IntPtr _hooks;

        public CodSpeed()
        {
            _hooks = instrument_hooks_init();
            if (_hooks == IntPtr.Zero) throw new InvalidOperationException("CodSpeed: failed to initialise instrument-hooks");
            instrument_hooks_set_feature(FeatureDisableCallgrindMarkers, true);
            // Compile the measuring code ahead, so the first measurement doesn't count its JIT compilation.
            Marshal.PrelinkAll(typeof(CodSpeed));
            RuntimeHelpers.PrepareMethod(((Func<Func<object?>, object?>)__codspeed_root_frame__Measure).Method.MethodHandle);
        }

        /// <summary>
        /// Whether the process runs under the CodSpeed runner.
        /// </summary>
        public bool IsInstrumented => instrument_hooks_is_instrumented(_hooks);

        /// <summary>
        /// Register the harness with the runner.
        /// </summary>
        public void Setup(string integration, string version, IEnumerable<string> skippedObjects)
        {
            if (instrument_hooks_set_integration(_hooks, integration, version) != 0)
                Console.Error.WriteLine("CodSpeed: failed to register the integration");
            // Attribute runtime costs to the root frame, as pytest-codspeed does for libpython.
            foreach (var path in skippedObjects) instrument_hooks_callgrind_add_obj_skip(path);
        }

        /// <summary>
        /// Measure the benchmark once as the benchmark at uri.
        /// </summary>
        public void Measure(string uri, Func<object?> benchmark)
        {
            if (instrument_hooks_start_benchmark(_hooks) != 0)
                throw new InvalidOperationException("CodSpeed: failed to start the benchmark");
            // Toggle callgrind here, rather than in start_benchmark, to measure as little harness as possible.
            callgrind_start_instrumentation();
            try
            {
                __codspeed_root_frame__Measure(benchmark);
            }
            finally
            {
                callgrind_stop_instrumentation();
                if (instrument_hooks_stop_benchmark(_hooks) != 0)
                    Console.Error.WriteLine("CodSpeed: failed to stop the benchmark");
                if (instrument_hooks_set_executed_benchmark(_hooks, Environment.ProcessId, uri) != 0)
                    Console.Error.WriteLine("CodSpeed: failed to report the executed benchmark");
            }
        }

        // Root frame of the benchmark flame graph: everything measured runs inside it.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static object? __codspeed_root_frame__Measure(Func<object?> benchmark) => benchmark();

        [DllImport(InstrumentHooks)]
        private static extern IntPtr instrument_hooks_init();

        [DllImport(InstrumentHooks)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool instrument_hooks_is_instrumented(IntPtr hooks);

        [DllImport(InstrumentHooks)]
        private static extern void instrument_hooks_set_feature(ulong feature, [MarshalAs(UnmanagedType.U1)] bool enabled);

        [DllImport(InstrumentHooks)]
        private static extern byte instrument_hooks_set_integration(IntPtr hooks, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string version);

        [DllImport(InstrumentHooks)]
        private static extern byte instrument_hooks_callgrind_add_obj_skip([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

        [DllImport(InstrumentHooks)]
        private static extern byte instrument_hooks_start_benchmark(IntPtr hooks);

        [DllImport(InstrumentHooks)]
        private static extern byte instrument_hooks_stop_benchmark(IntPtr hooks);

        [DllImport(InstrumentHooks)]
        private static extern byte instrument_hooks_set_executed_benchmark(IntPtr hooks, int pid, [MarshalAs(UnmanagedType.LPUTF8Str)] string uri);

        [DllImport(InstrumentHooks)]
        private static extern void callgrind_start_instrumentation();

        [DllImport(InstrumentHooks)]
        private static extern void callgrind_stop_instrumentation();
    }
}
