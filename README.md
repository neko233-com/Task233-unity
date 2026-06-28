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
await T233.NextFrame();
await T233.DelayFrames(3);
await T233.DelaySeconds(1.5d);
await T233.DelayMilliseconds(250);

T233.Post(static () => Debug.Log("next Update"));
```

Time units are explicit in the method names. `DelaySeconds` and `DelayMilliseconds` use scaled Unity time by default. Pass `ignoreTimeScale: true` for unscaled time.

For low-diff UniTask / `Task.Delay` migration, Task233 also provides compatibility aliases:

```csharp
await T233.Delay(250);
await T233.Delay(TimeSpan.FromSeconds(1));
await T233.WaitForSeconds(1);
```

New code should prefer the explicit-unit names.

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

The repository contains Unity Performance Testing benchmarks for Task233 plus an optional UniTask comparison assembly. The current benchmark style is intentionally short and stable: no warmup, one measurement, large iteration counts, then total elapsed time is converted to `ns/op` and `ops/s`. The latest local Unity 2022.3.51f1 run completed 30 tests in 22.96 seconds, including async/await business-flow coverage.

Numeric CI results require a Unity license secret. Without `UNITY_LICENSE` or `UNITY_SERIAL`, the workflow validates configuration and skips the Unity editor invocation.

The measured Unity 2022.3.51f1 comparison table lives in [`性能报告.md`](性能报告.md).
That report now includes a full comparison matrix covering measured hot paths, API coverage, cancellation semantics, business patterns, platform constraints, and areas where UniTask is still broader.

| Case | Task233 ns/op | Comparison ns/op | Result |
| --- | ---: | ---: | --- |
| `T233.Yield()` factory vs `UniTask.Yield()` | 2.403 | 2.766 | Task233 1.15x faster |
| `T233.DelayFrames(1)` factory vs `UniTask.DelayFrame(1)` | 6.828 | 240.385 | Task233 35.2x faster |
| `T233.DelaySeconds(0.001d)` factory vs `UniTask.Delay(TimeSpan)` | 6.471 | 286.124 | Task233 44.2x faster |
| `T233.DelayMilliseconds(1)` factory vs `UniTask.Delay(1)` | 6.907 | 310.160 | Task233 44.9x faster |
| `Task233CancelSource` vs `CancellationTokenSource` | 17.689 | 159.556 | Task233 9.0x faster |

Async/await runtime tests cover frame waits, explicit seconds/milliseconds APIs, low-diff migration aliases, cancellation exceptions, FIFO post order, nested workflows, owner-destroy cancellation, debounce, and timeout cancellation.

Run the Editor preview allocation probe from `Tools > Task233 > Preview` for a quick local warmed-GC check. For authoritative speed results, run Unity Performance Testing in the target Unity version and hardware.

## Benchmarks

Open Unity Test Runner and run `Task233.PerformanceTests`, or run in batch mode:

```powershell
Unity.exe -batchmode -projectPath TestProject -runTests -testPlatform EditMode -testResults artifacts/editmode-results.xml
```

The baseline suite runs Task233 internal measurements. To compare UniTask without a UPM git dependency, download the UniTask source package first:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/PrepareUniTaskPackage.ps1 -Version 2.5.11
```

`TestProject/Packages/manifest.json` points to `.perf/UniTask` with a `file:` dependency. The optional `Task233.UniTaskPerformanceTests` assembly enables itself through Unity Version Defines when `com.cysharp.unitask` is present.

The optional assembly is gated so normal users do not need UniTask installed.

## Docs

Static docs live in `docs/` and are published by GitHub Pages.

Additional package docs:

- [`Documentation~/migration.md`](Documentation~/migration.md) for UniTask / `Task` migration.
- [`Documentation~/agent-integration.md`](Documentation~/agent-integration.md) for coding agents and automation tools.
- [`AGENTS.md`](AGENTS.md) for repository-level agent notes.
