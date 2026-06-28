# Agent Notes

Task233 is a Unity 2022+ async scheduling package focused on low-GC PlayerLoop waits and cancellation.

Read these before editing:

- `Documentation~/agent-integration.md`
- `Documentation~/migration.md`
- `性能报告.md`

Key rules:

- Keep `Runtime/` player-safe. Do not use UnityEditor APIs there.
- Prefer explicit-unit APIs in examples: `DelayFrames`, `DelaySeconds`, `DelayMilliseconds`.
- Migration aliases exist for low-diff ports: `NextFrame`, `Delay(int)`, `Delay(TimeSpan)`, `WaitForSeconds`.
- Use `Task233CancelSource` for hot Unity waits.
- `Cancel(); Dispose();` must remain safe while work is pending.
- Do not introduce worker-thread requirements, unsafe code, reflection emit, or dynamic code generation.
- If benchmark numbers change, update `README.md`, `性能报告.md`, and `docs/index.html` together.
- Do not commit `.perf/`, `Library/`, `Temp/`, `artifacts/`, or Unity generated project output.
