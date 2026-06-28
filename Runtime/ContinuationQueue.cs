using System;

namespace Task233
{
    internal sealed class ContinuationQueue
    {
        private Action[] continuations = new Action[256];
        private int head;
        private int tail;
        private DelayNode delayedHead;
        private DelayNode pool;

        public void Enqueue(Action continuation)
        {
            if (continuation == null)
            {
                return;
            }

            var nextTail = (tail + 1) % continuations.Length;
            if (nextTail == head)
            {
                Grow();
                nextTail = (tail + 1) % continuations.Length;
            }

            continuations[tail] = continuation;
            tail = nextTail;
        }

        public void EnqueueDelayFrame(int frames, Action continuation)
        {
            if (frames <= 0)
            {
                Enqueue(continuation);
                return;
            }

            var node = Rent();
            node.RemainingFrames = frames;
            node.Continuation = continuation;
            node.Next = delayedHead;
            delayedHead = node;
        }

        public void Run()
        {
            MoveReadyDelayed();

            var limit = Count;
            for (var i = 0; i < limit; i++)
            {
                var continuation = Dequeue();
                continuation?.Invoke();
            }
        }

        public void Clear()
        {
            Array.Clear(continuations, 0, continuations.Length);
            head = 0;
            tail = 0;
            delayedHead = null;
            pool = null;
        }

        private int Count => tail >= head ? tail - head : continuations.Length - head + tail;

        private Action Dequeue()
        {
            if (head == tail)
            {
                return null;
            }

            var continuation = continuations[head];
            continuations[head] = null;
            head = (head + 1) % continuations.Length;
            return continuation;
        }

        private void Grow()
        {
            var old = continuations;
            var count = Count;
            var next = new Action[old.Length * 2];
            for (var i = 0; i < count; i++)
            {
                next[i] = old[(head + i) % old.Length];
            }

            continuations = next;
            head = 0;
            tail = count;
        }

        private void MoveReadyDelayed()
        {
            DelayNode previous = null;
            var current = delayedHead;

            while (current != null)
            {
                current.RemainingFrames--;
                if (current.RemainingFrames > 0)
                {
                    previous = current;
                    current = current.Next;
                    continue;
                }

                var ready = current;
                current = current.Next;

                if (previous == null)
                {
                    delayedHead = current;
                }
                else
                {
                    previous.Next = current;
                }

                Enqueue(ready.Continuation);
                Return(ready);
            }
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
            node.Continuation = null;
            node.RemainingFrames = 0;
            node.Next = pool;
            pool = node;
        }

        private sealed class DelayNode
        {
            public int RemainingFrames;
            public Action Continuation;
            public DelayNode Next;
        }
    }
}
