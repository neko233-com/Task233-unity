# Migration Guide

Task233 keeps the common UniTask / `Task.Delay` mental model: `await` a small value, continue on Unity main thread, and pass an owner-scoped cancellation handle when the work should stop.

## Imports

| From | To |
| --- | --- |
| `using Cysharp.Threading.Tasks;` | `using Task233;` |
| `CancellationTokenSource` for hot gameplay waits | `Task233CancelSource` |
| `CancellationToken` parameters | `Task233CancelSource cancellation = default` |

## Common Replacements

| Existing code | Task233 low-edit version | Preferred explicit-unit version |
| --- | --- | --- |
| `await UniTask.Yield();` | `await T233.Yield();` | same |
| `await UniTask.NextFrame();` | `await T233.NextFrame();` | `await T233.DelayFrames(1);` |
| `await UniTask.DelayFrame(3);` | `await T233.DelayFrame(3);` | `await T233.DelayFrames(3);` |
| `await UniTask.Delay(250);` | `await T233.Delay(250);` | `await T233.DelayMilliseconds(250);` |
| `await UniTask.Delay(TimeSpan.FromSeconds(1));` | `await T233.Delay(TimeSpan.FromSeconds(1));` | `await T233.DelaySeconds(1);` |
| `await UniTask.WaitForSeconds(1);` | `await T233.WaitForSeconds(1);` | `await T233.DelaySeconds(1);` |
| `cts.Cancel(); cts.Dispose();` | `cancel.Cancel(); cancel.Dispose();` | same |

The compatibility aliases exist to lower migration cost. New Task233 code should prefer the explicit-unit APIs because they are harder to misread in reviews and agent-generated patches.

## Cancellation Pattern

```csharp
using Task233;

private Task233CancelSource loadCancel;

private async void OnEnable()
{
    loadCancel = T233.CreateCancelSource();

    try
    {
        await T233.DelayMilliseconds(250, cancellation: loadCancel);
        // update UI here
    }
    catch (OperationCanceledException)
    {
    }
}

private void OnDisable()
{
    loadCancel.Cancel();
    loadCancel.Dispose();
}
```

`Cancel(); Dispose();` is safe while awaited work is still pending. Task233 retains scheduled cancellation handles until the continuation observes cancellation, then releases the internal slot.

## From `Task.Delay`

```csharp
// before
await System.Threading.Tasks.Task.Delay(250);

// low-edit migration
await T233.Delay(250);

// preferred
await T233.DelayMilliseconds(250);
```

Task233 delays resume on Unity's PlayerLoop main thread. It is not a background-thread scheduler and does not replace CPU-bound `Task.Run`.

## From UniTask Timeout / Debounce

Task233 intentionally keeps timeout and debounce as small owner-level patterns instead of large helper APIs.

```csharp
var current = T233.CreateCancelSource();

async void Submit(string query)
{
    current.Cancel();
    current.Dispose();
    current = T233.CreateCancelSource();

    try
    {
        await T233.DelayMilliseconds(150, cancellation: current);
        // run search for query
    }
    catch (OperationCanceledException)
    {
    }
}
```

## What Does Not Map Yet

| UniTask feature | Task233 status |
| --- | --- |
| `WhenAll`, `WhenAny` | Not implemented yet |
| `Forget()` | Not implemented yet |
| async LINQ / `IUniTaskAsyncEnumerable` | Not a current goal |
| MonoBehaviour/UI/TextMeshPro triggers | Not implemented yet |
| coroutine bridge | Not implemented yet |
| ThreadPool helpers | Not a Task233 runtime goal, especially for WebGL |

Keep UniTask installed for these ecosystem features, or migrate only the hot gameplay wait/cancel paths to Task233 first.
