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

## UniTask and ETTask comparison

The first benchmark layer measures Task233 primitives directly. To compare against UniTask and ETTask:

1. Add the target package to `TestProject/Packages/manifest.json`.
2. Add comparison tests under `Tests/Performance`.
3. Gate the tests with symbols such as `TASK233_HAS_UNITASK` and `TASK233_HAS_ETTASK`.

Keep each benchmark measuring the same operation:

- create awaitable
- schedule one continuation
- complete one frame delay
- run a large continuation batch

Report GC allocations and median execution time together. Fast code that allocates on hot paths is still a regression for Unity gameplay loops.
