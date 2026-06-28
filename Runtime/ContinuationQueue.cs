using System;
using UnityEngine;

namespace Task233
{
    internal sealed class ContinuationQueue
    {
        private Action[] continuations = new Action[256];
        private Task233CancelSource[] continuationCancellations = new Task233CancelSource[256];
        private bool[] skipWhenCanceled = new bool[256];
        private int head;
        private int tail;
        private DelayNode delayedHead;
        private DelayNode pool;

        public void Prewarm(int continuationCapacity, int delayNodeCapacity)
        {
            if (continuationCapacity > continuations.Length)
            {
                var count = Count;
                var oldContinuations = continuations;
                var oldCancellations = continuationCancellations;
                var oldSkipWhenCanceled = skipWhenCanceled;
                var nextContinuations = new Action[NextPowerOfTwo(continuationCapacity)];
                var nextCancellations = new Task233CancelSource[nextContinuations.Length];
                var nextSkipWhenCanceled = new bool[nextContinuations.Length];
                for (var i = 0; i < count; i++)
                {
                    var index = (head + i) & (oldContinuations.Length - 1);
                    nextContinuations[i] = oldContinuations[index];
                    nextCancellations[i] = oldCancellations[index];
                    nextSkipWhenCanceled[i] = oldSkipWhenCanceled[index];
                }

                continuations = nextContinuations;
                continuationCancellations = nextCancellations;
                skipWhenCanceled = nextSkipWhenCanceled;
                head = 0;
                tail = count;
            }

            for (var i = 0; i < delayNodeCapacity; i++)
            {
                Return(new DelayNode());
            }
        }

        public void Enqueue(Action continuation, Task233CancelSource cancellation = default, bool skipIfCanceled = false)
        {
            if (continuation == null)
            {
                return;
            }

            if (!Task233Cancellation.Retain(cancellation))
            {
                cancellation = default;
            }

            var nextTail = (tail + 1) & (continuations.Length - 1);
            if (nextTail == head)
            {
                Grow();
                nextTail = (tail + 1) & (continuations.Length - 1);
            }

            continuations[tail] = continuation;
            continuationCancellations[tail] = cancellation;
            skipWhenCanceled[tail] = skipIfCanceled;
            tail = nextTail;
        }

        private void EnqueueRetained(Action continuation, Task233CancelSource cancellation, bool skipIfCanceled = false)
        {
            if (continuation == null)
            {
                Task233Cancellation.Release(cancellation);
                return;
            }

            var nextTail = (tail + 1) & (continuations.Length - 1);
            if (nextTail == head)
            {
                Grow();
                nextTail = (tail + 1) & (continuations.Length - 1);
            }

            continuations[tail] = continuation;
            continuationCancellations[tail] = cancellation;
            skipWhenCanceled[tail] = skipIfCanceled;
            tail = nextTail;
        }

        public void EnqueueDelayFrame(int frames, Action continuation, Task233CancelSource cancellation)
        {
            if (frames <= 0 || cancellation.IsCancellationRequested)
            {
                Enqueue(continuation, cancellation);
                return;
            }

            var node = Rent();
            node.Kind = DelayKind.Frames;
            node.RemainingFrames = frames;
            node.Continuation = continuation;
            node.Cancellation = Task233Cancellation.Retain(cancellation) ? cancellation : default;
            node.Next = delayedHead;
            delayedHead = node;
        }

        public void EnqueueDelaySeconds(double seconds, bool ignoreTimeScale, Action continuation, Task233CancelSource cancellation)
        {
            if (seconds <= 0d || cancellation.IsCancellationRequested)
            {
                Enqueue(continuation, cancellation);
                return;
            }

            var now = ignoreTimeScale ? Time.unscaledTimeAsDouble : Time.timeAsDouble;
            var node = Rent();
            node.Kind = DelayKind.Seconds;
            node.TargetTime = now + seconds;
            node.IgnoreTimeScale = ignoreTimeScale;
            node.Continuation = continuation;
            node.Cancellation = Task233Cancellation.Retain(cancellation) ? cancellation : default;
            node.Next = delayedHead;
            delayedHead = node;
        }

        public void Run()
        {
            MoveReadyDelayed();

            var limit = Count;
            for (var i = 0; i < limit; i++)
            {
                var continuation = Dequeue(out var cancellation, out var skipIfCanceled);
                if (skipIfCanceled && cancellation.IsCancellationRequested)
                {
                    Task233Cancellation.Release(cancellation);
                    continue;
                }

                continuation?.Invoke();
                Task233Cancellation.Release(cancellation);
            }
        }

        public void Clear()
        {
            for (var i = 0; i < continuations.Length; i++)
            {
                Task233Cancellation.Release(continuationCancellations[i]);
            }

            var current = delayedHead;
            while (current != null)
            {
                Task233Cancellation.Release(current.Cancellation);
                current = current.Next;
            }

            Array.Clear(continuations, 0, continuations.Length);
            Array.Clear(continuationCancellations, 0, continuationCancellations.Length);
            Array.Clear(skipWhenCanceled, 0, skipWhenCanceled.Length);
            head = 0;
            tail = 0;
            delayedHead = null;
            pool = null;
        }

        private int Count => tail >= head ? tail - head : continuations.Length - head + tail;

        private Action Dequeue(out Task233CancelSource cancellation, out bool skipIfCanceled)
        {
            if (head == tail)
            {
                cancellation = default;
                skipIfCanceled = false;
                return null;
            }

            var continuation = continuations[head];
            cancellation = continuationCancellations[head];
            skipIfCanceled = skipWhenCanceled[head];
            continuations[head] = null;
            continuationCancellations[head] = default;
            skipWhenCanceled[head] = false;
            head = (head + 1) & (continuations.Length - 1);
            return continuation;
        }

        private void Grow()
        {
            var oldContinuations = continuations;
            var oldCancellations = continuationCancellations;
            var oldSkipWhenCanceled = skipWhenCanceled;
            var count = Count;
            var nextContinuations = new Action[oldContinuations.Length * 2];
            var nextCancellations = new Task233CancelSource[nextContinuations.Length];
            var nextSkipWhenCanceled = new bool[nextContinuations.Length];
            for (var i = 0; i < count; i++)
            {
                var index = (head + i) & (oldContinuations.Length - 1);
                nextContinuations[i] = oldContinuations[index];
                nextCancellations[i] = oldCancellations[index];
                nextSkipWhenCanceled[i] = oldSkipWhenCanceled[index];
            }

            continuations = nextContinuations;
            continuationCancellations = nextCancellations;
            skipWhenCanceled = nextSkipWhenCanceled;
            head = 0;
            tail = count;
        }

        private void MoveReadyDelayed()
        {
            DelayNode previous = null;
            var current = delayedHead;

            while (current != null)
            {
                var ready = current.Cancellation.IsCancellationRequested || IsReady(current);
                if (!ready)
                {
                    previous = current;
                    current = current.Next;
                    continue;
                }

                var completed = current;
                current = current.Next;

                if (previous == null)
                {
                    delayedHead = current;
                }
                else
                {
                    previous.Next = current;
                }

                EnqueueRetained(completed.Continuation, completed.Cancellation);
                Return(completed);
            }
        }

        private static bool IsReady(DelayNode node)
        {
            if (node.Kind == DelayKind.Frames)
            {
                node.RemainingFrames--;
                return node.RemainingFrames <= 0;
            }

            var now = node.IgnoreTimeScale ? Time.unscaledTimeAsDouble : Time.timeAsDouble;
            return now >= node.TargetTime;
        }

        private DelayNode Rent()
        {
            var node = pool;
            if (node == null)
            {
                return new DelayNode();
            }

            pool = node.Next;
            node.Next = null;
            return node;
        }

        private void Return(DelayNode node)
        {
            node.Kind = DelayKind.Frames;
            node.RemainingFrames = 0;
            node.TargetTime = 0d;
            node.IgnoreTimeScale = false;
            node.Continuation = null;
            node.Cancellation = default;
            node.Next = pool;
            pool = node;
        }

        private static int NextPowerOfTwo(int value)
        {
            var result = 1;
            while (result < value)
            {
                result <<= 1;
            }

            return result;
        }

        private enum DelayKind
        {
            Frames,
            Seconds
        }

        private sealed class DelayNode
        {
            public DelayKind Kind;
            public int RemainingFrames;
            public double TargetTime;
            public bool IgnoreTimeScale;
            public Action Continuation;
            public Task233CancelSource Cancellation;
            public DelayNode Next;
        }
    }
}
