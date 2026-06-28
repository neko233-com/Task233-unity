# Agent Integration Guide

This file is written for coding agents and automation tools that need to edit or use Task233 safely.

## Package Shape

| Path | Purpose |
| --- | --- |
| `Runtime/` | Player-safe Task233 runtime. No Editor-only API here. |
| `Editor/` | Editor preview window. Must stay editor-only. |
| `Tests/Runtime/` | Deterministic async/await and business-flow tests. |
| `Tests/Performance/` | Unity Performance Testing benchmarks. |
| `Documentation~/` | UPM documentation files. |
| `docs/` | GitHub Pages static HTML. |
| `TestProject/` | Local/CI test host project. Do not copy it into a Unity game's `Assets`. |

## API Selection Rules

Prefer explicit-unit APIs in new code:

```csharp
await T233.DelayFrames(3);
await T233.DelaySeconds(1.5d);
await T233.DelayMilliseconds(250);
```

Use migration aliases only when lowering diff size matters:

```csharp
await T233.NextFrame();
await T233.Delay(250);
await T233.Delay(TimeSpan.FromSeconds(1));
await T233.WaitForSeconds(1);
```

Use `Task233CancelSource` instead of `CancellationTokenSource` for hot Unity waits:

```csharp
var cancel = T233.CreateCancelSource();
try
{
    await T233.DelayFrames(10, cancellation: cancel);
}
catch (OperationCanceledException)
{
}
finally
{
    cancel.Dispose();
}
```

It is valid to call `cancel.Cancel(); cancel.Dispose();` while work is pending.

## Performance-Safe Patterns

- Use static or cached `Action` delegates with `T233.Post` in hot loops.
- Call `T233.Prewarm(...)` before known high-volume scheduling.
- Avoid captured lambdas in per-frame hot paths.
- Keep cancellation sources owner-scoped and dispose them.
- Do not introduce worker-thread dependencies into `Runtime/`.
- Do not add `unsafe`, reflection emit, or dynamic code generation.

## Test Commands

Prepare UniTask source package without UPM git clone:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/PrepareUniTaskPackage.ps1 -Version 2.5.11
```

Run full EditMode tests in Unity 2022.3.51f1:

```powershell
"C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Unity.exe" -batchmode -nographics -projectPath TestProject -runTests -testPlatform EditMode -testResults artifacts\editmode-results.xml
```

Run only runtime/business tests:

```powershell
"C:\Program Files\Unity\Hub\Editor\2022.3.51f1\Editor\Unity.exe" -batchmode -nographics -projectPath TestProject -runTests -testPlatform EditMode -assemblyNames Task233.RuntimeTests -testResults artifacts\runtime-results.xml
```

## Documentation Updates

When changing APIs or benchmark numbers, update all of these:

| File | Update when |
| --- | --- |
| `README.md` | Install, quick start, API summary, benchmark summary changes |
| `性能报告.md` | Any measured benchmark result or comparison claim changes |
| `docs/index.html` | GitHub Pages user-facing docs change |
| `Documentation~/migration.md` | UniTask / Task migration surface changes |
| `Documentation~/agent-integration.md` | Agent workflow or repo structure changes |

## CI / Release Notes

GitHub Actions creates an isolated `.perf/TestProject` and downloads UniTask source with `curl` from the `2.5.11` tag. If Unity license secrets are missing, the workflow validates setup and skips the real Unity editor run.

After pushing, check:

```powershell
gh run list --limit 5
```

Expected workflows:

- `Unity Performance Tests`
- `GitHub Pages`
