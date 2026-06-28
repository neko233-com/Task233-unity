# Task233 Unity

Task233 Unity is a Unity 2022+ async scheduling package focused on zero-GC hot paths, very low overhead main-thread continuations, frame/time delays, and repeatable performance testing.

It is designed to be benchmarked against UniTask and ETTask while staying useful as a standalone Unity Package Manager dependency.

## Install With Package Manager

Unity Package Manager is the recommended install path.

Open `Window > Package Manager > + > Add package from git URL...` and enter:

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

## Offline Install

For an offline Unity project, create this folder:

```text
Assets/neko233/Task233
```

Then copy these folders from this repository:

```text
Assets/neko233/Task233/Runtime
Assets/neko233/Task233/Editor
```

Do not copy `TestProject/` into `Assets`. The test project is only for this repository's automation and benchmark development.

`Runtime/` contains the player-safe scheduler and awaitables. `Editor/` contains only Unity Editor preview tooling and is excluded from player builds by the `Task233.Editor` asmdef.

## Unity

- Unity 2022.3 LTS or newer
- .NET Standard 2.1 compatible scripting runtime
- Performance tests use `com.unity.test-framework.performance`

## Compatibility

Task233 Runtime is written for Unity 2022+ and avoids worker-thread requirements, unsafe code, dynamic code generation, and reflection emit. The Unity package uses PlayerLoop for scheduling, so it targets Unity platforms first, including WebGL single-thread builds. The lightweight cancellation handle and awaitable API shape are kept plain C# friendly so a traditional .NET scheduler can be split out without changing the high-level API style.

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

## Editor Preview

Open:

```text
Tools > Task233 > Preview
```

The preview window can:

- Prewarm queues, delay nodes, and cancellation handles.
- Run an Editor allocation probe for warmed awaitable factory paths.
- Run a Play Mode sequence through `Yield`, `DelayFrames`, `DelayMilliseconds`, `DelaySeconds`, and `Post`.
- Cancel the Play Mode preview using `Task233CancelSource`.

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

- Zero GC hot paths for factory creation and pooled scheduling.
- No coroutine object for frame or time waits.
- Pooled scheduler nodes for frame, seconds, and milliseconds delays.
- PlayerLoop injection once per domain reload.
- Short benchmarks that can run locally and in GitHub Actions.
- WebGL-friendly single-threaded design with no worker thread dependency.
- Unity 2022+ platform-friendly runtime: no unsafe, no dynamic code generation, no worker thread requirement.

Call `T233.Prewarm()` during startup to reserve continuation queue, delay-node, and cancellation-source capacity. If a workload exceeds the warmed capacity, Task233 expands its arrays or node pool and that expansion can allocate.

## Performance Test Report

Last README report update: 2026-06-28.

The repository contains Unity Performance Testing benchmarks for Task233 plus an optional UniTask comparison assembly. The current benchmark style is intentionally short: no warmup, one measurement, large iteration counts, then total elapsed time is converted to `ns/op` and `ops/s`. The latest local Unity 2022.3.51f1 run completed 18 tests in 1.36 seconds.

Numeric CI results require a Unity license secret. Without `UNITY_LICENSE` or `UNITY_SERIAL`, the workflow validates configuration and skips the Unity editor invocation.

The measured Unity 2022.3.51f1 comparison table lives in [`性能报告.md`](性能报告.md).

| Case | Task233 ns/op | Comparison ns/op | Result |
| --- | ---: | ---: | --- |
| `T233.Yield()` factory vs `UniTask.Yield()` | 1.249 | 1.254 | Task233 slightly faster |
| `T233.DelayFrames(1)` factory vs `UniTask.DelayFrame(1)` | 11.769 | 214.967 | Task233 18.3x faster |
| `T233.DelaySeconds(0.001d)` factory vs `UniTask.Delay(TimeSpan)` | 12.769 | 252.724 | Task233 19.8x faster |
| `T233.DelayMilliseconds(1)` factory vs `UniTask.Delay(1)` | 12.141 | 258.307 | Task233 21.3x faster |
| `Task233CancelSource` vs `CancellationTokenSource` | 36.517 | 138.634 | Task233 3.8x faster |

Run the Editor preview allocation probe from `Tools > Task233 > Preview` for a quick local warmed-GC check. For authoritative speed results, run Unity Performance Testing in the target Unity version and hardware.

## Benchmarks

Open Unity Test Runner and run `Task233.PerformanceTests`, or run in batch mode:

```powershell
Unity.exe -batchmode -projectPath TestProject -runTests -testPlatform EditMode -testResults artifacts/editmode-results.xml
```

The baseline suite runs Task233 internal measurements. To compare UniTask, install UniTask and run the optional `Task233.UniTaskPerformanceTests` assembly. The asmdef enables itself through Unity Version Defines when `com.cysharp.unitask` is present.

To install UniTask into the test project for comparison, add the UniTask package to `TestProject/Packages/manifest.json`, then rerun the performance tests. The optional assembly is gated so normal users do not need UniTask installed.

## Docs

Static docs live in `docs/` and are published by GitHub Pages.
