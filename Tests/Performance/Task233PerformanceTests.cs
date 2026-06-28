using System.Threading.Tasks;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace Task233.Tests
{
    public sealed class Task233PerformanceTests
    {
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
            Measure.Method(() => _ = T233.DelayFrame(1))
                .WarmupCount(20)
                .MeasurementCount(100)
                .IterationsPerMeasurement(10000)
                .GC()
                .Run();
        }

        [Test]
        public async Task DelayFrameZeroCompletesSynchronously()
        {
            await T233.DelayFrame(0);
            Assert.Pass();
        }
    }
}
