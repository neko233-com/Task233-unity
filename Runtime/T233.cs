using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

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

        public static YieldAwaitable Yield(PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            Task233PlayerLoop.Initialize();
            return new YieldAwaitable(timing);
        }

        public static DelayFrameAwaitable DelayFrame(int frameCount, PlayerLoopTiming timing = PlayerLoopTiming.Update)
        {
            if (frameCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameCount));
            }

            Task233PlayerLoop.Initialize();
            return new DelayFrameAwaitable(frameCount, timing);
        }

        public readonly struct YieldAwaitable
        {
            private readonly PlayerLoopTiming timing;

            internal YieldAwaitable(PlayerLoopTiming timing)
            {
                this.timing = timing;
            }

            public Awaiter GetAwaiter()
            {
                return new Awaiter(timing);
            }

            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private readonly PlayerLoopTiming timing;

                internal Awaiter(PlayerLoopTiming timing)
                {
                    this.timing = timing;
                }

                public bool IsCompleted => false;

                public void GetResult()
                {
                }

                public void OnCompleted(Action continuation)
                {
                    Task233PlayerLoop.Enqueue(timing, continuation);
                }

                public void UnsafeOnCompleted(Action continuation)
                {
                    Task233PlayerLoop.Enqueue(timing, continuation);
                }
            }
        }

        public readonly struct DelayFrameAwaitable
        {
            private readonly int frameCount;
            private readonly PlayerLoopTiming timing;

            internal DelayFrameAwaitable(int frameCount, PlayerLoopTiming timing)
            {
                this.frameCount = frameCount;
                this.timing = timing;
            }

            public Awaiter GetAwaiter()
            {
                return new Awaiter(frameCount, timing);
            }

            public readonly struct Awaiter : ICriticalNotifyCompletion
            {
                private readonly int frameCount;
                private readonly PlayerLoopTiming timing;

                internal Awaiter(int frameCount, PlayerLoopTiming timing)
                {
                    this.frameCount = frameCount;
                    this.timing = timing;
                }

                public bool IsCompleted => frameCount == 0;

                public void GetResult()
                {
                }

                public void OnCompleted(Action continuation)
                {
                    Task233PlayerLoop.EnqueueDelayFrame(timing, frameCount, continuation);
                }

                public void UnsafeOnCompleted(Action continuation)
                {
                    Task233PlayerLoop.EnqueueDelayFrame(timing, frameCount, continuation);
                }
            }
        }
    }
}
