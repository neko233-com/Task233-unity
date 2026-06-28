using System;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Task233
{
    internal static class Task233PlayerLoop
    {
        private static readonly ContinuationQueue[] Queues = CreateQueues();
        private static bool initialized;

        public static void Reset()
        {
            initialized = false;
            Task233Cancellation.Reset();
            for (var i = 0; i < Queues.Length; i++)
            {
                Queues[i].Clear();
            }
        }

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            var loop = PlayerLoop.GetCurrentPlayerLoop();
            InsertRunner<Initialization>(ref loop, PlayerLoopTiming.Initialization);
            InsertRunner<EarlyUpdate>(ref loop, PlayerLoopTiming.EarlyUpdate);
            InsertRunner<FixedUpdate>(ref loop, PlayerLoopTiming.FixedUpdate);
            InsertRunner<PreUpdate>(ref loop, PlayerLoopTiming.PreUpdate);
            InsertRunner<Update>(ref loop, PlayerLoopTiming.Update);
            InsertRunner<PreLateUpdate>(ref loop, PlayerLoopTiming.PreLateUpdate);
            InsertRunner<PostLateUpdate>(ref loop, PlayerLoopTiming.PostLateUpdate);
            PlayerLoop.SetPlayerLoop(loop);
            initialized = true;
        }

        public static void Enqueue(PlayerLoopTiming timing, Action continuation)
        {
            Initialize();
            Queues[(int)timing].Enqueue(continuation);
        }

        public static void Enqueue(PlayerLoopTiming timing, Action continuation, Task233CancelSource cancellation)
        {
            Initialize();
            Queues[(int)timing].Enqueue(continuation, cancellation);
        }

        public static void Enqueue(PlayerLoopTiming timing, Action continuation, Task233CancelSource cancellation, bool skipIfCanceled)
        {
            Initialize();
            Queues[(int)timing].Enqueue(continuation, cancellation, skipIfCanceled);
        }

        public static void EnqueueDelayFrame(PlayerLoopTiming timing, int frameCount, Action continuation, Task233CancelSource cancellation)
        {
            Initialize();
            Queues[(int)timing].EnqueueDelayFrame(frameCount, continuation, cancellation);
        }

        public static void EnqueueDelaySeconds(PlayerLoopTiming timing, double seconds, bool ignoreTimeScale, Action continuation, Task233CancelSource cancellation)
        {
            Initialize();
            Queues[(int)timing].EnqueueDelaySeconds(seconds, ignoreTimeScale, continuation, cancellation);
        }

        public static void Prewarm(int continuationCapacityPerTiming, int delayNodeCapacityPerTiming)
        {
            Initialize();
            for (var i = 0; i < Queues.Length; i++)
            {
                Queues[i].Prewarm(continuationCapacityPerTiming, delayNodeCapacityPerTiming);
            }
        }

        private static ContinuationQueue[] CreateQueues()
        {
            var values = (PlayerLoopTiming[])Enum.GetValues(typeof(PlayerLoopTiming));
            var queues = new ContinuationQueue[values.Length];
            for (var i = 0; i < queues.Length; i++)
            {
                queues[i] = new ContinuationQueue();
            }

            return queues;
        }

        private static void Run(PlayerLoopTiming timing)
        {
            Queues[(int)timing].Run();
        }

        private static void InsertRunner<TPlayerLoop>(ref PlayerLoopSystem root, PlayerLoopTiming timing)
        {
            var runnerType = typeof(Task233PlayerLoopRunnerMarker<TPlayerLoop>);
            RemoveRunner(ref root, runnerType);

            var runner = new PlayerLoopSystem
            {
                type = runnerType,
                updateDelegate = () => Run(timing)
            };

            if (!InsertBefore(ref root, typeof(TPlayerLoop), runner))
            {
                AppendToRoot(ref root, runner);
            }
        }

        private static bool InsertBefore(ref PlayerLoopSystem system, Type targetType, PlayerLoopSystem runner)
        {
            var subs = system.subSystemList;
            if (subs == null)
            {
                return false;
            }

            for (var i = 0; i < subs.Length; i++)
            {
                if (subs[i].type == targetType)
                {
                    var next = new PlayerLoopSystem[subs.Length + 1];
                    Array.Copy(subs, 0, next, 0, i);
                    next[i] = runner;
                    Array.Copy(subs, i, next, i + 1, subs.Length - i);
                    system.subSystemList = next;
                    return true;
                }

                if (InsertBefore(ref subs[i], targetType, runner))
                {
                    system.subSystemList = subs;
                    return true;
                }
            }

            return false;
        }

        private static void RemoveRunner(ref PlayerLoopSystem system, Type runnerType)
        {
            var subs = system.subSystemList;
            if (subs == null)
            {
                return;
            }

            var count = 0;
            for (var i = 0; i < subs.Length; i++)
            {
                RemoveRunner(ref subs[i], runnerType);
                if (subs[i].type != runnerType)
                {
                    count++;
                }
            }

            if (count == subs.Length)
            {
                system.subSystemList = subs;
                return;
            }

            var next = new PlayerLoopSystem[count];
            var write = 0;
            for (var i = 0; i < subs.Length; i++)
            {
                if (subs[i].type != runnerType)
                {
                    next[write++] = subs[i];
                }
            }

            system.subSystemList = next;
        }

        private static void AppendToRoot(ref PlayerLoopSystem root, PlayerLoopSystem runner)
        {
            var subs = root.subSystemList;
            if (subs == null)
            {
                root.subSystemList = new[] { runner };
                return;
            }

            var next = new PlayerLoopSystem[subs.Length + 1];
            Array.Copy(subs, next, subs.Length);
            next[subs.Length] = runner;
            root.subSystemList = next;
        }

        private sealed class Task233PlayerLoopRunnerMarker<T>
        {
        }
    }
}
