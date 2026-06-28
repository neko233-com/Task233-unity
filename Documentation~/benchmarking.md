# Benchmarking Task233

Task233 ships with a minimal benchmark suite under `Tests/Performance`.

## Local

Run the `Task233.PerformanceTests` assembly in the Unity Test Runner, or use batch mode from a Unity 2022.3 editor:

```powershell
Unity.exe -batchmode -projectPath TestProject -runTests -testPlatform EditMode -testResults artifacts/editmode-results.xml
```

## GitHub Actions

`.github/workflows/unity-performance.yml` runs the same tests through GameCI. Add these repository secrets before expecting CI to pass:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

## UniTask comparison

The first benchmark layer measures Task233 primitives directly. To compare against UniTask:

1. Add UniTask to `TestProject/Packages/manifest.json`.
2. Add `TASK233_HAS_UNITASK` to the Unity scripting define symbols.
3. Run the optional `Task233.UniTaskPerformanceTests` assembly.

Keep each benchmark measuring the same operation:

- create awaitable
- schedule one continuation
- complete one frame delay
- run a large continuation batch

Report GC allocations and median execution time together. Fast code that allocates on hot paths is still a regression for Unity gameplay loops.

## Zero-GC target

Use `T233.Prewarm(continuationCapacityPerTiming, delayNodeCapacityPerTiming, cancelSourceCapacity)` before hot gameplay begins. The warmed path is designed to avoid scheduler allocations for:

- `T233.Post` with cached or static delegates
- `T233.Yield`
- `T233.DelayFrames`
- `T233.DelaySeconds`
- `T233.DelayMilliseconds`
- `Task233CancelSource` create/cancel/dispose reuse

Async methods can still allocate because of C# state-machine behavior and captured delegates. For the tightest loops, prefer `T233.Post(staticAction)` or cache continuations explicitly.
