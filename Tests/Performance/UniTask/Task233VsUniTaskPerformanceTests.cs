#if TASK233_HAS_UNITASK
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace Task233.Tests
{
    public sealed class Task233VsUniTaskPerformanceTests
    {
        private const int FactoryIterations = 20000000;
        private const int CancellationIterations = 2000000;

        [Test, Performance]
        public void Task233YieldFactory()
        {
            Measure.Method(() => _ = T233.Yield())
                .WarmupCount(0)
                .MeasurementCount(1)
                .IterationsPerMeasurement(FactoryIterations)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void UniTaskYieldFactory()
        {
            Measure.Method(() => _ = UniTask.Yield())
                .WarmupCount(0)
                .MeasurementCount(1)
                .IterationsPerMeasurement(FactoryIterations)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void Task233DelayFrameFactory()
        {
            Measure.Method(() => _ = T233.DelayFrames(1))
                .WarmupCount(0)
                .MeasurementCount(1)
                .IterationsPerMeasurement(FactoryIterations)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void UniTaskDelayFrameFactory()
        {
            Measure.Method(() => _ = UniTask.DelayFrame(1))
                .WarmupCount(0)
                .MeasurementCount(1)
                .IterationsPerMeasurement(FactoryIterations)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void Task233DelaySecondsFactory()
        {
            Measure.Method(() => _ = T233.DelaySeconds(0.001d))
                .WarmupCount(0)
                .MeasurementCount(1)
                .IterationsPerMeasurement(FactoryIterations)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void UniTaskDelaySecondsFactory()
        {
            Measure.Method(() => _ = UniTask.Delay(TimeSpan.FromSeconds(0.001d)))
                .WarmupCount(0)
                .MeasurementCount(1)
                .IterationsPerMeasurement(FactoryIterations)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void Task233DelayMillisecondsFactory()
        {
            Measure.Method(() => _ = T233.DelayMilliseconds(1))
                .WarmupCount(0)
                .MeasurementCount(1)
                .IterationsPerMeasurement(FactoryIterations)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void UniTaskDelayMillisecondsFactory()
        {
            Measure.Method(() => _ = UniTask.Delay(1))
                .WarmupCount(0)
                .MeasurementCount(1)
                .IterationsPerMeasurement(FactoryIterations)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void Task233CancelSourceCreateCancelDispose()
        {
            Measure.Method(() =>
                {
                    var cancel = T233.CreateCancelSource();
                    cancel.Cancel();
                    cancel.Dispose();
                })
                .WarmupCount(0)
                .MeasurementCount(1)
                .IterationsPerMeasurement(CancellationIterations)
                .GC()
                .Run();
        }

        [Test, Performance]
        public void CancellationTokenSourceCreateCancelDispose()
        {
            Measure.Method(() =>
                {
                    using (var cancel = new CancellationTokenSource())
                    {
                        cancel.Cancel();
                    }
                })
                .WarmupCount(0)
                .MeasurementCount(1)
                .IterationsPerMeasurement(CancellationIterations)
                .GC()
                .Run();
        }

    }
}
#endif
