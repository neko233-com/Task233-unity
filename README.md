# Task233 Unity

Task233 Unity is a Unity 2022+ async scheduling package focused on very low overhead main-thread continuations, frame delays, and repeatable performance testing.

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
        await T233.DelayFrame(3);
        Debug.Log("continued on Unity main thread");
    }
}
```

## Performance Goals

- No coroutine object for frame waits.
- Pooled scheduler nodes for frame and time delays.
- PlayerLoop injection once per domain reload.
- Benchmarks that can run locally and in GitHub Actions.

## Benchmarks

Open Unity Test Runner and run `Task233.PerformanceTests`, or run in batch mode:

```powershell
Unity.exe -batchmode -projectPath TestProject -runTests -testPlatform EditMode -testResults artifacts/editmode-results.xml
```

The baseline suite runs Task233 internal measurements. To add UniTask or ETTask comparisons, install those packages and enable `TASK233_HAS_UNITASK` or `TASK233_HAS_ETTASK` in Player Settings.

## Docs

Static docs live in `docs/` and are published by GitHub Pages.
