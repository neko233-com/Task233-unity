# Benchmarking Task233

Task233 ships with a short benchmark suite under `Tests/Performance`.

## Local

Run the `Task233.PerformanceTests` assembly in the Unity Test Runner, or use batch mode from Unity 2022.3.51f1:

```powershell
Unity.exe -batchmode -projectPath TestProject -runTests -testPlatform EditMode -testResults artifacts/editmode-results.xml
```

## GitHub Actions

`.github/workflows/unity-performance.yml` creates an isolated Unity project and runs the same tests through GameCI. Add these repository secrets before expecting real Unity Editor benchmarks in CI:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

Without `UNITY_LICENSE` or `UNITY_SERIAL`, the workflow validates the benchmark project setup and skips the Unity invocation.

## UniTask comparison

The first benchmark layer measures Task233 primitives directly. To compare against UniTask:

1. Download UniTask into `.perf/UniTask`.
2. Run the optional `Task233.UniTaskPerformanceTests` assembly.

`Task233.UniTaskPerformanceTests.asmdef` uses Unity Version Defines and enables `TASK233_HAS_UNITASK` automatically when `com.cysharp.unitask` is present.

Local download command:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/PrepareUniTaskPackage.ps1 -Version 2.5.11
```

The repository test project uses `file:../../.perf/UniTask`, so Unity imports downloaded source code instead of cloning the UniTask git repository.

Keep each benchmark measuring the same operation:

- create awaitable
- schedule one continuation
- complete one frame delay
- run a large continuation batch

The default short-run benchmark policy is:

- `WarmupCount(0)`
- `MeasurementCount(1)`
- 20,000,000 iterations for awaitable factory paths
- 2,000,000 iterations for cancellation create/cancel/dispose
- report total milliseconds, derived `ns/op`, derived `ops/s`, and GC median together

Fast code that allocates on hot paths is still a regression for Unity gameplay loops.

## Runtime business coverage

`Tests/Runtime` contains deterministic EditMode tests for async/await behavior and common business flows. These tests use a test-only manual PlayerLoop pump so they do not depend on EditMode frame timing:

- `await T233.Yield()`
- `DelayFrames`, `DelaySeconds`, and `DelayMilliseconds`
- cancellation exceptions
- FIFO `Post`
- nested async workflows
- owner cancellation after UI/object lifetime ends
- debounce, where only the last request applies
- timeout cancellation for slow work

The cancellation tests intentionally cover `Cancel(); Dispose();` while work is still pending. Task233 retains cancellation handles held by scheduled work and releases them after the continuation observes cancellation.

## README report

When a numeric benchmark run is available, update the `Performance Test Report` table in `README.md` with:

- Unity version
- Scripting backend
- Platform
- Hardware or CI runner
- Median time per operation
- GC bytes per operation

Do not compare numbers from different Unity versions or machines as if they are equivalent.

## Zero-GC target

The factory hot paths are designed to allocate zero GC without a user warmup call. For queued work, use `T233.Prewarm(continuationCapacityPerTiming, delayNodeCapacityPerTiming, cancelSourceCapacity)` before hot gameplay begins if you know the expected queue and delay-node capacity. The warmed scheduling path is designed to avoid scheduler allocations for:

- `T233.Post` with cached or static delegates
- `T233.Yield`
- `T233.DelayFrames`
- `T233.DelaySeconds`
- `T233.DelayMilliseconds`
- `Task233CancelSource` create/cancel/dispose reuse

Async methods can still allocate because of C# state-machine behavior and captured delegates. For the tightest loops, prefer `T233.Post(staticAction)` or cache continuations explicitly.
