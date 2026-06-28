using System;
using NUnit.Framework;

namespace Task233.Tests
{
    public sealed class Task233AsyncAwaitTests
    {
        [SetUp]
        public void SetUp()
        {
            Task233PlayerLoop.Reset();
        }

        [Test]
        public void YieldContinuesOnNextPump()
        {
            var completed = false;

            Run();

            Assert.IsFalse(completed);
            Pump();
            Assert.IsTrue(completed);

            async void Run()
            {
                await T233.Yield();
                completed = true;
            }
        }

        [Test]
        public void DelayFramesWaitsRequestedPumps()
        {
            var completed = false;

            Run();

            Pump();
            Assert.IsFalse(completed);
            Pump();
            Assert.IsTrue(completed);

            async void Run()
            {
                await T233.DelayFrames(2);
                completed = true;
            }
        }

        [Test]
        public void DelayMillisecondsZeroCompletesSynchronously()
        {
            var completed = false;

            Run();

            Assert.IsTrue(completed);

            async void Run()
            {
                await T233.DelayMilliseconds(0);
                completed = true;
            }
        }

        [Test]
        public void DelaySecondsZeroCompletesSynchronously()
        {
            var completed = false;

            Run();

            Assert.IsTrue(completed);

            async void Run()
            {
                await T233.DelaySeconds(0d, ignoreTimeScale: true);
                completed = true;
            }
        }

        [Test]
        public void MigrationAliasesCompleteSynchronouslyForZeroDelay()
        {
            var taskDelayCompleted = false;
            var waitForSecondsCompleted = false;

            RunTaskDelayAlias();
            RunWaitForSecondsAlias();

            Assert.IsTrue(taskDelayCompleted);
            Assert.IsTrue(waitForSecondsCompleted);

            async void RunTaskDelayAlias()
            {
                await T233.Delay(TimeSpan.Zero);
                taskDelayCompleted = true;
            }

            async void RunWaitForSecondsAlias()
            {
                await T233.WaitForSeconds(0);
                waitForSecondsCompleted = true;
            }
        }

        [Test]
        public void NextFrameMatchesOneFrameDelay()
        {
            var completed = false;

            Run();

            Assert.IsFalse(completed);
            Pump();
            Assert.IsTrue(completed);

            async void Run()
            {
                await T233.NextFrame();
                completed = true;
            }
        }

        [Test]
        public void CancelSourceStopsAwaitedWork()
        {
            var cancel = T233.CreateCancelSource();
            var canceled = false;
            var completed = false;

            Run();
            cancel.Cancel();
            Pump();

            Assert.IsTrue(completed);
            Assert.IsTrue(canceled);
            cancel.Dispose();

            async void Run()
            {
                try
                {
                    await T233.DelayFrames(4, cancellation: cancel);
                }
                catch (OperationCanceledException)
                {
                    canceled = true;
                }
                finally
                {
                    completed = true;
                }
            }
        }

        [Test]
        public void PostRunsInFifoOrderForBusinessQueue()
        {
            var result = 0;

            T233.Post(() => result = result * 10 + 1);
            T233.Post(() => result = result * 10 + 2);
            T233.Post(() => result = result * 10 + 3);

            Pump();

            Assert.AreEqual(123, result);
        }

        [Test]
        public void NestedAsyncWorkflowCompletesInOrder()
        {
            var state = 0;
            var completed = false;

            Run();
            Pump();

            Assert.IsTrue(completed);
            Assert.AreEqual(123, state);

            async void Run()
            {
                state = state * 10 + 1;
                await Step();
                state = state * 10 + 3;
                completed = true;
            }

            async System.Threading.Tasks.Task Step()
            {
                await T233.DelayFrames(1);
                state = state * 10 + 2;
            }
        }

        [Test]
        public void OwnerCancelPatternPreventsLateUiMutation()
        {
            var cancel = T233.CreateCancelSource();
            var label = "loading";
            var completed = false;

            Run();
            cancel.Cancel();
            Pump();

            Assert.IsTrue(completed);
            Assert.AreEqual("loading", label);
            cancel.Dispose();

            async void Run()
            {
                try
                {
                    await T233.DelayFrames(3, cancellation: cancel);
                    label = "done";
                }
                catch (OperationCanceledException)
                {
                    completed = true;
                }
            }
        }

        [Test]
        public void DebouncePatternKeepsOnlyLastRequest()
        {
            var current = default(Task233CancelSource);
            var applied = 0;

            Submit(1);
            Submit(2);
            Submit(3);

            Pump();
            Assert.AreEqual(0, applied);
            Pump();
            Assert.AreEqual(3, applied);
            current.Dispose();

            void Submit(int value)
            {
                if (current.IsCreated)
                {
                    current.Cancel();
                    current.Dispose();
                }

                current = T233.CreateCancelSource();
                Run(value, current);
            }

            async void Run(int value, Task233CancelSource cancellation)
            {
                try
                {
                    await T233.DelayFrames(2, cancellation: cancellation);
                    applied = value;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        [Test]
        public void TimeoutPatternCancelsSlowWork()
        {
            var slowWorkCancel = T233.CreateCancelSource();
            var timedOut = false;
            var completed = false;

            SlowWork();
            Timeout();

            Pump();
            Assert.IsFalse(timedOut);
            Assert.IsFalse(completed);

            Pump();
            Assert.IsTrue(timedOut);
            Assert.IsFalse(completed);

            Pump();
            Assert.IsTrue(completed);
            Assert.IsTrue(slowWorkCancel.IsCancellationRequested);
            slowWorkCancel.Dispose();

            async void SlowWork()
            {
                try
                {
                    await T233.DelayFrames(8, cancellation: slowWorkCancel);
                }
                catch (OperationCanceledException)
                {
                    completed = true;
                }
            }

            async void Timeout()
            {
                await T233.DelayFrames(2);
                timedOut = true;
                slowWorkCancel.Cancel();
            }
        }

        private static void Pump()
        {
            Task233PlayerLoop.RunForTesting(PlayerLoopTiming.Update);
        }
    }
}
