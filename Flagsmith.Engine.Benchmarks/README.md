# Engine benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org/) benchmarks of `Engine.GetEvaluationResult`, over synthetic environments
(see `Workloads.cs`) and the engine-test-data cases. Requires the .NET 10 SDK and the `Flagsmith.EngineTest/EngineTestData`
submodule.

```sh
cd Flagsmith.Engine.Benchmarks
dotnet run -c Release -- --filter '*'
```

The workloads only use the public evaluation context model, so they also run against older engine revisions. To compare
against one, check it out in a worktree that shares this checkout's test data, and run the same command from there:

```sh
git worktree add --detach ../baseline <commit>
rm -rf ../baseline/Flagsmith.EngineTest/EngineTestData
ln -s "$PWD/Flagsmith.EngineTest/EngineTestData" ../baseline/Flagsmith.EngineTest/EngineTestData
cp -r Flagsmith.Engine.Benchmarks ../baseline/
```
