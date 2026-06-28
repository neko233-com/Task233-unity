using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Task233
{
    public static class T233
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Task233PlayerLoop.Reset();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Task233PlayerLoop.Initialize();
        }

        public static Task233CancelSource CreateCancelSource()
        {
            return Task233Cancellation.Create();
        }

        public static void Prewarm(int continuationCapacityPerTiming = 1024, int delayNodeCapacityPerTiming = 1024, int cancelSourceCapacity = 1024)
        {
            if (continuationCapacityPerTiming < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(continuationCapacityPerTiming));
            }

            if (delayNodeCapacityPerTiming < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delayNodeCapacityPerTiming));
            }

            if (cancelSourceCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cancelSourceCapacity));
            }

            Task233Cancellation.Prewarm(cancelSourceCapacity);
            Task233PlayerLoop.Prewarm(continuationCapacityPerTiming, delayNodeCapacityPerTiming);
        }

        public static YieldAwaitable Yield(PlayerLoopTiming timing = PlayerLoopTiming.Update, Task233CancelSource cancellation = default)
        {
            Task233PlayerLoop.Initialize();
            return new YieldAwaitable(timing, cancellation);
        }

        public static void Post(Action continuation, PlayerLoopTiming timing = PlayerLoopTiming.Update, Task233CancelSource cancellation = default)
        {
            Task233PlayerLoop.Initialize();
            Task233PlayerLoop.Enqueue(timing, continuation, cancellation, true);
        }

        public static DelayAwaitable DelayFrame(int frameCount, PlayerLoopTiming timing = PlayerLoopTiming.Update, Task233CancelSource cancellation = default)
        {
            return DelayFrames(frameCount, timing, cancellation);
        }

        public static DelayAwaitable DelayFrames(int frameCount, PlayerLoopTiming timing = PlayerLoopTiming.Update, Task233CancelSource cancellation = default)
        {
            if (frameCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameCount));
            }

            Task233PlayerLoop.Initialize();
            return DelayAwaitable.Frames(frameCount, timing, cancellation);
        }

        public static DelayAwaitable DelaySeconds(double seconds, PlayerLoopTiming timing = PlayerLoopTiming.Update, Task233CancelSource cancellation = default, bool ignoreTimeScale = false)
        {
            if (double.IsNaN(seconds) || seconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds));
            }

            Task233PlayerLoop.Initialize();
            return DelayAwaitable.Seconds(seconds, ignoreTimeScale, timing, cancellation);
        }

        public static DelayAwaitable DelayMilliseconds(int milliseconds, PlayerLoopTiming timing = PlayerLoopTiming.Update, Task233CancelSource cancellation = default, bool ignoreTimeScale = false)
        {
            if (milliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(milliseconds));
            }

            Task233PlayerLoop.Initialize();
            return DelayAwaitable.Seconds(milliseconds / 1000d, ignoreTimeScale, timing, cancellation);
        }

        public readonly struct YieldAwaitable
        {
            private readonly PlayerLoopTiming timing;
            private readonly Task233CancelSource cancellation;

            internal YieldAwaitable(PlayerLoopTiming timing, Task233CancelSource cancellation)
            {
                this.timing = timing;
                this.cancellation = cancellation;
            }

            public Awaiter GetAwaiter()
            {
                return new Awaiter(timing, cancellation);
            }

            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private readonly PlayerLoopTiming timing;
                private readonly Task233CancelSource cancellation;

                internal Awaiter(PlayerLoopTiming timing, Task233CancelSource cancellation)
                {
                    this.timing = timing;
                    this.cancellation = cancellation;
                }

                public bool IsCompleted => cancellation.IsCancellationRequested;

                public void GetResult()
                {
                    cancellation.ThrowIfCancellationRequested();
                }

                public void OnCompleted(Action continuation)
                {
                    Task233PlayerLoop.Enqueue(timing, continuation, cancellation);
                }

                public void UnsafeOnCompleted(Action continuation)
                {
                    Task233PlayerLoop.Enqueue(timing, continuation, cancellation);
                }
            }
        }

        public readonly struct DelayAwaitable
        {
            private readonly DelayKind kind;
            private readonly int frames;
            private readonly double seconds;
            private readonly bool ignoreTimeScale;
            private readonly PlayerLoopTiming timing;
            private readonly Task233CancelSource cancellation;

            private DelayAwaitable(DelayKind kind, int frames, double seconds, bool ignoreTimeScale, PlayerLoopTiming timing, Task233CancelSource cancellation)
            {
                this.kind = kind;
                this.frames = frames;
                this.seconds = seconds;
                this.ignoreTimeScale = ignoreTimeScale;
                this.timing = timing;
                this.cancellation = cancellation;
            }

            internal static DelayAwaitable Frames(int frames, PlayerLoopTiming timing, Task233CancelSource cancellation)
            {
                return new DelayAwaitable(DelayKind.Frames, frames, 0d, false, timing, cancellation);
            }

            internal static DelayAwaitable Seconds(double seconds, bool ignoreTimeScale, PlayerLoopTiming timing, Task233CancelSource cancellation)
            {
                return new DelayAwaitable(DelayKind.Seconds, 0, seconds, ignoreTimeScale, timing, cancellation);
            }

            public Awaiter GetAwaiter()
            {
                return new Awaiter(kind, frames, seconds, ignoreTimeScale, timing, cancellation);
            }

            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private readonly DelayKind kind;
                private readonly int frames;
                private readonly double seconds;
                private readonly bool ignoreTimeScale;
                private readonly PlayerLoopTiming timing;
                private readonly Task233CancelSource cancellation;

                internal Awaiter(DelayKind kind, int frames, double seconds, bool ignoreTimeScale, PlayerLoopTiming timing, Task233CancelSource cancellation)
                {
                    this.kind = kind;
                    this.frames = frames;
                    this.seconds = seconds;
                    this.ignoreTimeScale = ignoreTimeScale;
                    this.timing = timing;
                    this.cancellation = cancellation;
                }

                public bool IsCompleted => cancellation.IsCancellationRequested || (kind == DelayKind.Frames ? frames == 0 : seconds <= 0d);

                public void GetResult()
                {
                    cancellation.ThrowIfCancellationRequested();
                }

                public void OnCompleted(Action continuation)
                {
                    Schedule(continuation);
                }

                public void UnsafeOnCompleted(Action continuation)
                {
                    Schedule(continuation);
                }

                private void Schedule(Action continuation)
                {
                    if (kind == DelayKind.Frames)
                    {
                        Task233PlayerLoop.EnqueueDelayFrame(timing, frames, continuation, cancellation);
                    }
                    else
                    {
                        Task233PlayerLoop.EnqueueDelaySeconds(timing, seconds, ignoreTimeScale, continuation, cancellation);
                    }
                }
            }
        }

        private enum DelayKind
        {
            Frames,
            Seconds
        }
    }
}
