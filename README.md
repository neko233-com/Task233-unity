# Task233 Unity

Task233 Unity is a Unity 2022+ async scheduling package focused on zero-GC hot paths, very low overhead main-thread continuations, frame/time delays, and repeatable performance testing.

It is designed to be benchmarked against UniTask and ETTask while staying useful as a standalone Unity Package Manager dependency.

## Install

Use Unity Package Manager:

```text
https://github.com/neko233-com/Task233-unity.git
```

Or add it to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.neko233.task233": "https://github.com/neko233-com/Task233-unity.git"
  }
}
```

## Unity

- Unity 2022.3 LTS or newer
- .NET Standard 2.1 compatible scripting runtime
- Performance tests use `com.unity.test-framework.performance`

## Quick Start

```csharp
using Task233;
using UnityEngine;

public sealed class Example : MonoBehaviour
{
    private async void Start()
    {
        await T233.Yield();
        await T233.DelayFrames(3);
        await T233.DelaySeconds(0.25d);
        await T233.DelayMilliseconds(16);
        Debug.Log("continued on Unity main thread");
    }
}
```

## API

```csharp
await T233.Yield();
await T233.DelayFrames(3);
await T233.DelaySeconds(1.5d);
await T233.DelayMilliseconds(250);

T233.Post(static () => Debug.Log("next Update"));
```

Time units are explicit in the method names. `DelaySeconds` and `DelayMilliseconds` use scaled Unity time by default. Pass `ignoreTimeScale: true` for unscaled time.

## Cancellation

Task233 has a lightweight cancellation handle so hot paths do not have to traffic in `CancellationToken`.

```csharp
var cancel = T233.CreateCancelSource();

try
{
    await T233.DelaySeconds(3, cancellation: cancel);
}
catch (OperationCanceledException)
{
}
finally
{
    cancel.Dispose();
}
```

Call `cancel.Cancel()` from the owner that wants to stop the wait, such as `OnDisable`, a UI button, or a timeout controller.

`Task233CancelSource` is a struct handle backed by a single-threaded table. Create and dispose it on the Unity main thread.

## Performance Goals

- Zero GC on warmed hot paths.
- No coroutine object for frame or time waits.
- Pooled scheduler nodes for frame, seconds, and milliseconds delays.
- PlayerLoop injection once per domain reload.
- Benchmarks that can run locally and in GitHub Actions.
- WebGL-friendly single-threaded design with no worker thread dependency.

Call `T233.Prewarm()` during startup to reserve continuation queue, delay-node, and cancellation-source capacity. If a workload exceeds the warmed capacity, Task233 expands its arrays or node pool and that expansion can allocate.

## Benchmarks

Open Unity Test Runner and run `Task233.PerformanceTests`, or run in batch mode:

```powershell
Unity.exe -batchmode -projectPath TestProject -runTests -testPlatform EditMode -testResults artifacts/editmode-results.xml
```

The baseline suite runs Task233 internal measurements. To compare UniTask, install UniTask, add `TASK233_HAS_UNITASK` in Player Settings, and run the optional `Task233.UniTaskPerformanceTests` assembly.

## Docs

Static docs live in `docs/` and are published by GitHub Pages.
