using System;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace Task233.Tests
{
    public sealed class Task233PerformanceTests
    {
        private static readonly Action NoopAction = Noop;

        [Test, Performance]
        public void PostContinuation()
        {
            T233.Prewarm(16384, 16384);

            Measure.Method(() => T233.Post(NoopAction))
                .WarmupCount(20)
                .MeasurementCount(1000)
                .IterationsPerMeasurement(1)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void ScheduleYieldAwaitable()
        {
            Measure.Method(() => _ = T233.Yield())
                .WarmupCount(20)
                .MeasurementCount(100)
                .IterationsPerMeasurement(10000)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void ScheduleDelayFrameAwaitable()
        {
            Measure.Method(() => _ = T233.DelayFrames(1))
                .WarmupCount(20)
                .MeasurementCount(100)
                .IterationsPerMeasurement(10000)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void ScheduleDelaySecondsAwaitable()
        {
            Measure.Method(() => _ = T233.DelaySeconds(0.001d))
                .WarmupCount(20)
                .MeasurementCount(100)
                .IterationsPerMeasurement(10000)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void ScheduleDelayMillisecondsAwaitable()
        {
            Measure.Method(() => _ = T233.DelayMilliseconds(1))
                .WarmupCount(20)
                .MeasurementCount(100)
                .IterationsPerMeasurement(10000)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void CancelSourceCreateCancelDispose()
        {
            Measure.Method(() =>
                {
                    var cancel = T233.CreateCancelSource();
                    cancel.Cancel();
                    cancel.Dispose();
                })
                .WarmupCount(20)
                .MeasurementCount(100)
                .IterationsPerMeasurement(10000)
                .GC()
                .Run();
        }

        [Test]
        public void DelayFrameZeroCompletesSynchronously()
        {
            var awaiter = T233.DelayFrames(0).GetAwaiter();
            Assert.IsTrue(awaiter.IsCompleted);
            awaiter.GetResult();
        }

        [Test]
        public void CancelBeforeAwaitThrows()
        {
            var cancel = T233.CreateCancelSource();
            cancel.Cancel();

            try
            {
                var awaiter = T233.DelayFrames(1, cancellation: cancel).GetAwaiter();
                Assert.IsTrue(awaiter.IsCompleted);
                awaiter.GetResult();
                Assert.Fail("Expected OperationCanceledException.");
            }
            catch (OperationCanceledException)
            {
                Assert.Pass();
            }
            finally
            {
                cancel.Dispose();
            }
        }

        private static void Noop()
        {
        }
    }
}
